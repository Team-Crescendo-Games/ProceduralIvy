using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class RuntimeProceduralIvy : RuntimeIvy
    {
        private RuntimeIvyGrowth rtIvyGrowth;

        public override void Initialize(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer, IvyParameters ivyParameters)
        {
            base.Initialize(growthParameters, ivyContainer, ivyParameters);

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

        public override bool IsGrowingFinished() => currentTimer > currentLifetime;

        protected override float GetNormalizedLifeTime()
        {
            var res = currentTimer / growthParameters.lifetime;
            res = Mathf.Clamp(res, 0.1f, 1f);
            return res;
        }

        protected override void InitializeMeshesData(Mesh bakedMesh, int numBranches)
        {
            meshBuilder.InitializeMeshesDataProcedural(bakedMesh, numBranches, growthParameters.lifetime,
                growthParameters.growthSpeed);
        }

        protected override int GetMaxNumPoints()
        {
            var timePerPoint = ivyParameters.stepSize / growthParameters.growthSpeed;
            var res = Mathf.CeilToInt(growthParameters.lifetime / timePerPoint) * ivyParameters.maxBranches * 2;

            res = 20;

            return res;
        }

        protected override int GetMaxNumLeaves()
        {
            var res = GetMaxNumPoints();

            return res;
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