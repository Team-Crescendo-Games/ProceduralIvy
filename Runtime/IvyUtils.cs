using System;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public static class IvyUtils
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
    }
}