using System;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [Serializable]
    public class IvyPreset : ScriptableObject
    {
        public IvyParameters ivyParameters;

        public void CopyFrom(IvyParametersGUI copyFrom)
        {
            ivyParameters.DeepCopy(copyFrom);
        }

#if UNITY_EDITOR
        [ContextMenu("Show GUID")]
        public void ShowGUID()
        {
            var res = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
            Debug.Log("GUID: " + res);
        }
#endif
    }
}