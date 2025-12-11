using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TeamCrescendo.ProceduralIvy
{
    public class ProceduralIvyWindow : EditorWindow
    {
        private const string KEY_WINDOW_OPENED = "ProceduralIvyWindow_Opened";

        // Singleton & Sub-systems
        public static ProceduralIvyWindow Instance;
        public static ProceduralIvySceneGui SceneGuiController;
        public static IvyParametersGUI ivyParametersGUI;
        
        public IvyInfo currentIvyInfo;
        public InfoPool infoPool;
        public GameObject ivyGO;
        public IvyPreset selectedPreset;
        public static event Action OnIvyGoCreated;

        // GUI Resources
        public static GUISkin windowSkin;
        public static Texture2D downArrowTex, materialTex, leaveTex, presetTex, infoTex;
        public GUISkin oldSkin;

        // UI State
        public bool placingSeed;
        public Vector2 generalScrollView;
        public float YSpace;
        public Rect generalArea;
        public Color bckgColor = new(0.45f, 0.45f, 0.45f);
        public Color bckgColor2 = new(0.40f, 0.40f, 0.40f);
        public Vector2 leavesPrefabsScrollView;
        public bool valueUpdated;
        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // Parameter Updating Logic
        public bool updatingValue;
        public float originalUpdatingValue;
        public float updatingValueMultiplier;
        public float mouseStartPoint;
        public int presetSelected;
        public List<int> presetsChanged = new();
        public IvyParameter updatingParameter;

        // UI Zones
        private readonly UIZone_BranchesSettings branchesSettingsZone = new();
        private readonly UIZone_GeneralSettings generalSettingsZone = new();
        private readonly UIZone_GrowthSettings growthSettings = new();
        private readonly UIZone_LeavesSettings leavesSettingsZone = new();
        private readonly UIZone_MainButtons mainButtonsZone = new();

        [MenuItem("Tools/Team Crescendo/Procedural Ivy")]
        public static void Init()
        {
            if (Instance != null)
                return;
            
            // Initialize Self
            Instance = (ProceduralIvyWindow)GetWindow(typeof(ProceduralIvyWindow));
            Instance.minSize = new Vector2(450f, 455f);
            Instance.titleContent = new GUIContent("Procedural Ivy");

            ivyParametersGUI = CreateInstance<IvyParametersGUI>();

            Selection.selectionChanged -= Instance.OnSelectionChanged;
            Selection.selectionChanged += Instance.OnSelectionChanged;

            // Initialize GUI View
            if (SceneGuiController != null)
                SceneGuiController.Cleanup();
            SceneGuiController = new ProceduralIvySceneGui();
            
            // Create initial data
            InfoPool dataObject = Instance.CreateIvyDataObject(ProceduralIvyResources.Instance.defaultPreset);
            ivyParametersGUI.CopyFrom(dataObject.ivyParameters);
            
            // Cosmetics
            var res = ProceduralIvyResources.Instance;
            Assert.IsNotNull(res);
            windowSkin = res.windowSkin;
            downArrowTex = res.arrowDown;
            materialTex = res.materialIcon;
            leaveTex = res.leafIcon;
            presetTex = res.presetIcon;
            infoTex = res.infoIcon;
            
            // Callbacks
            SceneView.duringSceneGui -= OnDrawSceneGui;
            SceneView.duringSceneGui += OnDrawSceneGui;
            Undo.undoRedoPerformed -= OnUndoPerformed;
            Undo.undoRedoPerformed += OnUndoPerformed;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            
            EditorPrefs.SetBool(KEY_WINDOW_OPENED, true);
            
            Debug.Log("Procedural Ivy Window initialized");
        }

        private void Update()
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

        private void OnDestroy()
        {
            if (SceneGuiController != null)
            {
                SceneGuiController.Cleanup();
                SceneGuiController = null;
            }

            Selection.selectionChanged -= OnSelectionChanged;

            SceneView.RepaintAll();
            
            SceneView.duringSceneGui -= OnDrawSceneGui;
            Undo.undoRedoPerformed -= OnUndoPerformed;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;

            EditorPrefs.SetBool(KEY_WINDOW_OPENED, false);
            
            Debug.Log("Procedural Ivy Window destroyed");
            
            Instance = null;
        }

        #region GUI

        private void OnGUI()
        {
            if (Instance == null) 
                Init();
            
            oldSkin = GUI.skin;
            GUI.skin = windowSkin;

            EditorGUI.BeginChangeCheck();
            DrawMainGUI();

            if (EditorGUI.EndChangeCheck() || valueUpdated)
            {
                if (GenerateLightmapUVsActivated())
                    CustomDisplayDialog.Init(windowSkin, EditorConstants.LIGHTMAP_UVS_WARNING, "Lightmap UVs warning",
                        infoTex, 370f, 155f, null);
                
                valueUpdated = false;
                SaveParameters();
                RefreshMesh();
                Repaint();
            }

            GUI.skin = oldSkin;
        }
        
        private void DrawMainGUI()
        {
            EditorGUI.DrawRect(new Rect(0f, 0f, position.width, position.height), bckgColor);
            generalScrollView = GUI.BeginScrollView(new Rect(0f, 0f, position.width, position.height),
                generalScrollView, new Rect(0f, 0f, position.width - 17f, YSpace), false, false);

            float generalAreaWidth;
            if (YSpace > position.height)
                generalAreaWidth = position.width - 34f;
            else
                generalAreaWidth = position.width - 20f;

            YSpace = 0f;

            var presetDropDownYSpace = 0f;

            // Updated to pass 'this' instead of controller
            mainButtonsZone.DrawZone(this, ivyParametersGUI, windowSkin, ref YSpace, generalArea,
                bckgColor2);

            generalArea = new Rect(10f, 10f, generalAreaWidth, 520f);

            var generalSettingsYSpace = 0f;

            generalSettingsZone.DrawZone("General settings", 265f, ivyParametersGUI, windowSkin, 
                ref YSpace, ref presetDropDownYSpace, ref generalSettingsYSpace, generalArea, bckgColor2, animationCurve);

            var branchesAreaYSpace = 0f;

            branchesSettingsZone.DrawZone("Branches settings", 185f, ivyParametersGUI, windowSkin, 
                ref YSpace, ref presetDropDownYSpace, ref branchesAreaYSpace, generalArea, bckgColor2, animationCurve);

            var leavesAreaYSpace = 0f;

            leavesSettingsZone.DrawZone("Leaves settings", 230f, ivyParametersGUI, windowSkin,
                ref YSpace, ref presetDropDownYSpace, ref leavesAreaYSpace, generalArea, bckgColor2, animationCurve);

            var growthAreaYSpace = 0f;

            growthSettings.DrawZone("Growth settings", 260f, ivyParametersGUI, windowSkin,
                ref YSpace, ref presetDropDownYSpace, ref growthAreaYSpace,
                generalArea, bckgColor2, animationCurve);

            GUI.EndScrollView();

            if (updatingValue) UpdateValue();
        }
        
        private void UpdateValue()
        {
            var evt = Event.current;
            if (updatingValue && evt != null)
            {
                switch (evt.rawType)
                {
                    case EventType.MouseUp:
                        updatingValue = false;
                        break;
                }
            }

            var delta = GUIUtility.GUIToScreenPoint(Event.current.mousePosition).x - mouseStartPoint;
            var value = originalUpdatingValue + delta * updatingValueMultiplier;

            updatingParameter.UpdateValue(value);
            valueUpdated = true;
            Repaint();
        }
        
        #endregion

        #region Create Ivy
        
        public InfoPool CreateIvyDataObject() => CreateIvyDataObject(new IvyParameters(ivyParametersGUI));

        private InfoPool CreateIvyDataObject(IvyParameters ivyParameters)
        {
            infoPool = CreateInstance<InfoPool>();
            
            infoPool.ivyContainer = CreateInstance<IvyContainer>();
            infoPool.ivyContainer.branches = new List<BranchContainer>();

            infoPool.ivyParameters = ivyParameters;

            infoPool.growth = CreateInstance<EditorIvyGrowth>();
            infoPool.growth.infoPool = infoPool;

            infoPool.meshBuilder = CreateInstance<EditorMeshBuilder>();
            infoPool.meshBuilder.infoPool = infoPool;
            infoPool.meshBuilder.ivyMesh = new Mesh();

            return infoPool;
        }

        private InfoPool CreateIvyDataObject(IvyPreset preset)
        {
            selectedPreset = preset;
            var parameters = new IvyParameters(preset);
            parameters.DeepCopy(preset);
            return CreateIvyDataObject(parameters);
        }
        
        #endregion
        
        #region Scene GUI Helpers
        
        public void CreateIvyGO(Vector3 position, Vector3 normal)
        {
            ivyGO = new GameObject();
            ivyGO.transform.position = position + normal * infoPool.ivyParameters.minDistanceToSurface;
            ivyGO.transform.rotation = Quaternion.LookRotation(normal);
            ivyGO.transform.RotateAround(ivyGO.transform.position, ivyGO.transform.right, 90f);
            ivyGO.name = "New Ivy";

            infoPool.ivyContainer.ivyGO = ivyGO;
            infoPool.growth.origin = ivyGO.transform.position;

            var mr = ivyGO.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new [] {infoPool.ivyParameters.branchesMaterial};

            ivyGO.AddComponent<MeshFilter>();

            var ivyInfo = ivyGO.AddComponent<IvyInfo>();
            ivyInfo.Setup(infoPool, selectedPreset);

            ModifyIvy(ivyInfo);

            infoPool.ivyContainer.RecordCreated();
            
            Selection.activeGameObject = ivyGO;
            AssignLabel(ivyGO);

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
                DestroyImmediate(componentsToDelete[i]);
            
            var go = Instantiate(infoPool.ivyContainer.ivyGO);
            go.name = go.name.Remove(go.name.Length - 7, 7);
            
            DestroyImmediate(infoPool.ivyContainer.ivyGO);
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
        
        public void RecordIvyToUndo()
        {
            Undo.IncrementCurrentGroup();
            infoPool.ivyContainer.RecordUndo();
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
        
        public void OnVinesMaterialChanged(Material newMaterial)
        {
            valueUpdated = true;
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
                SaveParameters();
                infoPool.meshBuilder.InitLeavesData();
                valueUpdated = true;
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

        public void OptimizeCurrentIvy()
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
                        RecordIvyToUndo();
                        branch.RemoveBranchPoint(p);
                        RefreshMesh();
                    }
                }
            }
        }
        
        public void OrientationToggle(float XSpace, float YSpace)
        {
            var rect = new Rect(XSpace, YSpace, 100f, 50f);
            var globalStyle = windowSkin.GetStyle("toggleroundoff");
            var localStyle = windowSkin.GetStyle("toggleroundon");
            if (ivyParametersGUI.globalOrientation)
            {
                globalStyle = windowSkin.GetStyle("toggleroundon");
                localStyle = windowSkin.GetStyle("toggleroundoff");
            }

            GUI.Label(new Rect(rect.x, rect.y + 4f, rect.width / 2f, rect.height / 3f), "Global",
                windowSkin.GetStyle("intfloatfieldlabel"));
            if (GUI.Button(
                    new Rect(rect.x + 15f, rect.y + rect.height / 8f + 20f, rect.height / 3f * 2f,
                        rect.height / 3f * 2f), "", globalStyle))
            {
                ivyParametersGUI.globalOrientation = true;
                valueUpdated = true;
            }

            GUI.Label(new Rect(rect.x + rect.width / 2f + 15f, rect.y + 4f, rect.width / 2f, rect.height / 3f), "Local",
                windowSkin.GetStyle("intfloatfieldlabel"));
            if (GUI.Button(
                    new Rect(rect.x + rect.width / 2f + 28f, rect.y + rect.height / 8f + 20f, rect.height / 3f * 2f,
                        rect.height / 3f * 2f), "", localStyle))
            {
                ivyParametersGUI.globalOrientation = false;
                valueUpdated = true;
            }
        }
        
        public void OnUpdatingParameter(IvyParameter ivyParameter, float multiplier)
        {
            updatingParameter = ivyParameter;
            updatingValue = true;
            updatingValueMultiplier = multiplier;
            originalUpdatingValue = ivyParameter.value;
            mouseStartPoint = GUIUtility.GUIToScreenPoint(Event.current.mousePosition).x;
        }
        
        #endregion

        #region Misc
        
        public void ModifyIvy(IvyInfo ivyInfo)
        {
            currentIvyInfo = ivyInfo;
            selectedPreset = ivyInfo.originalPreset;

            infoPool = ivyInfo.infoPool;
            infoPool.ivyContainer.ivyGO = ivyInfo.gameObject;

            ivyGO = ivyInfo.gameObject;

            infoPool.growth.growing = false;

            infoPool.ivyParameters.branchesMaterial = infoPool.GetMeshRenderer().sharedMaterials[0];

            ivyParametersGUI.CopyFrom(infoPool.ivyParameters);

            infoPool.meshBuilder.InitLeavesData();
        }

        public void RefreshMesh()
        {
            if (ivyGO)
            {
                infoPool.meshBuilder.InitLeavesData();
                infoPool.meshBuilder.BuildGeometry();

                var mr = infoPool.GetMeshRenderer();
                var newMaterials = mr.sharedMaterials;
                newMaterials[0] = infoPool.ivyParameters.branchesMaterial;
                mr.sharedMaterials = newMaterials;

                var mf = infoPool.GetMeshFilter();
                mf.mesh = infoPool.meshBuilder.ivyMesh;
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
            return numVertices >= limit;
        }

        private static void AssignLabel(GameObject g)
        {
            var res = ProceduralIvyResources.Instance;
            if (res != null && res.labelIcon != null)
                EditorGUIUtility.SetIconForObject(g, res.labelIcon);
        }

        public void SaveParameters()
        {
            RegisterUndo();
            infoPool.ivyParameters.DeepCopy(ivyParametersGUI);
        }
        
        public void RegisterUndo()
        {
            if (infoPool) Undo.RegisterCompleteObjectUndo(infoPool, "Ivy Parameter Change");
        }
        
        #endregion

        #region Callbacks
        
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

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (EditorPrefs.GetBool(KEY_WINDOW_OPENED, false))
            {
                Init();
                
                if (Selection.activeGameObject == null)
                {
                    Instance.CreateIvyDataObject(ProceduralIvyResources.Instance.defaultPreset);
                }
                else
                {
                    var selectedIvyInfo = Selection.activeGameObject.GetComponent<IvyInfo>();
                    if (selectedIvyInfo != null) Instance.ModifyIvy(selectedIvyInfo);
                }
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            OnScriptsReloaded();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) 
                OnScriptsReloaded();
        }

        private static void OnDrawSceneGui(SceneView sceneView)
        {
            if (SceneGuiController != null)
                SceneGuiController.OnSceneGUI(sceneView);
        }
        
        private static void OnUndoPerformed()
        {
            if (Instance != null)
            {
                Instance.RefreshMesh();
                ivyParametersGUI.CopyFrom(Instance.infoPool.ivyParameters);
            }
        }
        
        #endregion
    }
}