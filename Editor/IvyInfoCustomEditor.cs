using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [CustomEditor(typeof(IvyInfo))]
    public class IvyInfoCustomEditor : Editor
    {
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
                    $"Est. Data Memory: {currentStats.memoryKB:F2} KB",
                    MessageType.Info);
            }
        }
    }
}