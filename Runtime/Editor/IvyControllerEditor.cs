using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    [CustomEditor(typeof(IvyController))]
    public class IvyControllerEditor : Editor
    {
        private const string STR_BAKED_IVY = "Baked Ivy";
        private const string STR_PROCEDURAL_IVY = "Procedural Ivy";
        private SerializedProperty spDelay;

        private SerializedProperty spGrowthParameters;
        private SerializedProperty spGrowthSpeed;
        private SerializedProperty spLifetime;
        private SerializedProperty spSpeedOverLifetimeCurve;
        private SerializedProperty spSpeedOverLifetimeEnabled;
        private SerializedProperty spStartGrowthOnAwake;

        private void OnEnable()
        {
            RefreshSerializedProperties();
        }

        private void RefreshSerializedProperties()
        {
            spGrowthParameters = serializedObject.FindProperty("growthParameters");
            spDelay = spGrowthParameters.FindPropertyRelative("delay");
            spGrowthSpeed = spGrowthParameters.FindPropertyRelative("growthSpeed");
            spLifetime = spGrowthParameters.FindPropertyRelative("lifetime");
            spSpeedOverLifetimeEnabled = spGrowthParameters.FindPropertyRelative("speedOverLifetimeEnabled");
            spSpeedOverLifetimeCurve = spGrowthParameters.FindPropertyRelative("speedOverLifetimeCurve");
            spStartGrowthOnAwake = spGrowthParameters.FindPropertyRelative("startGrowthOnAwake");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var ivyController = (IvyController)target;
            var growthParameters = ivyController.growthParameters;

            GUILayout.Space(10f);

            if (ivyController.rtIvy is RuntimeProceduralIvy)
                EditorGUILayout.LabelField(STR_PROCEDURAL_IVY);
            else if (ivyController.rtIvy is RuntimeBakedIvy) EditorGUILayout.LabelField(STR_BAKED_IVY);

            GUILayout.Space(10f);

            EditorGUILayout.PropertyField(spGrowthSpeed);

            if (ivyController.rtIvy is RuntimeProceduralIvy) EditorGUILayout.PropertyField(spLifetime);

            EditorGUILayout.PropertyField(spDelay);

            GUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(spSpeedOverLifetimeEnabled);
            if (spSpeedOverLifetimeEnabled.boolValue)
                EditorGUILayout.PropertyField(spSpeedOverLifetimeCurve, GUIContent.none);
            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(spStartGrowthOnAwake);

            serializedObject.ApplyModifiedProperties();
        }
    }
}