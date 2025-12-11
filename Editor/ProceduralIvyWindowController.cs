using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TeamCrescendo.ProceduralIvy
{
    public class ProceduralIvyWindowController 
    {
        public IvyInfo currentIvyInfo;
        public InfoPool infoPool;
        public GameObject ivyGO;
        public MeshFilter mf;
        public MeshRenderer mr;

        public IvyPreset selectedPreset;

        private IvyParametersGUI ivyParametersGUI;
        public static event Action OnIvyGoCreated;

        public void Init(IvyParametersGUI ivyParametersGUI)
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            this.ivyParametersGUI = ivyParametersGUI;
        }

        public void Cleanup()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        public InfoPool CreateIvyDataObject() => CreateIvyDataObject(new IvyParameters(ivyParametersGUI));

        private InfoPool CreateIvyDataObject(IvyParameters ivyParameters)
        {
            infoPool = ScriptableObject.CreateInstance<InfoPool>();
            
            infoPool.ivyContainer = ScriptableObject.CreateInstance<IvyContainer>();
            infoPool.ivyContainer.branches = new List<BranchContainer>();

            infoPool.ivyParameters = ivyParameters;

            infoPool.growth = ScriptableObject.CreateInstance<EditorIvyGrowth>();
            infoPool.growth.infoPool = infoPool;

            infoPool.meshBuilder = ScriptableObject.CreateInstance<EditorMeshBuilder>();
            infoPool.meshBuilder.infoPool = infoPool;
            infoPool.meshBuilder.ivyMesh = new Mesh();

            ProceduralIvyWindow.SceneGuiController.infoPool = infoPool;

            return infoPool;
        }

        public InfoPool CreateIvyDataObject(IvyPreset selectedPreset)
        {
            this.selectedPreset = selectedPreset;

            var parameters = new IvyParameters(selectedPreset);
            parameters.DeepCopy(selectedPreset);

            return CreateIvyDataObject(parameters);
        }

        public void ModifyIvy(IvyInfo ivyInfo)
        {
            currentIvyInfo = ivyInfo;
            selectedPreset = ivyInfo.originalPreset;

            infoPool = ivyInfo.infoPool;
            infoPool.ivyContainer.ivyGO = ivyInfo.gameObject;

            mf = ivyInfo.meshFilter;
            mr = ivyInfo.meshRenderer;
            ivyGO = ivyInfo.gameObject;

            infoPool.growth.growing = false;

            infoPool.ivyParameters.branchesMaterial = mr.sharedMaterials[0];

            ivyParametersGUI.CopyFrom(infoPool.ivyParameters);

            infoPool.meshBuilder.InitLeavesData();
        }

        public bool StartIvy(Vector3 firstPoint, Vector3 firstGrabVector)
        {
            if (infoPool.ivyContainer.branches.Count == 0)
            {
                infoPool.growth.Initialize(firstPoint, firstGrabVector);
                infoPool.meshBuilder.InitLeavesData();
                infoPool.meshBuilder.InitializeMeshBuilder();
                return true;
            }

            return false;
        }

        public void ResetIvy()
        {
            infoPool.ivyContainer.Clear();
        }

        public void GenerateLMUVs()
        {
            infoPool.meshBuilder.GenerateLMUVs();
        }

        public void RefreshMesh()
        {
            if (ivyGO)
            {
                infoPool.meshBuilder.InitLeavesData();
                infoPool.meshBuilder.BuildGeometry();

                var newMaterials = mr.sharedMaterials;
                newMaterials[0] = infoPool.ivyParameters.branchesMaterial;
                mr.sharedMaterials = newMaterials;

                mf.mesh = infoPool.meshBuilder.ivyMesh;
            }
        }

        public void Update()
        {
            if (infoPool != null && infoPool.growth != null && infoPool.growth.growing)
            {
                if (!IsVertexLimitReached())
                {
                    infoPool.growth.Step();
                    RefreshMesh();
                }
                else
                {
                    if (infoPool.ivyParameters.buffer32Bits)
                        Debug.LogWarning("Vertices limit reached at " + Constants.VERTEX_LIMIT_32 + ".", infoPool.ivyContainer.ivyGO);
                    else
                        Debug.LogWarning("Vertices limit reached at " + Constants.VERTEX_LIMIT_16 + ".", infoPool.ivyContainer.ivyGO);
                    infoPool.growth.growing = false;
                }
            }
        }

        private bool IsVertexLimitReached()
        {
            var numVertices = infoPool.meshBuilder.verts.Length + infoPool.ivyParameters.sides + 1;
            int limit;
            if (infoPool.ivyParameters.buffer32Bits)
                limit = Constants.VERTEX_LIMIT_32;
            else
                limit = Constants.VERTEX_LIMIT_16;
            var res = numVertices >= limit;
            return res;
        }

        public void SaveIvy()
        {
            Undo.IncrementCurrentGroup();
            infoPool.ivyContainer.RecordUndo();
        }

        public void RegisterUndo()
        {
            if (infoPool) Undo.RegisterCompleteObjectUndo(infoPool, "Ivy Parameter Change");
        }

        public void CreateIvyGO(Vector3 position, Vector3 normal)
        {
            ivyGO = new GameObject();
            ivyGO.transform.position = position + normal * infoPool.ivyParameters.minDistanceToSurface;
            ivyGO.transform.rotation = Quaternion.LookRotation(normal);
            ivyGO.transform.RotateAround(ivyGO.transform.position, ivyGO.transform.right, 90f);
            ivyGO.name = "New Ivy";

            infoPool.ivyContainer.ivyGO = ivyGO;
            infoPool.growth.origin = ivyGO.transform.position;

            mr = ivyGO.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new [] {infoPool.ivyParameters.branchesMaterial};

            mf = ivyGO.AddComponent<MeshFilter>();

            var ivyInfo = ivyGO.AddComponent<IvyInfo>();
            ivyInfo.Setup(infoPool, mf, mr, selectedPreset);

            ModifyIvy(ivyInfo);

            infoPool.ivyContainer.RecordCreated();
            
            OnIvyGoCreated?.Invoke();
        }

        public void SaveCurrentIvyIntoScene()
        {
            RemoveAllScripts();
            OnSelectionChanged();
        }

        private GameObject RemoveAllScripts()
        {
            if (infoPool.ivyParameters.generateLightmapUVs) infoPool.meshBuilder.GenerateLMUVs();
            var componentsToDelete = new List<MonoBehaviour>();
            componentsToDelete.AddRange(ivyGO.GetComponentsInChildren<MonoBehaviour>());

            for (var i = 0; i < componentsToDelete.Count; i++) 
                Object.DestroyImmediate(componentsToDelete[i]);
            
            var go = Object.Instantiate(infoPool.ivyContainer.ivyGO);
            go.name = go.name.Remove(go.name.Length - 7, 7);
            
            Object.DestroyImmediate(infoPool.ivyContainer.ivyGO);
            return go;
        }

        public void SaveCurrentIvyAsPrefab(string fileName)
        {
            var filePath = EditorUtility.SaveFilePanelInProject("Save Ivy as prefab", fileName, "prefab", "");
            fileName = Path.GetFileName(filePath);
            var separator = new[] { "." };
            fileName = fileName.Split(separator, StringSplitOptions.None)[0];
            if (filePath.Length > 0)
            {
                var initIndexFileName = filePath.LastIndexOf("/");
                var localFolderPath = filePath.Substring(0, initIndexFileName);

                var infoPoolFilePath = Path.Combine(localFolderPath, fileName + ".asset");
                AssetDatabase.CreateAsset(infoPool, infoPoolFilePath);
                AssetDatabase.AddObjectToAsset(infoPool.ivyContainer, infoPool);
                AssetDatabase.AddObjectToAsset(infoPool.meshBuilder, infoPool);
                AssetDatabase.AddObjectToAsset(infoPool.growth, infoPool);
                AssetDatabase.AddObjectToAsset(infoPool.meshBuilder.ivyMesh, infoPool);

                for (var i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                    AssetDatabase.AddObjectToAsset(infoPool.ivyContainer.branches[i], infoPool);

                var go = RemoveAllScripts();

                var prefabSaved = PrefabUtility.SaveAsPrefabAssetAndConnect(go, filePath, InteractionMode.AutomatedAction);

                EditorGUIUtility.PingObject(prefabSaved);
            }
        }

        private void OnSelectionChanged()
        {
            var activeGameObject = Selection.activeGameObject;

            if (activeGameObject != null)
            {
                var ivyInfo = activeGameObject.GetComponent<IvyInfo>();

                if (ivyInfo != null)
                    ModifyIvy(ivyInfo);
                else
                    currentIvyInfo = null;
            }
            else
            {
                if (ivyGO == null)
                {
                    if (infoPool != null && infoPool.ivyContainer != null) 
                        infoPool.ivyContainer.Clear();
                    
                    CreateIvyDataObject();
                }

                currentIvyInfo = null;
            }
        }

        public void OnVinesMaterialChanged(Material newMaterial)
        {
            ProceduralIvyWindow.Instance.valueUpdated = true;
        }

        public void OnPresetChanged(IvyPreset newPreset)
        {
            if (newPreset)
            {
                selectedPreset = newPreset;
                if (currentIvyInfo != null) currentIvyInfo.originalPreset = selectedPreset;

                var presetGUID = UIUtils.GetGUIDByAsset(selectedPreset);
                EditorPrefs.SetString("RealIvyDefaultGUID", presetGUID);

                ivyParametersGUI.CopyFrom(selectedPreset);
                ProceduralIvyWindow.Instance.SaveParameters();
                infoPool.meshBuilder.InitLeavesData();
                ProceduralIvyWindow.Instance.valueUpdated = true;
            }
        }

        public void SaveCurrentParametersAsNewPreset(string filePath)
        {
            var newPreset = ScriptableObject.CreateInstance<IvyPreset>();
            newPreset.ivyParameters = new IvyParameters(infoPool.ivyParameters);

            AssetDatabase.CreateAsset(newPreset, filePath);
            AssetDatabase.SaveAssets();

            OnPresetChanged(newPreset);
        }

        public void OverridePreset()
        {
            selectedPreset.CopyFrom(ivyParametersGUI);

            EditorUtility.SetDirty(selectedPreset);
            AssetDatabase.SaveAssets();

            OnPresetChanged(selectedPreset);
        }

        public void OnScriptReloaded(IvyPreset selectedPreset)
        {
            if (Selection.activeGameObject == null)
            {
                CreateIvyDataObject(selectedPreset);
            }
            else
            {
                var selectedIvyInfo = Selection.activeGameObject.GetComponent<IvyInfo>();
                if (selectedIvyInfo != null) ModifyIvy(selectedIvyInfo);
            }
        }

        public bool AreThereUnsavedChanges() => !infoPool.ivyParameters.IsEqualTo(selectedPreset.ivyParameters);

        public bool GenerateLightmapUVsActivated()
            => infoPool.ivyParameters.generateLightmapUVs != ivyParametersGUI.generateLightmapUVs &&
               ivyParametersGUI.generateLightmapUVs;

        public void PrepareRuntimeBaked()
        {
            var rtIvy = ivyGO.GetComponent<RuntimeIvy>();
            var rtBakedIvy = (RuntimeBakedIvy)rtIvy;
            var defaultGrowthParameters = new RuntimeGrowthParameters();
            var ivyController = ivyGO.GetComponent<IvyController>();

            if (rtIvy == null)
            {
                rtBakedIvy = ivyGO.GetComponent<RuntimeBakedIvy>();
                if (rtBakedIvy == null) 
                    rtBakedIvy = ivyGO.AddComponent<RuntimeBakedIvy>();

                if (ivyController == null) 
                    ivyController = ivyGO.AddComponent<IvyController>();

                ivyController.runtimeIvy = rtBakedIvy;
                ivyController.ivyContainer = currentIvyInfo.infoPool.ivyContainer;
                ivyController.ivyParameters = currentIvyInfo.infoPool.ivyParameters;
                ivyController.growthParameters = defaultGrowthParameters;

                if (rtBakedIvy.mrProcessedMesh == null)
                {
                    var processedMesh = new GameObject();
                    processedMesh.name = "processedMesh";
                    processedMesh.transform.parent = rtBakedIvy.transform;
                    processedMesh.transform.localPosition = Vector3.zero;
                    processedMesh.transform.localRotation = Quaternion.identity;
                    var mrProcessedMesh = processedMesh.AddComponent<MeshRenderer>();
                    var mfProcessedMesh = processedMesh.AddComponent<MeshFilter>();

                    rtBakedIvy.mrProcessedMesh = mrProcessedMesh;
                    rtBakedIvy.mfProcessedMesh = mfProcessedMesh;
                }
            }

            ivyController.ivyParameters = currentIvyInfo.infoPool.ivyParameters;
        }

        public void PrepareRuntimeProcedural()
        {
            var rtIvy = ivyGO.GetComponent<RuntimeIvy>();

            if (rtIvy == null)
            {
                var rtProceduralIvy = ivyGO.GetComponent<RuntimeProceduralIvy>();
                if (rtProceduralIvy == null) rtProceduralIvy = ivyGO.AddComponent<RuntimeProceduralIvy>();

                var ivyController = ivyGO.GetComponent<IvyController>();
                if (ivyController == null) ivyController = ivyGO.AddComponent<IvyController>();

                ivyController.runtimeIvy = rtProceduralIvy;
                ivyController.ivyContainer = currentIvyInfo.infoPool.ivyContainer;
                ivyController.ivyParameters = currentIvyInfo.infoPool.ivyParameters;
                ivyController.growthParameters = new RuntimeGrowthParameters();

                if (rtProceduralIvy.mfProcessedMesh == null)
                {
                    var processedMesh = new GameObject();
                    processedMesh.name = "processedMesh";
                    processedMesh.transform.parent = rtProceduralIvy.transform;
                    processedMesh.transform.localPosition = Vector3.zero;
                    processedMesh.transform.localRotation = Quaternion.identity;
                    var mrProcessedMesh = processedMesh.AddComponent<MeshRenderer>();
                    var mfProcessedMesh = processedMesh.AddComponent<MeshFilter>();

                    rtProceduralIvy.mfProcessedMesh = mfProcessedMesh;
                    rtProceduralIvy.mrProcessedMesh = mrProcessedMesh;
                }
            }
        }

        public void Optimize()
        {
            for (var b = 0; b < infoPool.ivyContainer.branches.Count; b++)
            {
                var branch = infoPool.ivyContainer.branches[b];
                for (var p = 1; p < branch.branchPoints.Count - 1; p++)
                {
                    var segment1 = branch.branchPoints[p].point - branch.branchPoints[p - 1].point;
                    var segment2 = branch.branchPoints[p + 1].point - branch.branchPoints[p].point;
                    if (Vector3.Angle(segment1, segment2) < infoPool.ivyParameters.optAngleBias)
                    {
                        SaveIvy();
                        branch.RemoveBranchPoint(p);
                        RefreshMesh();
                    }
                }
            }
        }
    }
}