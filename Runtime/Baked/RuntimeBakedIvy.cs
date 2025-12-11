using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class RuntimeBakedIvy : RTIvy
    {
        public override bool IsGrowingFinished()
        {
            var res = true;

            if (rtIvyContainer.branches.Count > rtBuildingIvyContainer.branches.Count)
                res = false;
            else
                for (var i = 0; i < activeBakedBranches.Count; i++)
                    res = res && rtBuildingIvyContainer.branches[i].branchPoints.Count >=
                        activeBakedBranches[i].branchPoints.Count;

            return res;
        }

        protected override void Init(IvyContainer ivyContainer, IvyParameters ivyParameters)
        {
            base.Init(ivyContainer, ivyParameters);
            CalculateLifetime();
        }

        private void CalculateLifetime()
        {
            var totalIvyLength = 0f;
            for (var i = 0; i < rtIvyContainer.branches.Count; i++)
                totalIvyLength += rtIvyContainer.branches[i].totalLength;

            currentLifetime = totalIvyLength / growthParameters.growthSpeed;
            currentLifetime *= 2;
        }

        protected override float GetNormalizedLifeTime()
        {
            var res = 0f;

            res = rtBuildingIvyContainer.branches[0].totalLength / rtIvyContainer.branches[0].totalLength;
            res = Mathf.Clamp(res, 0.1f, 1f);

            return res;
        }

        protected override void InitializeMeshesData(Mesh bakedMesh, int numBranches)
        {
            meshBuilder.InitializeMeshesDataBaked(bakedMesh, numBranches);
        }

        protected override int GetMaxNumPoints()
        {
            return 0;
        }

        protected override int GetMaxNumLeaves()
        {
            return 0;
        }

        public override void InitIvy(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer,
            IvyParameters ivyParameters)
        {
            this.growthParameters = growthParameters;
            Init(ivyContainer, ivyParameters);
            InitMeshBuilder();
            AddFirstBranch();
        }

        public void InitIvyEditor(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer,
            IvyParameters ivyParameters)
        {
            this.growthParameters = growthParameters;
            Init(ivyContainer, ivyParameters);
        }
    }
}