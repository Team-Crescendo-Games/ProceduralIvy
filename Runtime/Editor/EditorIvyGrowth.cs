using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace TeamCrescendo.ProceduralIvy
{
    public static class EditorIvyGrowth
    {
        private static bool growing;
        private static Random.State rng;
        private static IvyData _lastUsedIvyData;

        public static bool IsGrowing() => growing;
        public static void SetGrowing(bool value) => growing = value;

        private static void TryContextSwitch(IvyData ivyData)
        {
            if (_lastUsedIvyData == ivyData) return;
            
            Random.InitState(ivyData.ivyParameters.randomSeed);
            rng = Random.state;

            _lastUsedIvyData = ivyData;
        }

        public static void StartGrowthBranch(IvyData ivyData, Transform rootTransform, Vector3 firstPoint,
            Vector3 firstGrabVector)
        {
            Assert.IsTrue(ivyData.ivyContainer.branches.Count == 0, "This ivy already has existing branches!");

            TryContextSwitch(ivyData);

            var newBranchContainer = ScriptableObject.CreateInstance<BranchContainer>();
            newBranchContainer.AddBranchPoint(firstPoint, firstGrabVector, true, newBranchContainer.branchNumber);
            newBranchContainer.currentHeight = ivyData.ivyParameters.minDistanceToSurface;
            newBranchContainer.growDirection = Quaternion.AngleAxis(Random.value * 360f, rootTransform.up) * rootTransform.forward;
            ivyData.ivyContainer.firstVertexVector = newBranchContainer.growDirection;
            newBranchContainer.randomizeHeight = Random.Range(4f, 8f);
            ProceduralIvyCommon.CalculateNewBranchHeight(ivyData.ivyParameters, newBranchContainer);
            newBranchContainer.branchSense = ProceduralIvyCommon.ChooseBranchSense();

            ivyData.ivyContainer.AddBranchEditor(newBranchContainer);

            Debug.Log($"Initialized new Editor Growth context: {newBranchContainer.branchNumber}. {ivyData}");
        }

        public static void Step(IvyData ivyData)
        {
            TryContextSwitch(ivyData);
            
            if (ivyData.ivyContainer.branches.Count == 0)
                throw new InvalidOperationException("No branches found. Must SetContext to initialize the growth context!");
            
            if (!IsGrowing())
                throw new InvalidOperationException("Ivy is not currently growing.");
            
            Random.state = rng;

            // prevent collection modified on add branch
            List<BranchContainer> branchesToEnumerate = new(ivyData.ivyContainer.branches);

            foreach (var branch in branchesToEnumerate)
            {
                branch.heightParameter += ivyData.ivyParameters.stepSize;

                // If the branch is not falling (it is clinging to a surface),
                // we calculate the new height for the next point and check for a wall ahead.
                // If it is falling, we calculate the next point of the drop.
                if (branch.falling)
                {
                    CheckFall(ivyData, branch);
                }
                else
                {
                    ProceduralIvyCommon.CalculateNewBranchHeight(ivyData.ivyParameters, branch);
                    CheckWall(ivyData, branch);
                }
            }

            rng = Random.state;
        }
        
        // add a branch from an existing `baseBranch` starting from `originBranchPoint`
        public static void AddBranch(IvyData ivyData, BranchContainer baseBranch, BranchPoint originBranchPoint, Vector3 normal)
        {
            var newBranchContainer = ScriptableObject.CreateInstance<BranchContainer>();

            newBranchContainer.AddBranchPoint(originBranchPoint.point, -normal);

            newBranchContainer.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(baseBranch.growDirection, normal));
            newBranchContainer.randomizeHeight = Random.Range(4f, 8f);
            newBranchContainer.currentHeight = baseBranch.currentHeight;
            newBranchContainer.heightParameter = baseBranch.heightParameter;
            newBranchContainer.branchSense = ProceduralIvyCommon.ChooseBranchSense();
            newBranchContainer.originPointOfThisBranch = originBranchPoint;

            ivyData.ivyContainer.AddBranchEditor(newBranchContainer);

            originBranchPoint.InitBranchInThisPoint(newBranchContainer.branchNumber);
        }

        private static void CheckWall(IvyData ivyData, BranchContainer branch)
        {
            Vector3 potentialPointPosition = branch.GetLastBranchPoint().point +
                                             branch.growDirection * ivyData.ivyParameters.stepSize +
                                             branch.GetLastBranchPoint().grabVector * branch.deltaHeight;

            var direction = potentialPointPosition - branch.GetLastBranchPoint().point;

            if (!Physics.Raycast(branch.branchPoints[^1].point, direction, 
                    out RaycastHit hit, ivyData.ivyParameters.stepSize * 1.15f,
                    ivyData.ivyParameters.layerMask.value))
            {
                CheckFloor(ivyData, branch, potentialPointPosition, -branch.GetLastBranchPoint().grabVector);
            }
            else
            {
                ProceduralIvyCommon.SetGrowDirectionAfterWall(branch, -branch.GetLastBranchPoint().grabVector, hit.normal);
                AddPoint(ivyData, branch, hit.point, hit.normal);
            }
        }

        //Si no encontramos muro en el paso anterior, entonces buscamos si tenemos suelo. tiramos el rayo y si da positivo, añadimos punto, calculamos growdirection y decimos al sistema que no estamos cayendo. Si por el contrario no 
        //hemos encontrado suelo, intenamos agarrarnos al otro lado de la posible esquina.
        private static void CheckFloor(IvyData ivyData, BranchContainer branch, Vector3 potentialPointPosition, Vector3 oldSurfaceNormal)
        {
            if (Physics.Raycast(potentialPointPosition, -oldSurfaceNormal, 
                    out RaycastHit hit, branch.currentHeight * 2f, ivyData.ivyParameters.layerMask.value))
            {
                AddPoint(ivyData, branch, hit.point, hit.normal);
                ProceduralIvyCommon.SetNewGrowDirection(ivyData.ivyParameters, branch);
                branch.fallIteration = 0f;
                branch.falling = false;
            }
            else
            {
                if (Random.value < ivyData.ivyParameters.grabProbabilityOnFall)
                {
                    CheckCorner(ivyData, branch, potentialPointPosition, oldSurfaceNormal);
                }
                else
                {
                    AddFallingPoint(ivyData, branch);
                    branch.fallIteration += 1f - ivyData.ivyParameters.stiffness;
                    branch.falling = true;
                    branch.currentHeight = 0f;
                    branch.heightParameter = -45f;
                }
            }
        }

        //Si hábíamos perdido pie, comprobamos si estamos en una esquina e intentamos seguir por el otro lado de lamisma
        private static void CheckCorner(IvyData ivyData, BranchContainer branch, Vector3 potentialPointPosition, Vector3 oldSurfaceNormal)
        {
            var ray = new Ray(potentialPointPosition + branch.branchPoints[^1].grabVector * 2f *
                branch.currentHeight, -branch.growDirection);
            if (Physics.Raycast(ray, out RaycastHit hit, ivyData.ivyParameters.stepSize * 1.15f,
                    ivyData.ivyParameters.layerMask.value))
            {
                AddPoint(ivyData, branch, potentialPointPosition, oldSurfaceNormal);
                AddPoint(ivyData, branch, hit.point, hit.normal);

                ProceduralIvyCommon.SetGrowDirectionAfterCorner(branch, oldSurfaceNormal, hit.normal);
            }
            else
            {
                AddFallingPoint(ivyData, branch);
                branch.fallIteration += 1f - ivyData.ivyParameters.stiffness;
                branch.falling = true;
                branch.currentHeight = 0f;
                branch.heightParameter = -45f;
            }
        }

        //Este se usa si estamos en una caída. Está la probabilidad de buscar una superficie donde agarrarnos (checkgrabpoint). Si topamos con una superficie se añade punto y se dice al sistema que no estamos cayendo
        private static void CheckFall(IvyData ivyData, BranchContainer branch)
        {
            var ray = new Ray(branch.branchPoints[^1].point, branch.growDirection);
            if (!Physics.Raycast(ray, out RaycastHit hit, ivyData.ivyParameters.stepSize * 1.15f,
                    ivyData.ivyParameters.layerMask.value))
            {
                if (Random.value < ivyData.ivyParameters.grabProbabilityOnFall)
                {
                    CheckGrabPoint(ivyData, branch);
                }
                else
                {
                    ProceduralIvyCommon.SetNewGrowDirectionFalling(ivyData.ivyParameters, branch);
                    AddFallingPoint(ivyData, branch);
                    branch.fallIteration += 1f - ivyData.ivyParameters.stiffness;
                    branch.falling = true;
                }
            }
            else
            {
                ProceduralIvyCommon.SetGrowDirectionAfterFall(branch, hit.normal);
                AddPoint(ivyData, branch, hit.point, hit.normal);
                branch.fallIteration = 0f;
                branch.falling = false;
            }
        }

        //Con esto tiramos rayos alrededor del último punto buscando una superficie donde agarrarnos.
        private static void CheckGrabPoint(IvyData ivyData, BranchContainer branch)
        {
            const float totalSteps = 6;
            const float angleStep = 360f / totalSteps;
            for (var i = 0; i < totalSteps; i++)
            {
                var angle = angleStep * i;
                var ray = new Ray(branch.branchPoints[^1].point + branch.growDirection * ivyData.ivyParameters.stepSize,
                    Quaternion.AngleAxis(angle, branch.growDirection) * branch.GetLastBranchPoint().grabVector);
                if (Physics.Raycast(ray, out RaycastHit hit, ivyData.ivyParameters.stepSize * 2f,
                        ivyData.ivyParameters.layerMask.value))
                {
                    AddPoint(ivyData, branch, hit.point, hit.normal);
                    ProceduralIvyCommon.SetGrowDirectionAfterGrab(branch, hit.normal);
                    branch.fallIteration = 0f;
                    branch.falling = false;
                    break;
                }

                if (i == 5)
                {
                    AddFallingPoint(ivyData, branch);
                    ProceduralIvyCommon.SetNewGrowDirectionFalling(ivyData.ivyParameters, branch);
                    branch.fallIteration += 1f - ivyData.ivyParameters.stiffness;
                    branch.falling = true;
                }
            }
        }

        //Añadimos punto y todo lo que ello conlleva. Está la posibilidad de spawnear una rama
        public static void AddPoint(IvyData ivyData, BranchContainer branch, Vector3 point, Vector3 normal)
        {
            branch.totalLength += ivyData.ivyParameters.stepSize;
            branch.heightParameter += ivyData.ivyParameters.stepSize;

            branch.AddBranchPoint(point + normal * branch.currentHeight, -normal);

            //Con este if lo que comprobamos realmente es si estamos en modo procedural o en modo pintado
            if (growing && Random.value < ivyData.ivyParameters.branchProbability && 
                ivyData.ivyContainer.branches.Count < ivyData.ivyParameters.maxBranches) 
                AddBranch(ivyData, branch, branch.GetLastBranchPoint(), normal);

            if (ivyData.ivyParameters.generateLeaves)
                AddLeaf(ivyData, branch);
        }

        //Añadimos punto y todo lo que ello conlleva. Es ligeramente diferente a AddPoint. Está la posibilidad de spawnear una rama
        private static void AddFallingPoint(IvyData ivyData, BranchContainer branch)
        {
            var grabVector = branch.rotationOnFallIteration * branch.GetLastBranchPoint().grabVector;

            branch.totalLength += ivyData.ivyParameters.stepSize;
            branch.AddBranchPoint(branch.branchPoints[^1].point + branch.growDirection * ivyData.ivyParameters.stepSize, 
                grabVector);

            if (Random.value < ivyData.ivyParameters.branchProbability &&
                ivyData.ivyContainer.branches.Count < ivyData.ivyParameters.maxBranches)
                AddBranch(ivyData, branch, branch.GetLastBranchPoint(), -branch.GetLastBranchPoint().grabVector);

            if (ivyData.ivyParameters.generateLeaves)
                AddLeaf(ivyData, branch);
        }

        // Checks if the branch has reached a growth interval suitable for a new leaf.
        // If the spacing condition is met, it performs a weighted random selection 
        // to pick a leaf type and anchors it to the midpoint of the latest segment.
        private static void AddLeaf(IvyData ivyData, BranchContainer branch)
        {
            var spacing = ivyData.ivyParameters.leaveEvery + 
                          Random.Range(0, ivyData.ivyParameters.randomLeaveEvery);

            if (branch.branchPoints.Count % spacing == 0)
            {
                var chosenLeaf = 0;
                var maxRoll = -1f;
                var leafCount = ivyData.ivyParameters.leavesPrefabs.Length;

                for (var i = 0; i < leafCount; i++)
                {
                    var currentRoll = Random.Range(0f, ivyData.ivyParameters.leavesProb[i]);
                    if (currentRoll > maxRoll)
                    {
                        maxRoll = currentRoll;
                        chosenLeaf = i;
                    }
                }

                var segmentStart = branch.branchPoints[^2];
                var segmentEnd = branch.branchPoints[^1];
                var leafPos = Vector3.Lerp(segmentStart.point, segmentEnd.point, 0.5f);
                var grabDir = -branch.GetLastBranchPoint().grabVector;

                branch.AddLeafEditor(leafPos, branch.totalLength, branch.growDirection, 
                    grabDir, chosenLeaf, segmentStart, segmentEnd);
            }
        }
    }
}