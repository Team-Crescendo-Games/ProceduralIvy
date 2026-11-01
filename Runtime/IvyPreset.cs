using System;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public class IvyPreset : ScriptableObject
    {
        public IvyParameters ivyParameters;

        private void OnValidate() => ivyParameters.Validate();
    }
}