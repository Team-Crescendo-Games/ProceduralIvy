using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [CustomEditor(typeof(IvyInfo))]
    public class IvyInfoCustomEditor : Editor
    {
        public static string FormatBytes(long bytes)
        {
            if (bytes > 1024 * 1024) return $"{(bytes / (1024f * 1024f)):F2} MB";
            if (bytes > 10 * 1024) return $"{(bytes / 1024f):F2} KB";
            return $"{bytes} Bytes";
        }
        
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledGroupScope(true))
                base.OnInspectorGUI();
            
            var ivyInfo = (IvyInfo)target;

            if (GUILayout.Button("Edit in Real Ivy Editor"))
            {
                ProceduralIvyEditorWindow.Init();
            }

            if (ivyInfo.infoPool != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("Memory Profiling", EditorStyles.boldLabel);

                var currentStats = ivyInfo.infoPool.GetMemoryStats();
                
                EditorGUILayout.HelpBox(
                    $"Total Branches: {currentStats.branchCount}\n" +
                    $"Total Segments: {currentStats.pointCount}\n" +
                    $"Total Leaves: {currentStats.leafCount}\n" +
                    $"Generated Vertices: {currentStats.vertexCount}\n" +
                    $"Est. Data Memory: {FormatBytes(currentStats.memoryBytes)}",
                    MessageType.Info);
            }
        }
    }
}