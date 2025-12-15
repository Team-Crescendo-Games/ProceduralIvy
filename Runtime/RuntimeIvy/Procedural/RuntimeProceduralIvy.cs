using System.Linq;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class RuntimeProceduralIvy : RuntimeIvy
    {
        private RuntimeIvyGrowth rtIvyGrowth;

        public override void Initialize(RuntimeGrowthParameters growthParameters, IvyContainer ivyContainer, IvyParameters ivyParameters)
        {
            base.Initialize(growthParameters, ivyContainer, ivyParameters);

            rtIvyGrowth = new RuntimeIvyGrowth(rtIvyContainer, ivyParameters, gameObject, leavesMeshesByChosenLeaf,
                GetMaxNumPoints(), GetMaxNumLeaves(), GetMaxNumVerticesPerLeaf());

            currentLifetime = growthParameters.lifetime;
        }

        protected override void AdvanceBranchGrowth(int branchIndex)
        {
            base.AdvanceBranchGrowth(branchIndex);
            rtIvyGrowth.Step();
        }

        public override bool IsGrowingFinished() => currentTimer > currentLifetime;

        protected override float GetNormalizedLifeTime() => Mathf.Clamp(currentTimer / growthParameters.lifetime, 0.1f, 1);

        protected override void InitializeMeshesData(Mesh bakedMesh, int numBranches)
        {
            meshBuilder.InitializeMeshesDataProcedural(bakedMesh, numBranches, growthParameters.lifetime,
                growthParameters.growthSpeed);
        }

        protected override int GetMaxNumPoints()
        {
            var timePerPoint = ivyParameters.stepSize / growthParameters.growthSpeed;
            var res = Mathf.CeilToInt(growthParameters.lifetime / timePerPoint) * ivyParameters.maxBranches * 2;
            return res;
        }

        protected override int GetMaxNumLeaves() => GetMaxNumPoints();

        private int GetMaxNumVerticesPerLeaf()
        {
            if (leavesMeshesByChosenLeaf.Length == 0) return 0;
            return leavesMeshesByChosenLeaf.Max(m => m.vertices.Length);
        }
    }
}