using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TeamCrescendo.ProceduralIvy
{
    public abstract class RuntimeIvy : MonoBehaviour
    {
        public bool verbose = false;
        
        private MeshFilter meshFilter;
        public MeshFilter MeshFilter => meshFilter ??= GetComponent<MeshFilter>();
        
        private MeshRenderer meshRenderer;
        public MeshRenderer MeshRenderer => meshRenderer ??= GetComponent<MeshRenderer>();
        
        public MeshRenderer mrProcessedMesh;
        public MeshFilter mfProcessedMesh;
        
        private List<float> srcTotalLengthPerBranch;
        private List<float> dstTotalLengthPerBranch;
        private List<float> growingFactorPerBranch;

        protected List<RTBranchContainer> activeBakedBranches;
        protected List<RTBranchContainer> activeBuildingBranches;
        private int backtrackingPoints;
        private Mesh bakedMesh;
        protected float currentGrowthSpeed;
        protected float currentLifetime;
        protected float currentSpeed;
        protected float currentTimer;
        protected List<Vector3> dstPoints;

        protected RuntimeGrowthParameters growthParameters;

        protected IvyParameters ivyParameters;
        protected List<int> lastCopiedIndexPerBranch;

        protected int lastIdxActiveBranch;
        protected List<Material> leavesMaterials;

        protected RTMeshData[] leavesMeshesByChosenLeaf;
        protected int maxBranches;

        public RTBakedMeshBuilder meshBuilder;
        private Mesh processedMesh;

        private bool refreshProcessedMesh;

        protected RTIvyContainer rtBuildingIvyContainer;
        protected RTIvyContainer rtIvyContainer;

        protected List<Vector3> srcPoints;
        protected int[] submeshByChoseLeaf;

        public void AwakeInit()
        {
            bakedMesh = MeshFilter.sharedMesh;
            MeshFilter.sharedMesh = null;
        }

        protected virtual void Init(IvyContainer ivyContainer, IvyParameters ivyParameters)
        {
            rtIvyContainer = new RTIvyContainer();
            rtBuildingIvyContainer = new RTIvyContainer();

            this.ivyParameters = new IvyParameters(ivyParameters);

            CreateLeavesDict();

            if (ivyContainer != null)
                rtIvyContainer.Initialize(ivyContainer, ivyParameters, gameObject,
                    leavesMeshesByChosenLeaf, ivyContainer.firstVertexVector);
            else
                rtIvyContainer.Initialize();

            SetUpMaxBranches(ivyContainer);

            activeBakedBranches = new List<RTBranchContainer>(maxBranches);
            activeBuildingBranches = new List<RTBranchContainer>(maxBranches);
            
            var firstVertexVector =
                ivyContainer == null ? CalculateFirstVertexVector() : ivyContainer.firstVertexVector;
            rtBuildingIvyContainer.Initialize(firstVertexVector);
            lastIdxActiveBranch = -1;

            var submeshCount = ivyParameters.leavesPrefabs.Length + 1;
            processedMesh = new Mesh { subMeshCount = submeshCount };
            
            mfProcessedMesh.sharedMesh = processedMesh;

            refreshProcessedMesh = false;
            backtrackingPoints = GetBacktrackingPoints();

            if (bakedMesh == null)
            {
                bakedMesh = new Mesh { subMeshCount = submeshCount };
                bakedMesh.subMeshCount = submeshCount;
            }
            
            lastCopiedIndexPerBranch = new List<int>(maxBranches);
            srcPoints = new List<Vector3>(maxBranches);
            dstPoints = new List<Vector3>(maxBranches);
            growingFactorPerBranch = new List<float>(maxBranches);
            srcTotalLengthPerBranch = new List<float>(maxBranches);
            dstTotalLengthPerBranch = new List<float>(maxBranches);

            for (var i = 0; i < maxBranches; i++)
            {
                srcPoints.Add(Vector3.zero);
                dstPoints.Add(Vector3.zero);
                growingFactorPerBranch.Add(0f);
                srcTotalLengthPerBranch.Add(0f);
                dstTotalLengthPerBranch.Add(0f);
                lastCopiedIndexPerBranch.Add(-1);

                var branchPointsSize = GetMaxNumPoints();
                var numLeaves = GetMaxNumLeaves();

                var branchContainer = new RTBranchContainer(branchPointsSize, numLeaves);
                activeBuildingBranches.Add(branchContainer);
            }
        }

        private void SetUpMaxBranches(IvyContainer ivyContainer)
        {
            maxBranches = ivyParameters.maxBranches;
            if (ivyContainer != null) maxBranches = Mathf.Max(ivyParameters.maxBranches, ivyContainer.branches.Count);
        }

        protected void InitMeshBuilder()
        {
            meshBuilder = new RTBakedMeshBuilder(ivyParameters, rtBuildingIvyContainer,
                this, maxBranches, processedMesh, mrProcessedMesh,
                backtrackingPoints, submeshByChoseLeaf, leavesMeshesByChosenLeaf, 
                leavesMaterials);

            InitializeMeshesData(bakedMesh, maxBranches);
        }

        protected void AddFirstBranch()
        {
            AddNextBranch(0);
        }

        private int GetBacktrackingPoints()
        {
            var res = Mathf.CeilToInt(ivyParameters.tipInfluence / ivyParameters.stepSize);
            return res;
        }

        public void UpdateIvy(float deltaTime)
        {
            UpdateGrowthSpeed();

            for (var i = 0; i < activeBakedBranches.Count; i++) 
                Growing(i, deltaTime);

            currentTimer += deltaTime;

            if (!RefreshGeometry())
                return;

            if (refreshProcessedMesh)
            {
                meshBuilder.RefreshProcessedMesh();
                refreshProcessedMesh = false;
            }
        }

        protected void Growing(int branchIndex, float deltaTime)
        {
            var currentBranch = activeBuildingBranches[branchIndex];

            CalculateFactors(srcPoints[branchIndex], dstPoints[branchIndex]);
            growingFactorPerBranch[branchIndex] += currentSpeed * deltaTime;
            growingFactorPerBranch[branchIndex] = Mathf.Clamp(growingFactorPerBranch[branchIndex], 0f, 1f);

            currentBranch.totalLength = Mathf.Lerp(srcTotalLengthPerBranch[branchIndex],
                dstTotalLengthPerBranch[branchIndex], growingFactorPerBranch[branchIndex]);

            var lastPoint = currentBranch.GetLastBranchPoint();
            lastPoint.length = currentBranch.totalLength;

            lastPoint.point = Vector3.Lerp(srcPoints[branchIndex], dstPoints[branchIndex],
                growingFactorPerBranch[branchIndex]);

            if (growingFactorPerBranch[branchIndex] >= 1)
            {
                if (!RefreshGeometry())
                    return;
                NextPoints(branchIndex);
            }
        }

        protected virtual void NextPoints(int branchIndex)
        {
            if (rtBuildingIvyContainer.branches[branchIndex].branchPoints.Count > 0)
            {
                var lastBuildingBranchPoint = rtBuildingIvyContainer.branches[branchIndex].GetLastBranchPoint();
                if (lastBuildingBranchPoint.index < activeBakedBranches[branchIndex].branchPoints.Count - 1)
                {
                    var indexBranchPoint = lastBuildingBranchPoint.index;
                    indexBranchPoint++;

                    var branchPoint = activeBakedBranches[branchIndex].branchPoints[indexBranchPoint];
                    var branch = rtBuildingIvyContainer.branches[branchIndex];

                    branch.AddBranchPoint(branchPoint, ivyParameters.stepSize);

                    if (branchPoint.newBranch)
                    {
                        var candidateBranch =
                            rtIvyContainer.GetBranchContainerByBranchNumber(branchPoint.newBranchNumber);
                        if (candidateBranch.branchPoints.Count >= 2) AddNextBranch(branchPoint.newBranchNumber);
                    }

                    UpdateGrowingPoints(branchIndex);

                    if (rtBuildingIvyContainer.branches[branchIndex].branchPoints.Count > backtrackingPoints)
                    {
                        if (!IsVertexLimitReached())
                        {
                            meshBuilder.CheckCopyMesh(branchIndex, activeBakedBranches);
                            refreshProcessedMesh = true;
                        }
                        else
                        {
                            Debug.LogWarning("Limit vertices reached! --> " + Constants.VERTEX_LIMIT_16 + " vertices");
                        }
                    }
                }
            }
        }

        private void CalculateFactors(Vector3 srcPoint, Vector3 dstPoint)
        {
            var factor = Vector3.Distance(srcPoint, dstPoint) / ivyParameters.stepSize;
            factor = 1.0f / factor;
            currentSpeed = factor * currentGrowthSpeed;
        }

        protected virtual void AddNextBranch(int branchNumber)
        {
            lastIdxActiveBranch++;

            var newBuildingBranch = activeBuildingBranches[lastIdxActiveBranch];
            var bakedBranch = rtIvyContainer.GetBranchContainerByBranchNumber(branchNumber);

            newBuildingBranch.AddBranchPoint(bakedBranch.branchPoints[0], ivyParameters.stepSize);
            newBuildingBranch.AddBranchPoint(bakedBranch.branchPoints[1], ivyParameters.stepSize);


            newBuildingBranch.leavesOrderedByInitSegment = bakedBranch.leavesOrderedByInitSegment;

            rtBuildingIvyContainer.AddBranch(newBuildingBranch);
            activeBakedBranches.Add(bakedBranch);
            activeBuildingBranches.Add(newBuildingBranch);
            meshBuilder.activeBranches.Add(newBuildingBranch);

            UpdateGrowingPoints(rtBuildingIvyContainer.branches.Count - 1);

            var lastBranchPoint = newBuildingBranch.GetLastBranchPoint();
            if (lastBranchPoint.newBranch) AddNextBranch(lastBranchPoint.newBranchNumber);
        }

        private void UpdateGrowingPoints(int branchIndex)
        {
            if (rtBuildingIvyContainer.branches[branchIndex].branchPoints.Count > 0)
            {
                var fromPoint = rtBuildingIvyContainer.branches[branchIndex].GetLastBranchPoint();
                if (fromPoint.index < activeBakedBranches[branchIndex].branchPoints.Count - 1)
                {
                    var nextPoint = activeBakedBranches[branchIndex].branchPoints[fromPoint.index + 1];
                    growingFactorPerBranch[branchIndex] = 0f;

                    srcPoints[branchIndex] = fromPoint.point;
                    dstPoints[branchIndex] = nextPoint.point;


                    srcTotalLengthPerBranch[branchIndex] = fromPoint.length;
                    dstTotalLengthPerBranch[branchIndex] = fromPoint.length + ivyParameters.stepSize;
                }
            }
        }

        private bool RefreshGeometry()
        {
            // try
            // {
                meshBuilder.BuildGeometry(activeBakedBranches, activeBuildingBranches);
                return true;
            // }
            // catch (Exception e)
            // {
            //     if (verbose) Debug.LogWarning(e);
            //     return false;
            // }
        }

        private void UpdateGrowthSpeed()
        {
            currentGrowthSpeed = growthParameters.growthSpeed;

            if (growthParameters.speedOverLifetimeEnabled)
            {
                var t = GetNormalizedLifeTime();
                currentGrowthSpeed = growthParameters.growthSpeed * growthParameters.speedOverLifetimeCurve.Evaluate(t);
            }
        }

        public bool IsVertexLimitReached()
        {
            var numVertices = meshBuilder.processedMeshData.VertexCount() + ivyParameters.sides + 1;
            return numVertices >= Constants.VERTEX_LIMIT_16;
        }

        private Vector3 CalculateFirstVertexVector() =>
            Quaternion.AngleAxis(Random.value * 360f, transform.up) * transform.forward;

        private void CreateLeavesDict()
        {
            leavesMaterials = new List<Material>();

            var prefabs = ivyParameters.leavesPrefabs;
            leavesMeshesByChosenLeaf = new RTMeshData[prefabs.Length];

            leavesMaterials.Add(ivyParameters.branchesMaterial);

            submeshByChoseLeaf = new int[prefabs.Length];
            var submeshCount = 0;
            for (var i = 0; i < prefabs.Length; i++)
            {
                var leafMeshRenderer = prefabs[i].GetComponent<MeshRenderer>();
                var leafMeshFilter = prefabs[i].GetComponent<MeshFilter>();

                if (!leavesMaterials.Contains(leafMeshRenderer.sharedMaterial))
                {
                    leavesMaterials.Add(leafMeshRenderer.sharedMaterial);
                    submeshCount++;
                }

                submeshByChoseLeaf[i] = submeshCount;
                var leafMeshData = new RTMeshData(leafMeshFilter.sharedMesh);
                leavesMeshesByChosenLeaf[i] = leafMeshData;
            }

            var materials = leavesMaterials.ToArray();
            mrProcessedMesh.sharedMaterials = materials;
        }

        protected abstract void InitializeMeshesData(Mesh bakedMesh, int numBranches);
        protected abstract float GetNormalizedLifeTime();
        protected abstract int GetMaxNumPoints();
        protected abstract int GetMaxNumLeaves();
        public abstract bool IsGrowingFinished();

        public abstract void InitIvy(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer,
            IvyParameters ivyParameters);
    }
}