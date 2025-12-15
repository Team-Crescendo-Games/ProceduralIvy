using System;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public static class ProceduralIvyCommon
    {
        public static Quaternion CalculateLeafOrientation(
            IvyParameters ivyParameters, 
            Vector3 lastPointForward, 
            Vector3 lastPointUpward, 
            System.Random rng,
            out Vector3 forward,
            out Vector3 left)
        {
            forward = ivyParameters.globalOrientation ? ivyParameters.globalRotation : lastPointForward;
            left = Vector3.Cross(forward, lastPointUpward).normalized;

            // base alignment
            var leafRotation = Quaternion.LookRotation(lastPointUpward, forward);

            // parameter defined offsets
            leafRotation = Quaternion.AngleAxis(ivyParameters.rotation.x, left) *
                           Quaternion.AngleAxis(ivyParameters.rotation.y, lastPointUpward) *
                           Quaternion.AngleAxis(ivyParameters.rotation.z, forward) * leafRotation;

            rng ??= new();
            
            // random jitter
            float rx = (float)(rng.NextDouble() * 2.0 - 1.0) * ivyParameters.randomRotation.x;
            float ry = (float)(rng.NextDouble() * 2.0 - 1.0) * ivyParameters.randomRotation.y;
            float rz = (float)(rng.NextDouble() * 2.0 - 1.0) * ivyParameters.randomRotation.z;

            leafRotation = Quaternion.AngleAxis(rx, left) *
                           Quaternion.AngleAxis(ry, lastPointUpward) *
                           Quaternion.AngleAxis(rz, forward) * leafRotation;
            
            return leafRotation;
        }
        
        // Calculates the distance (height) of the branch from the surface geometry.
        // It oscillates between min/max distance using a sine wave to create natural
        // volume, preventing the ivy from looking flat or "painted on."
        public static void CalculateNewBranchHeight(IvyParameters p, BranchContainer branch)
        {
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
        
        public static void SetGrowDirectionAfterFall(BranchContainer branch, Vector3 newSurfaceNormal)
        {
            branch.growDirection =
                Vector3.ProjectOnPlane(-branch.GetLastBranchPoint().grabVector, newSurfaceNormal).normalized;
        }

        public static void SetGrowDirectionAfterGrab(BranchContainer branch, Vector3 newSurfaceNormal)
        {
            branch.growDirection = Vector3.ProjectOnPlane(branch.growDirection, newSurfaceNormal).normalized;
        }

        public static void SetGrowDirectionAfterCorner(BranchContainer branch, Vector3 oldSurfaceNormal,
            Vector3 newSurfaceNormal)
        {
            branch.growDirection = Vector3.ProjectOnPlane(-oldSurfaceNormal, newSurfaceNormal).normalized;
        }
        
        public static void SetGrowDirectionAfterWall(BranchContainer branch, Vector3 oldSurfaceNormal,
            Vector3 newSurfaceNormal)
        {
            branch.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(oldSurfaceNormal, newSurfaceNormal));
        }
        
        public static int ChooseBranchSense() => UnityEngine.Random.value < 0.5f ? -1 : 1;
        
        // Applies sinusoidal noise to the growth direction to simulate organic meandering.
        // Rotates the growth vector around the surface normal (grabVector) using a 
        // sine function based on total length. Projects the result back onto the 
        // plane to ensure the ivy stays attached to the geometry.
        public static void SetNewGrowDirection(IvyParameters p, BranchContainer branch)
        {
            var grabVector = branch.GetLastBranchPoint().grabVector;

            // Jitter the noise
            var freqRandomness = 1 + UnityEngine.Random.Range(-p.directionRandomness, p.directionRandomness);
            var frequency = branch.branchSense * branch.totalLength * p.directionFrequency * freqRandomness;

            // Rotate the grab vector by sin noise
            const float noiseStrength = 10f;
            var amplitudeMod = Mathf.Max(p.directionRandomness, 1f);
            var angle = Mathf.Sin(frequency) * p.directionAmplitude * p.stepSize * noiseStrength * amplitudeMod;
            var rotation = Quaternion.AngleAxis(angle, grabVector);
            var newDir = rotation * branch.growDirection;

            branch.growDirection = Vector3.ProjectOnPlane(newDir, grabVector).normalized;
        }
        
        public static void SetNewGrowDirectionFalling(IvyParameters p, BranchContainer branch)
        {
            var lastPoint = branch.GetLastBranchPoint();

            // Linearly interpolate towards gravity based on how long the branch has been falling.
            var gravityWeightedDir = Vector3.Lerp(branch.growDirection, p.gravity, branch.fallIteration * 0.1f);

            // Factor out the randomness and amplitude math that is shared between both rotation passes.
            float randFactor = p.directionRandomness * 0.125f; // / 8f
            float freqNoise = 1f + UnityEngine.Random.Range(-randFactor, randFactor);
    
            float angleScalar = p.directionAmplitude * p.stepSize * 5f * Mathf.Max(randFactor, 1f);
            float basePhase = branch.branchSense * branch.totalLength * freqNoise;

            // Primary Rotation (Surface Normal Axis)
            // High frequency "wiggle" relative to the wall it detached from.
            float primaryAngle = Mathf.Sin(basePhase * p.directionFrequency) * angleScalar;
            Quaternion primaryRot = Quaternion.AngleAxis(primaryAngle, lastPoint.grabVector);

            // Secondary Rotation (Perpendicular Axis)
            // Lower frequency "curl" (half freq) to add 3D depth to the spiral.
            float secondaryAngle = Mathf.Sin(basePhase * p.directionFrequency * 0.5f) * angleScalar;
            Vector3 secondaryAxis = Vector3.Cross(lastPoint.grabVector, branch.growDirection);
            Quaternion secondaryRot = Quaternion.AngleAxis(secondaryAngle, secondaryAxis);

            // Apply Rotations
            Vector3 newGrowDirection = secondaryRot * primaryRot * gravityWeightedDir;

            branch.rotationOnFallIteration = Quaternion.FromToRotation(branch.growDirection, newGrowDirection);
            branch.growDirection = newGrowDirection;
        }
    }
}