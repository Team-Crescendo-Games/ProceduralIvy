using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [CustomEditor(typeof(IvyInfo))]
    public class IvyInfoCustomEditor : Editor
    {
        private IvyInfo ivyInfo;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Edit in Real Ivy Editor"))
            {
                var ivyInfo = (IvyInfo)target;
                ProceduralIvyWindow.Init();
                ProceduralIvyWindow.Controller.ModifyIvy(ivyInfo);
            }
        }
    }
}