using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace TeamCrescendo.ProceduralIvy
{
    public class RuntimeIvy : MonoBehaviour
    {
        private MeshRenderer mr;
        private MeshFilter mf;

        private List<BranchContainer> activeBakedBranches;
        private List<BranchContainer> activeBuildingBranches;
        private int backtrackingPoints;
        
        private float currentGrowthSpeed;
        private float currentSpeed;
        private RuntimeGrowthParameters growthParameters;
        private IvyParameters ivyParameters;

        private int lastIdxActiveBranch;

        private RuntimeMeshBuilder meshBuilder;

        private bool refreshProcessedMesh;

        private IvyContainer rtBuildingIvyContainer;
        private IvyContainer rtIvyContainer;

        private float[] srcTotalLengthPerBranch;
        private float[] dstTotalLengthPerBranch;
        private float[] growingFactorPerBranch;
        private Vector3[] srcPoints;
        private Vector3[] dstPoints;

        private bool awoken = false;

        public void Awake()
        {
            if (awoken) return;
            
            mf = GetComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();
            
            Assert.IsNotNull(mf);
            Assert.IsNotNull(mr);
            
            awoken = true;
        }
        
        public void Initialize(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer, IvyParameters ivyParameters)
        {
            if (ivyContainer == null || ivyContainer.branches.Count == 0)
                throw new ArgumentException("[RuntimeIvy] No branches found in IvyContainer. Use the Editor Window to create a baked ivy first!");
            
            // initialize if not already
            Awake();
            
            this.growthParameters = growthParameters;

            // instantiate a new parameter set object
            this.ivyParameters = new IvyParameters(ivyParameters);

            CreateLeavesDict(out List<Material> leavesMaterials, 
                out int[] submeshByChosenLeaf, 
                out MeshData[] leavesMeshesByChosenLeaf);
            
            rtIvyContainer = IvyContainer.Create(ivyContainer, ivyParameters, gameObject,
                leavesMeshesByChosenLeaf, ivyContainer.firstVertexVector);

            int maxBranches = GetMaxBranches(ivyContainer);

            activeBakedBranches = new List<BranchContainer>(maxBranches);
            activeBuildingBranches = new List<BranchContainer>(maxBranches);
            for (var i = 0; i < maxBranches; i++)
                activeBuildingBranches.Add(BranchContainer.Create(maxBranches));

            var firstVertexVector = ivyContainer == null ? 
                CalculateFirstVertexVector() 
                : ivyContainer.firstVertexVector;
            rtBuildingIvyContainer = IvyContainer.Create(firstVertexVector);
            lastIdxActiveBranch = -1;
            
            var submeshCount = ivyParameters.leavesPrefabs.Length + 1;
            var processedMesh = new Mesh { subMeshCount = submeshCount };
            Mesh bakedMesh = mf.sharedMesh;
            mf.sharedMesh = processedMesh;

            backtrackingPoints = Mathf.CeilToInt(ivyParameters.tipInfluence / ivyParameters.stepSize);
            if (backtrackingPoints < 2)
                throw new ArgumentException($"[RuntimeIvy] Invalid backtracking points: {backtrackingPoints}. Decrease step size or tipInfluence!");

            srcPoints = new Vector3[maxBranches];
            dstPoints = new Vector3[maxBranches];
            growingFactorPerBranch = new float[maxBranches];
            srcTotalLengthPerBranch = new float[maxBranches];
            dstTotalLengthPerBranch = new float[maxBranches];
            
            // init mesh builder
            meshBuilder = new RuntimeMeshBuilder(ivyParameters, rtBuildingIvyContainer,
                maxBranches, processedMesh, bakedMesh, mr,
                backtrackingPoints, submeshByChosenLeaf, leavesMeshesByChosenLeaf, 
                leavesMaterials);
            
            // add first branch
            AddNextBranch(0);
        }

        private int GetMaxBranches(IvyContainer ivyContainer)
        {
            int maxBranches = ivyParameters.maxBranches;
            if (ivyContainer != null) 
                maxBranches = Mathf.Max(ivyParameters.maxBranches, ivyContainer.branches.Count);
            return maxBranches;
        }

        private void CreateLeavesDict(
            out List<Material> leavesMaterials, 
            out int[] submeshByChosenLeaf, 
            out MeshData[] leavesMeshesByChosenLeaf)
        {
            leavesMaterials = new ();

            var prefabs = ivyParameters.leavesPrefabs;
            leavesMeshesByChosenLeaf = new MeshData[prefabs.Length];

            leavesMaterials.Add(ivyParameters.branchesMaterial);
            
            submeshByChosenLeaf = new int[prefabs.Length];
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

                submeshByChosenLeaf[i] = submeshCount;
                var leafMeshData = new MeshData(leafMeshFilter.sharedMesh);
                leavesMeshesByChosenLeaf[i] = leafMeshData;
            }

            mr.SetSharedMaterials(leavesMaterials);
        }

        public void UpdateIvy(float dt)
        {
            UpdateGrowthSpeed();
            
            for (var i = 0; i < activeBakedBranches.Count; i++) 
                GrowBranch(i, dt);
            
            if (refreshProcessedMesh)
            {
                meshBuilder.RefreshProcessedMesh();
                refreshProcessedMesh = false;
            }
        }

        private void GrowBranch(int branchIndex, float deltaTime)
        {
            var currentBranch = activeBuildingBranches[branchIndex];

            UpdateGrowthSpeed(srcPoints[branchIndex], dstPoints[branchIndex]);
            growingFactorPerBranch[branchIndex] += currentSpeed * deltaTime;
            growingFactorPerBranch[branchIndex] = Mathf.Clamp01(growingFactorPerBranch[branchIndex]);

            currentBranch.totalLength = Mathf.Lerp(srcTotalLengthPerBranch[branchIndex],
                dstTotalLengthPerBranch[branchIndex], growingFactorPerBranch[branchIndex]);

            var lastPoint = currentBranch.GetLastBranchPoint();
            lastPoint.length = currentBranch.totalLength;

            lastPoint.point = Vector3.Lerp(srcPoints[branchIndex], dstPoints[branchIndex],
                growingFactorPerBranch[branchIndex]);

            if (growingFactorPerBranch[branchIndex] >= 1)
            {
                // if (!RefreshGeometry())
                //     return;
                AdvanceBranchGrowth(branchIndex);
            }
        }

        private void AdvanceBranchGrowth(int branchIndex)
        {
            var currentBranch = rtBuildingIvyContainer.branches[branchIndex];

            if (currentBranch.branchPoints.Count == 0) return;

            var lastBuildingBranchPoint = currentBranch.GetLastBranchPoint();
            var bakedBranch = activeBakedBranches[branchIndex];

            // check if we have reached the end of the baked data
            if (lastBuildingBranchPoint.index >= bakedBranch.branchPoints.Count - 1) return;

            int nextIndex = lastBuildingBranchPoint.index + 1;
            var nextPoint = bakedBranch.branchPoints[nextIndex];

            currentBranch.AddBranchPoint(nextPoint, ivyParameters.stepSize);

            // Handle branching logic
            if (nextPoint.newBranch)
            {
                var candidateBranch = rtIvyContainer.GetBranchContainerByBranchNumber(nextPoint.newBranchNumber);
                if (candidateBranch.branchPoints.Count >= 2) 
                {
                    AddNextBranch(nextPoint.newBranchNumber);
                }
            }

            UpdateGrowingPoints(branchIndex);

            // Handle Mesh Updates
            if (currentBranch.branchPoints.Count > backtrackingPoints)
            {
                if (IsVertexLimitReached())
                {
                    Debug.LogWarning($"Limit vertices reached! --> {Constants.VERTEX_LIMIT_16} vertices");
                    return;
                }

                meshBuilder.CheckCopyMesh(branchIndex, activeBakedBranches);
                refreshProcessedMesh = true;
            }
        }

        private void UpdateGrowthSpeed(Vector3 srcPoint, Vector3 dstPoint)
        {
            float distance = Vector3.Distance(srcPoint, dstPoint);
            if (distance > 0.0001f)
                currentSpeed = ivyParameters.stepSize / distance * currentGrowthSpeed;
        }

        private void AddNextBranch(int branchNumber)
        {
            lastIdxActiveBranch++;

            var newBuildingBranch = activeBuildingBranches[lastIdxActiveBranch];
            BranchContainer bakedBranch = rtIvyContainer.GetBranchContainerByBranchNumber(branchNumber);
            if (bakedBranch == null) 
                throw new ArgumentException($"[RuntimeIvy] Branch {branchNumber} not found in IvyContainer.");

            newBuildingBranch.AddBranchPoint(bakedBranch.branchPoints[0], ivyParameters.stepSize);
            newBuildingBranch.AddBranchPoint(bakedBranch.branchPoints[1], ivyParameters.stepSize);

            newBuildingBranch.leavesOrderedByInitSegment = bakedBranch.leavesOrderedByInitSegment;

            rtBuildingIvyContainer.AddBranchRuntime(newBuildingBranch);
            activeBakedBranches.Add(bakedBranch);
            activeBuildingBranches.Add(newBuildingBranch);

            UpdateGrowingPoints(rtBuildingIvyContainer.branches.Count - 1);

            var lastBranchPoint = newBuildingBranch.GetLastBranchPoint();
            if (lastBranchPoint.newBranch) AddNextBranch(lastBranchPoint.newBranchNumber);
        }

        private void UpdateGrowingPoints(int branchIndex)
        {
            if (rtBuildingIvyContainer.branches[branchIndex].branchPoints.Count == 0) return;
            
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

        protected float GetNormalizedLifeTime()
        {
            var res = rtBuildingIvyContainer.branches[0].totalLength / rtIvyContainer.branches[0].totalLength;
            return Mathf.Clamp(res, 0.1f, 1f);
        }
        
        public bool IsGrowingFinished()
        {
            var res = true;

            if (rtIvyContainer.branches.Count > rtBuildingIvyContainer.branches.Count)
                res = false;
            else
                for (var i = 0; i < activeBakedBranches.Count; i++)
                {
                    res = res && rtBuildingIvyContainer.branches[i].branchPoints.Count >=
                        activeBakedBranches[i].branchPoints.Count;
                }

            return res;
        }
    }
}