#if UNITY_EDITOR
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [CreateAssetMenu(fileName = "ProceduralIvyResources", menuName = "Team Crescendo/Procedural Ivy Resources")]
    public class ProceduralIvyResources : ScriptableObject
    {
        [Header("Tool Icons")]
        public Texture2D paintTool;
        public Texture2D moveTool;
        public Texture2D smoothTool;
        public Texture2D refineTool;
        public Texture2D optimizeTool;
        public Texture2D cutTool;
        public Texture2D deleteTool;
        public Texture2D shaveTool;
        public Texture2D addLeavesTool;

        // Singleton-like access for Editor use
        private static ProceduralIvyResources _instance;
        public static ProceduralIvyResources Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Tries to find the asset in the project automatically
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ProceduralIvyResources");
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ProceduralIvyResources>(path);
                    }
                    else
                    {
                        Debug.LogError("ProceduralIvyResources asset not found! Please create one in the Resources folder or via CreateAssetMenu.");
                    }
                }
                return _instance;
            }
        }
    }
}
#endif