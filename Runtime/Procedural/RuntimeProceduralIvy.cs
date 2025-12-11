using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class RuntimeProceduralIvy : RTIvy
    {
        private RuntimeIvyGrowth rtIvyGrowth;

        protected override void Init(IvyContainer ivyContainer, IvyParameters ivyParameters)
        {
            base.Init(ivyContainer, ivyParameters);

            rtIvyGrowth = new RuntimeIvyGrowth();
            rtIvyGrowth.Init(rtIvyContainer, ivyParameters, gameObject, leavesMeshesByChosenLeaf,
                GetMaxNumPoints(), GetMaxNumLeaves(), GetMaxNumVerticesPerLeaf());

            for (var i = 0; i < 10; i++) rtIvyGrowth.Step();

            currentLifetime = growthParameters.lifetime;
        }

        protected override void NextPoints(int branchIndex)
        {
            base.NextPoints(branchIndex);
            rtIvyGrowth.Step();
        }

        public override bool IsGrowingFinished()
        {
            var res = currentTimer > currentLifetime;
            return res;
        }

        protected override float GetNormalizedLifeTime()
        {
            var res = currentTimer / growthParameters.lifetime;
            res = Mathf.Clamp(res, 0.1f, 1f);
            return res;
        }

        public void SetIvyParameters(IvyPreset ivyPreset)
        {
            ivyParameters.CopyFrom(ivyPreset);
        }

        protected override void InitializeMeshesData(Mesh bakedMesh, int numBranches)
        {
            meshBuilder.InitializeMeshesDataProcedural(bakedMesh, numBranches, growthParameters.lifetime,
                growthParameters.growthSpeed);
        }

        protected override int GetMaxNumPoints()
        {
            var timePerPoint = ivyParameters.stepSize / growthParameters.growthSpeed;
            var res = Mathf.CeilToInt(growthParameters.lifetime / timePerPoint) * ivyParameters.maxBranchs * 2;

            res = 20;

            return res;
        }

        protected override int GetMaxNumLeaves()
        {
            var res = GetMaxNumPoints();

            return res;
        }

        public override void InitIvy(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer,
            IvyParameters ivyParameters)
        {
            this.growthParameters = growthParameters;
            Init(null, ivyParameters);
            InitMeshBuilder();
            AddFirstBranch();
        }

        private int GetMaxNumVerticesPerLeaf()
        {
            var res = 0;

            for (var i = 0; i < ivyParameters.leavesPrefabs.Length; i++)
                if (res <= leavesMeshesByChosenLeaf[i].vertices.Length)
                    res = leavesMeshesByChosenLeaf[i].vertices.Length;

            return res;
        }
    }
}