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
        private static InfoPool lastUsedInfoPool;

        public static bool IsGrowing() => growing;
        public static void SetGrowing(bool value) => growing = value;

        private static void TryContextSwitch(InfoPool infoPool)
        {
            if (lastUsedInfoPool == infoPool) return;
            
            Random.InitState(infoPool.ivyParameters.randomSeed);
            rng = Random.state;

            lastUsedInfoPool = infoPool;
        }

        public static void StartGrowthBranch(InfoPool infoPool, Transform rootTransform, Vector3 firstPoint,
            Vector3 firstGrabVector)
        {
            Assert.IsTrue(infoPool.ivyContainer.branches.Count == 0, "This ivy already has existing branches!");

            TryContextSwitch(infoPool);

            var newBranchContainer = ScriptableObject.CreateInstance<BranchContainer>();
            newBranchContainer.AddBranchPoint(firstPoint, firstGrabVector, true, newBranchContainer.branchNumber);
            newBranchContainer.currentHeight = infoPool.ivyParameters.minDistanceToSurface;
            newBranchContainer.growDirection = Quaternion.AngleAxis(Random.value * 360f, rootTransform.up) * rootTransform.forward;
            infoPool.ivyContainer.firstVertexVector = newBranchContainer.growDirection;
            newBranchContainer.randomizeHeight = Random.Range(4f, 8f);
            CalculateNewHeight(infoPool, newBranchContainer);
            newBranchContainer.branchSense = ChooseBranchSense();

            infoPool.ivyContainer.AddBranch(newBranchContainer);

            Debug.Log($"Initialized new Editor Growth context: {newBranchContainer.branchNumber}. {infoPool}");
        }

        public static void Step(InfoPool infoPool)
        {
            TryContextSwitch(infoPool);
            
            if (infoPool.ivyContainer.branches.Count == 0)
                throw new InvalidOperationException("No branches found. Must SetContext to initialize the growth context!");
            
            if (!IsGrowing())
                throw new InvalidOperationException("Ivy is not currently growing.");
            
            Random.state = rng;

            // prevent collection modified on add branch
            List<BranchContainer> branchesToEnumerate = new(infoPool.ivyContainer.branches);

            foreach (var branch in branchesToEnumerate)
            {
                branch.heightParameter += infoPool.ivyParameters.stepSize;

                // If the branch is not falling (it is clinging to a surface),
                // we calculate the new height for the next point and check for a wall ahead.
                // If it is falling, we calculate the next point of the drop.
                if (branch.falling)
                {
                    CheckFall(infoPool, branch);
                }
                else
                {
                    CalculateNewHeight(infoPool, branch);
                    CheckWall(infoPool, branch);
                }
            }

            rng = Random.state;
        }
        
        // add a branch from an existing `baseBranch` starting from `originBranchPoint`
        public static void AddBranch(InfoPool infoPool, BranchContainer baseBranch, BranchPoint originBranchPoint, Vector3 normal)
        {
            var newBranchContainer = ScriptableObject.CreateInstance<BranchContainer>();

            newBranchContainer.AddBranchPoint(originBranchPoint.point, -normal);

            newBranchContainer.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(baseBranch.growDirection, normal));
            newBranchContainer.randomizeHeight = Random.Range(4f, 8f);
            newBranchContainer.currentHeight = baseBranch.currentHeight;
            newBranchContainer.heightParameter = baseBranch.heightParameter;
            newBranchContainer.branchSense = ChooseBranchSense();
            newBranchContainer.originPointOfThisBranch = originBranchPoint;

            infoPool.ivyContainer.AddBranch(newBranchContainer);

            originBranchPoint.InitBranchInThisPoint(newBranchContainer.branchNumber);
        }

        // Calculates the distance (height) of the branch from the surface geometry.
        // It oscillates between min/max distance using a sine wave to create natural
        // volume, preventing the ivy from looking flat or "painted on."
        private static void CalculateNewHeight(InfoPool infoPool, BranchContainer branch)
        {
            var p = infoPool.ivyParameters;

            // normalize sine wave from [-1, 1] to [0, 1] range
            branch.heightVar = (Mathf.Sin(branch.heightParameter * p.DTSFrequency - 45f) + 1f) / 2f;
            branch.newHeight = Mathf.Lerp(p.minDistanceToSurface, p.maxDistanceToSurface, branch.heightVar);

            // Adds a second layer of higher-frequency variation based on 'randomizeHeight'
            // This adds surface roughness so the loop doesn't look like a perfect sine wave
            var noiseWave = (Mathf.Sin(branch.heightParameter * p.DTSFrequency * branch.randomizeHeight) + 1) / 2f;
            var noiseAmplitude = p.maxDistanceToSurface / 4f * p.DTSRandomness;
            branch.newHeight += noiseWave * noiseAmplitude;

            // Ensure we never clip into the wall or float too far away
            branch.newHeight = Mathf.Clamp(branch.newHeight, p.minDistanceToSurface, p.maxDistanceToSurface);

            branch.deltaHeight = branch.currentHeight - branch.newHeight;
            branch.currentHeight = branch.newHeight;
        }

        private static int ChooseBranchSense() => Random.value < 0.5f ? -1 : 1;

        private static void CheckWall(InfoPool infoPool, BranchContainer branch)
        {
            Vector3 potentialPointPosition = branch.GetLastBranchPoint().point +
                                             branch.growDirection * infoPool.ivyParameters.stepSize +
                                             branch.GetLastBranchPoint().grabVector * branch.deltaHeight;

            var direction = potentialPointPosition - branch.GetLastBranchPoint().point;

            if (!Physics.Raycast(branch.branchPoints[^1].point, direction, 
                    out RaycastHit hit, infoPool.ivyParameters.stepSize * 1.15f,
                    infoPool.ivyParameters.layerMask.value))
            {
                CheckFloor(infoPool, branch, potentialPointPosition, -branch.GetLastBranchPoint().grabVector);
            }
            else
            {
                NewGrowDirectionAfterWall(branch, -branch.GetLastBranchPoint().grabVector, hit.normal);
                AddPoint(infoPool, branch, hit.point, hit.normal);
            }
        }

        //Si no encontramos muro en el paso anterior, entonces buscamos si tenemos suelo. tiramos el rayo y si da positivo, añadimos punto, calculamos growdirection y decimos al sistema que no estamos cayendo. Si por el contrario no 
        //hemos encontrado suelo, intenamos agarrarnos al otro lado de la posible esquina.
        private static void CheckFloor(InfoPool infoPool, BranchContainer branch, Vector3 potentialPointPosition, Vector3 oldSurfaceNormal)
        {
            if (Physics.Raycast(potentialPointPosition, -oldSurfaceNormal, 
                    out RaycastHit hit, branch.currentHeight * 2f, infoPool.ivyParameters.layerMask.value))
            {
                AddPoint(infoPool, branch, hit.point, hit.normal);
                NewGrowDirection(infoPool, branch);
                branch.fallIteration = 0f;
                branch.falling = false;
            }
            else
            {
                if (Random.value < infoPool.ivyParameters.grabProvabilityOnFall)
                {
                    CheckCorner(infoPool, branch, potentialPointPosition, oldSurfaceNormal);
                }
                else
                {
                    AddFallingPoint(infoPool, branch);
                    branch.fallIteration += 1f - infoPool.ivyParameters.stiffness;
                    branch.falling = true;
                    branch.currentHeight = 0f;
                    branch.heightParameter = -45f;
                }
            }
        }

        //Si hábíamos perdido pie, comprobamos si estamos en una esquina e intentamos seguir por el otro lado de lamisma
        private static void CheckCorner(InfoPool infoPool, BranchContainer branch, Vector3 potentialPointPosition, Vector3 oldSurfaceNormal)
        {
            var ray = new Ray(potentialPointPosition + branch.branchPoints[^1].grabVector * 2f *
                branch.currentHeight, -branch.growDirection);
            if (Physics.Raycast(ray, out RaycastHit hit, infoPool.ivyParameters.stepSize * 1.15f,
                    infoPool.ivyParameters.layerMask.value))
            {
                AddPoint(infoPool, branch, potentialPointPosition, oldSurfaceNormal);
                AddPoint(infoPool, branch, hit.point, hit.normal);

                NewGrowDirectionAfterCorner(branch, oldSurfaceNormal, hit.normal);
            }
            else
            {
                AddFallingPoint(infoPool, branch);
                branch.fallIteration += 1f - infoPool.ivyParameters.stiffness;
                branch.falling = true;
                branch.currentHeight = 0f;
                branch.heightParameter = -45f;
            }
        }

        //Este se usa si estamos en una caída. Está la probabilidad de buscar una superficie donde agarrarnos (checkgrabpoint). Si topamos con una superficie se añade punto y se dice al sistema que no estamos cayendo
        private static void CheckFall(InfoPool infoPool, BranchContainer branch)
        {
            var ray = new Ray(branch.branchPoints[^1].point, branch.growDirection);
            if (!Physics.Raycast(ray, out RaycastHit hit, infoPool.ivyParameters.stepSize * 1.15f,
                    infoPool.ivyParameters.layerMask.value))
            {
                if (Random.value < infoPool.ivyParameters.grabProvabilityOnFall)
                {
                    CheckGrabPoint(infoPool, branch);
                }
                else
                {
                    NewGrowDirectionFalling(infoPool, branch);
                    AddFallingPoint(infoPool, branch);
                    branch.fallIteration += 1f - infoPool.ivyParameters.stiffness;
                    branch.falling = true;
                }
            }
            else
            {
                NewGrowDirectionAfterFall(branch, hit.normal);
                AddPoint(infoPool, branch, hit.point, hit.normal);
                branch.fallIteration = 0f;
                branch.falling = false;
            }
        }

        //Con esto tiramos rayos alrededor del último punto buscando una superficie donde agarrarnos.
        private static void CheckGrabPoint(InfoPool infoPool, BranchContainer branch)
        {
            for (var i = 0; i < 6; i++)
            {
                var angle = Mathf.Rad2Deg * 2 * Mathf.PI / 6 * i;
                var ray = new Ray(branch.branchPoints[^1].point + branch.growDirection * infoPool.ivyParameters.stepSize,
                    Quaternion.AngleAxis(angle, branch.growDirection) * branch.GetLastBranchPoint().grabVector);
                if (Physics.Raycast(ray, out RaycastHit hit, infoPool.ivyParameters.stepSize * 2f,
                        infoPool.ivyParameters.layerMask.value))
                {
                    AddPoint(infoPool, branch, hit.point, hit.normal);
                    NewGrowDirectionAfterGrab(branch, hit.normal);
                    branch.fallIteration = 0f;
                    branch.falling = false;
                    break;
                }

                if (i == 5)
                {
                    AddFallingPoint(infoPool, branch);
                    NewGrowDirectionFalling(infoPool, branch);
                    branch.fallIteration += 1f - infoPool.ivyParameters.stiffness;
                    branch.falling = true;
                }
            }
        }

        //Añadimos punto y todo lo que ello conlleva. Está la posibilidad de spawnear una rama
        public static void AddPoint(InfoPool infoPool, BranchContainer branch, Vector3 point, Vector3 normal)
        {
            branch.totalLenght += infoPool.ivyParameters.stepSize;
            branch.heightParameter += infoPool.ivyParameters.stepSize;

            branch.AddBranchPoint(point + normal * branch.currentHeight, -normal);

            //Con este if lo que comprobamos realmente es si estamos en modo procedural o en modo pintado
            if (growing && Random.value < infoPool.ivyParameters.branchProbability && 
                infoPool.ivyContainer.branches.Count < infoPool.ivyParameters.maxBranches) 
                AddBranch(infoPool, branch, branch.GetLastBranchPoint(), normal);

            AddLeaf(infoPool, branch);
        }

        //Añadimos punto y todo lo que ello conlleva. Es ligeramente diferente a AddPoint. Está la posibilidad de spawnear una rama
        private static void AddFallingPoint(InfoPool infoPool, BranchContainer branch)
        {
            var grabVector = branch.rotationOnFallIteration * branch.GetLastBranchPoint().grabVector;

            branch.totalLenght += infoPool.ivyParameters.stepSize;
            branch.AddBranchPoint(branch.branchPoints[^1].point + branch.growDirection * infoPool.ivyParameters.stepSize, 
                grabVector);

            if (Random.value < infoPool.ivyParameters.branchProbability &&
                infoPool.ivyContainer.branches.Count < infoPool.ivyParameters.maxBranches)
                AddBranch(infoPool, branch, branch.GetLastBranchPoint(), -branch.GetLastBranchPoint().grabVector);

            AddLeaf(infoPool, branch);
        }

        // Checks if the branch has reached a growth interval suitable for a new leaf.
        // If the spacing condition is met, it performs a weighted random selection 
        // to pick a leaf type and anchors it to the midpoint of the latest segment.
        private static void AddLeaf(InfoPool infoPool, BranchContainer branch)
        {
            var spacing = infoPool.ivyParameters.leaveEvery + 
                          Random.Range(0, infoPool.ivyParameters.randomLeaveEvery);

            if (branch.branchPoints.Count % spacing == 0)
            {
                var chosenLeaf = 0;
                var maxRoll = -1f;
                var leafCount = infoPool.ivyParameters.leavesPrefabs.Length;

                for (var i = 0; i < leafCount; i++)
                {
                    var currentRoll = Random.Range(0f, infoPool.ivyParameters.leavesProb[i]);
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

                branch.AddLeaf(leafPos, branch.totalLenght, branch.growDirection, 
                    grabDir, chosenLeaf, segmentStart, segmentEnd);
            }
        }

        // Applies sinusoidal noise to the growth direction to simulate organic meandering.
        // Rotates the growth vector around the surface normal (grabVector) using a 
        // sine function based on total length. Projects the result back onto the 
        // plane to ensure the ivy stays attached to the geometry.
        private static void NewGrowDirection(InfoPool infoPool, BranchContainer branch)
        {
            var p = infoPool.ivyParameters;
            var grabVector = branch.GetLastBranchPoint().grabVector;

            // Jitter the noise
            var freqRandomness = 1 + Random.Range(-p.directionRandomness, p.directionRandomness);
            var frequency = branch.branchSense * branch.totalLenght * p.directionFrequency * freqRandomness;

            // Rotate the grab vector by sin noise
            const float noiseStrength = 10f;
            var amplitudeMod = Mathf.Max(p.directionRandomness, 1f);
            var angle = Mathf.Sin(frequency) * p.directionAmplitude * p.stepSize * noiseStrength * amplitudeMod;
            var rotation = Quaternion.AngleAxis(angle, grabVector);
            var newDir = rotation * branch.growDirection;

            branch.growDirection = Vector3.ProjectOnPlane(newDir, grabVector).normalized;
        }

        private static void NewGrowDirectionAfterWall(BranchContainer branch, Vector3 oldSurfaceNormal,
            Vector3 newSurfaceNormal)
        {
            branch.growDirection = Vector3.ProjectOnPlane(oldSurfaceNormal, newSurfaceNormal).normalized;
        }

        private static void NewGrowDirectionFalling(InfoPool infoPool, BranchContainer branch)
        {
            var newGrowDirection = Vector3.Lerp(branch.growDirection, infoPool.ivyParameters.gravity,
                branch.fallIteration / 10f);
            newGrowDirection = Quaternion.AngleAxis(
                Mathf.Sin(branch.branchSense * branch.totalLenght * infoPool.ivyParameters.directionFrequency *
                          (1 + Random.Range(-infoPool.ivyParameters.directionRandomness / 8f,
                              infoPool.ivyParameters.directionRandomness / 8f))) *
                infoPool.ivyParameters.directionAmplitude * infoPool.ivyParameters.stepSize * 5f *
                Mathf.Max(infoPool.ivyParameters.directionRandomness / 8f, 1f),
                branch.GetLastBranchPoint().grabVector) * newGrowDirection;

            newGrowDirection = Quaternion.AngleAxis(
                Mathf.Sin(branch.branchSense * branch.totalLenght * infoPool.ivyParameters.directionFrequency / 2f *
                          (1 + Random.Range(-infoPool.ivyParameters.directionRandomness / 8f,
                              infoPool.ivyParameters.directionRandomness / 8f))) *
                infoPool.ivyParameters.directionAmplitude * infoPool.ivyParameters.stepSize * 5f *
                Mathf.Max(infoPool.ivyParameters.directionRandomness / 8f, 1f),
                Vector3.Cross(branch.GetLastBranchPoint().grabVector, branch.growDirection)) * newGrowDirection;

            branch.rotationOnFallIteration = Quaternion.FromToRotation(branch.growDirection, newGrowDirection);
            branch.growDirection = newGrowDirection;
        }

        private static void NewGrowDirectionAfterFall(BranchContainer branch, Vector3 newSurfaceNormal)
        {
            branch.growDirection =
                Vector3.ProjectOnPlane(-branch.GetLastBranchPoint().grabVector, newSurfaceNormal).normalized;
        }

        private static void NewGrowDirectionAfterGrab(BranchContainer branch, Vector3 newSurfaceNormal)
        {
            branch.growDirection = Vector3.ProjectOnPlane(branch.growDirection, newSurfaceNormal).normalized;
        }

        private static void NewGrowDirectionAfterCorner(BranchContainer branch, Vector3 oldSurfaceNormal,
            Vector3 newSurfaceNormal)
        {
            branch.growDirection = Vector3.ProjectOnPlane(-oldSurfaceNormal, newSurfaceNormal).normalized;
        }
    }
}