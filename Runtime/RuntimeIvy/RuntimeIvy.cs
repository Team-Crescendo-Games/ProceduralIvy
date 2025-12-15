using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace TeamCrescendo.ProceduralIvy
{
    public abstract class RuntimeIvy : MonoBehaviour
    {
        public bool verbose = false;

        private MeshRenderer processedMeshRenderer;
        private MeshFilter processedMeshFilter;

        protected List<RTBranchContainer> activeBakedBranches;
        private List<RTBranchContainer> activeBuildingBranches;
        private int backtrackingPoints;
        private Mesh bakedMesh;
        private float currentGrowthSpeed;
        protected float currentLifetime;
        private float currentSpeed;
        protected float currentTimer;

        protected RuntimeGrowthParameters growthParameters;

        protected IvyParameters ivyParameters;

        private int lastIdxActiveBranch;
        private List<Material> leavesMaterials;

        protected RTMeshData[] leavesMeshesByChosenLeaf;
        private int maxBranches;

        protected RTBakedMeshBuilder meshBuilder;
        private Mesh processedMesh;

        private bool refreshProcessedMesh;

        protected RTIvyContainer rtBuildingIvyContainer;
        protected RTIvyContainer rtIvyContainer;

        private List<float> srcTotalLengthPerBranch;
        private List<float> dstTotalLengthPerBranch;
        private List<float> growingFactorPerBranch;
        private List<Vector3> srcPoints;
        private List<Vector3> dstPoints;
        private int[] submeshByChoseLeaf;

        private bool awoken = false;

        public void Awake()
        {
            if (awoken) return;
            
            // destroy baked mf and mr
            MeshFilter mf = GetComponent<MeshFilter>();
            Assert.IsNotNull(mf);
            bakedMesh = mf.sharedMesh;
            Destroy(mf);

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null) Destroy(mr);
            
            var childProcessedMeshObj = new GameObject($"{name} + ProcessedMesh");
            childProcessedMeshObj.transform.SetParent(transform);
            childProcessedMeshObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            childProcessedMeshObj.hideFlags = HideFlags.HideAndDontSave;

            processedMeshRenderer = childProcessedMeshObj.AddComponent<MeshRenderer>();
            processedMeshFilter = childProcessedMeshObj.AddComponent<MeshFilter>();
            
            awoken = true;
        }
        
        public virtual void Initialize(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer, IvyParameters ivyParameters)
        {
            // initialize if not already
            Awake();
            
            this.growthParameters = growthParameters;
            rtIvyContainer = new RTIvyContainer();
            rtBuildingIvyContainer = new RTIvyContainer();

            // instantiate a new parameter set object
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
            processedMeshFilter.sharedMesh = processedMesh;

            backtrackingPoints = Mathf.CeilToInt(ivyParameters.tipInfluence / ivyParameters.stepSize);

            if (bakedMesh == null)
                bakedMesh = new Mesh { subMeshCount = submeshCount };
            
            srcPoints = new List<Vector3>(new Vector3[maxBranches]);
            dstPoints = new List<Vector3>(new Vector3[maxBranches]);
            growingFactorPerBranch = new List<float>(new float[maxBranches]);
            srcTotalLengthPerBranch = new List<float>(new float[maxBranches]);
            dstTotalLengthPerBranch = new List<float>(new float[maxBranches]);

            for (var i = 0; i < maxBranches; i++)
            {
                var branchContainer = new RTBranchContainer(GetMaxNumPoints(), GetMaxNumLeaves());
                activeBuildingBranches.Add(branchContainer);
            }
            
            // init mesh builder
            meshBuilder = new RTBakedMeshBuilder(ivyParameters, rtBuildingIvyContainer,
                this, maxBranches, processedMesh, processedMeshRenderer,
                backtrackingPoints, submeshByChoseLeaf, leavesMeshesByChosenLeaf, 
                leavesMaterials);

            InitializeMeshesData(bakedMesh, maxBranches);
            
            // add first branch
            AddNextBranch(0);
        }

        private void SetUpMaxBranches(IvyContainer ivyContainer)
        {
            maxBranches = ivyParameters.maxBranches;
            if (ivyContainer != null) 
                maxBranches = Mathf.Max(ivyParameters.maxBranches, ivyContainer.branches.Count);
        }

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
                if (!prefabs[i].TryGetComponent<MeshRenderer>(out var leafMeshRenderer))
                    throw new ArgumentException($"No MeshRenderer found on {prefabs[i].name}. Assign a valid prefab.");
                if (!prefabs[i].TryGetComponent<MeshFilter>(out var leafMeshFilter))
                    throw new ArgumentException($"No MeshFilter found on {prefabs[i].name}. Assign a valid prefab.");
                
                if (!leavesMaterials.Contains(leafMeshRenderer.sharedMaterial))
                {
                    leavesMaterials.Add(leafMeshRenderer.sharedMaterial);
                    submeshCount++;
                }

                submeshByChoseLeaf[i] = submeshCount;
                var leafMeshData = new RTMeshData(leafMeshFilter.sharedMesh);
                leavesMeshesByChosenLeaf[i] = leafMeshData;
            }

            processedMeshRenderer.SetSharedMaterials(leavesMaterials);
        }

        public void UpdateIvy(float dt)
        {
            UpdateGrowthSpeed();

            for (var i = 0; i < activeBakedBranches.Count; i++) 
                Growing(i, dt);

            currentTimer += dt;

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

        protected abstract void InitializeMeshesData(Mesh bakedMesh, int numBranches);
        protected abstract float GetNormalizedLifeTime();
        protected abstract int GetMaxNumPoints();
        protected abstract int GetMaxNumLeaves();
        public abstract bool IsGrowingFinished();
    }
}