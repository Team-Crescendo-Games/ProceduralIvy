using System;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public class RuntimeGrowthParameters
    {
        public float growthSpeed = 25f;
        
        public float lifetime = 5f;
        
        public bool speedOverLifetimeEnabled = false;
        
        public AnimationCurve speedOverLifetimeCurve = new (
            new Keyframe(0f, 0f), 
            new Keyframe(0.2f, 1f),
            new Keyframe(0.8f, 1f), 
            new Keyframe(1f, 0f));
        
        public float delay = 0f;
        
        public bool startGrowthOnAwake = true;
    }
}