using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TeamCrescendo.ProceduralIvy
{
    public class ProceduralIvyEditorWindow : EditorWindow
    {
        private SerializedObject serializedObject;
        private IvyInfo currentIvyInfo;
        
        #region Query Methods

        // Header
        private Label HeaderLabel => rootVisualElement.Q<Label>("header-label");

        // Tabs (Navigation)
        private ToolbarToggle TabGeneral => rootVisualElement.Q<ToolbarToggle>("tabGeneral");
        private ToolbarToggle TabBranches => rootVisualElement.Q<ToolbarToggle>("tabBranches");
        private ToolbarToggle TabLeaves => rootVisualElement.Q<ToolbarToggle>("tabLeaves");
        private ToolbarToggle TabGrowth => rootVisualElement.Q<ToolbarToggle>("tabGrowth");
        private ToolbarToggle TabExport => rootVisualElement.Q<ToolbarToggle>("tabExport");

        // Tab Content Containers
        private VisualElement ContentGeneral => rootVisualElement.Q<VisualElement>("contentGeneral");
        private VisualElement ContentGeneralSub => rootVisualElement.Q<VisualElement>("contentGeneralSub");
        private VisualElement ContentBranches => rootVisualElement.Q<VisualElement>("contentBranches");
        private VisualElement ContentLeaves => rootVisualElement.Q<VisualElement>("contentLeaves");
        private VisualElement ContentGrowth => rootVisualElement.Q<VisualElement>("contentGrowth");
        private VisualElement ContentExport => rootVisualElement.Q<VisualElement>("contentExport");

        // General Tab Controls
        private ToolbarToggle PlaceSeedToggle => rootVisualElement.Q<ToolbarToggle>("place-seed-toggle");
        private ToolbarToggle GrowthToggle => rootVisualElement.Q<ToolbarToggle>("growth-toggle");
        private Button RandomizeBtn => rootVisualElement.Q<Button>("randomize-btn");
        private Button ResetBtn => rootVisualElement.Q<Button>("reset-btn");
        private Button OptimizeBtn => rootVisualElement.Q<Button>("optimize-btn");
        private ObjectField PresetField => rootVisualElement.Q<ObjectField>("preset-field");
        private Button SavePresetBtn => rootVisualElement.Q<Button>("save-preset-btn");
        private Button SaveAsPresetBtn => rootVisualElement.Q<Button>("save-as-preset-btn");

        // Leaves Tab Controls
        private VisualElement LeavesListContainer => rootVisualElement.Q<VisualElement>("leaves-list-container");
        private Button AddLeafBtn => rootVisualElement.Q<Button>("add-leaf-btn");
        private Toggle GlobalOrientationToggle => rootVisualElement.Q<Toggle>("global-orientation-toggle");
        private VisualElement RotationContainer => rootVisualElement.Q<VisualElement>("rotation-container");
        private Vector3Field RotField => rootVisualElement.Q<Vector3Field>("rot-field");

        // Export Tab Controls
        private Button SaveSceneBtn => rootVisualElement.Q<Button>("save-scene-btn");
        private Button SavePrefabBtn => rootVisualElement.Q<Button>("save-prefab-btn");
        private Button ConvertProcBtn => rootVisualElement.Q<Button>("convert-proc-btn");
        private Button ConvertBakedBtn => rootVisualElement.Q<Button>("convert-baked-btn");
        
        #endregion
        
        private bool isPlacingSeed = false;
        private IvyPreset currentSelectedPreset;
        
        #region Init

        [MenuItem("Tools/Team Crescendo/Procedural Ivy 2")]
        public static void Init()
        {
            ProceduralIvyEditorWindow wnd = GetWindow<ProceduralIvyEditorWindow>();
            wnd.titleContent = new GUIContent("Procedural Ivy");
            wnd.minSize = new Vector2(400, 600);
        }
        
        public void CreateGUI()
        {
            var visualTree = LoadAsset<VisualTreeAsset>("ProceduralIvyEditorWindow");
    
            if (visualTree == null)
            {
                var errorLabel = new Label("Error: Could not find 'ProceduralIvyEditorWindow.uxml'.\nMake sure the file is named correctly.");
                errorLabel.style.color = Color.red;
                errorLabel.style.whiteSpace = WhiteSpace.Normal;
                rootVisualElement.Add(errorLabel);
                return;
            }
    
            visualTree.CloneTree(rootVisualElement);

            // load stylesheet
            var styleSheet = LoadAsset<StyleSheet>("ProceduralIvyWindow");
            if (styleSheet != null && !rootVisualElement.styleSheets.Contains(styleSheet))
                rootVisualElement.styleSheets.Add(styleSheet);
            
            if (PresetField != null) PresetField.objectType = typeof(IvyPreset);

            SetupTabs();
            RegisterCallbacks();
            OnSelectionChanged();
            SetupPresetsCallbacks();
            UpdateMenuState();
        }
        
        private void SetupTabs()
        {
            var tabs = new Dictionary<ToolbarToggle, VisualElement>
            {
                { TabGeneral, ContentGeneral },
                { TabBranches, ContentBranches },
                { TabLeaves, ContentLeaves },
                { TabGrowth, ContentGrowth },
                { TabExport, ContentExport }
            };

            foreach (var pair in tabs)
            {
                pair.Key.RegisterValueChangedCallback(evt =>
                {
                    // Enforce radio button behavior: cannot uncheck the active tab directly
                    if (!evt.newValue)
                    {
                        pair.Key.SetValueWithoutNotify(true);
                        return;
                    }

                    // Handle switching
                    foreach (var otherPair in tabs)
                    {
                        bool isTarget = otherPair.Key == pair.Key;
                
                        // Toggle visibility
                        otherPair.Value.style.display = isTarget ? DisplayStyle.Flex : DisplayStyle.None;

                        // Uncheck other buttons visually
                        if (!isTarget)
                            otherPair.Key.SetValueWithoutNotify(false);
                    }
                });
            }

            TabGeneral.value = false; 
            TabGeneral.value = true;
        }

        private T LoadAsset<T>(string name) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"{name} t:{typeof(T).Name}");
    
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
    
            return null;
        }

        private void SetupPresetsCallbacks()
        {
            var loadedPresets = Resources.LoadAll<IvyPreset>("DefaultPresets");
            var presetNames = loadedPresets.Select(p => p.name).ToList();

            var presetDropdown = rootVisualElement.Q<DropdownField>("preset-dropdown");

            if (presetDropdown != null)
            {
                presetDropdown.choices = presetNames;
                if (presetNames.Count > 0) presetDropdown.value = presetNames[0];

                presetDropdown.RegisterValueChangedCallback(evt =>
                {
                    currentSelectedPreset = loadedPresets.FirstOrDefault(p => p.name == evt.newValue);
                });
            }
        }
        
        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoPerformed;
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoPerformed;
            Selection.selectionChanged -= OnSelectionChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
            
            isPlacingSeed = false;
        }
        
        private void RegisterCallbacks()
        {
            // setup place seed button
            if (PlaceSeedToggle != null)
            {
                PlaceSeedToggle.RegisterValueChangedCallback(evt => 
                {
                    isPlacingSeed = evt.newValue;
                    
                    if (isPlacingSeed)
                    {
                        PlaceSeedToggle.AddToClassList("toggle-btn-active");
                        PlaceSeedToggle.text = "Placing Seed... (Esc to Cancel)";
                    }
                    else
                    {
                        PlaceSeedToggle.RemoveFromClassList("toggle-btn-active");
                        PlaceSeedToggle.text = "Place Seed Mode";
                    }
                    
                    SceneView.RepaintAll(); // Force scene refresh to show/hide handles immediately
                });
            }

            // setup growth button
            if (GrowthToggle != null)
            {
                GrowthToggle.RegisterValueChangedCallback(evt =>
                {
                    if (currentIvyInfo == null) return;
                    
                    bool wasGrowing = currentIvyInfo.infoPool.growth.IsGrowing();
                    currentIvyInfo.infoPool.growth.SetGrowing(evt.newValue);
                    if (currentIvyInfo.infoPool.growth.IsGrowing())
                    {
                        GrowthToggle.AddToClassList("toggle-btn-active");
                        GrowthToggle.text = "Growing!";
                    }
                    else
                    {
                        GrowthToggle.RemoveFromClassList("toggle-btn-active");
                        GrowthToggle.text = "Start Growth";
                    }

                    // logic for start growth
                    if (!wasGrowing && currentIvyInfo.infoPool.growth.IsGrowing())
                    {
                        bool success = StartGrowthIvy(currentIvyInfo.infoPool.ivyContainer.ivyGO.transform.position,
                            -currentIvyInfo.infoPool.ivyContainer.ivyGO.transform.up);
                        
                        if (!success) currentIvyInfo.infoPool.growth.SetGrowing(false);
                    }
                    
                    SceneView.RepaintAll();
                });
            }
            
            RandomizeBtn.clicked += OnRandomizeClicked;
            ResetBtn.clicked += OnResetClicked;
            OptimizeBtn.clicked += OnOptimizeClicked;

            PresetField.RegisterValueChangedCallback(OnPresetChanged);
            SavePresetBtn.clicked += OnSavePresetClicked;
            SaveAsPresetBtn.clicked += OnSaveAsPresetClicked;

            // Leaves Tab
            AddLeafBtn.clicked += OnAddLeafClicked;
            GlobalOrientationToggle.RegisterValueChangedCallback(OnGlobalOrientationChanged);
    
            // Export Tab
            SaveSceneBtn.clicked += OnSaveSceneClicked;
            SavePrefabBtn.clicked += OnSavePrefabClicked;
            ConvertProcBtn.clicked += OnConvertProceduralClicked;
            ConvertBakedBtn.clicked += OnConvertBakedClicked;
        }
        
        private void Update()
        {
            HandleGrowthUpdate();
        }
        
        private void OnInspectorUpdate()
        {
            UpdateMenuState();
        }
        
        private void UpdateMenuState()
        {
            bool shouldEnable = currentIvyInfo != null;

            ContentBranches.SetEnabled(shouldEnable);
            ContentLeaves.SetEnabled(shouldEnable);
            ContentGrowth.SetEnabled(shouldEnable);
            ContentExport.SetEnabled(shouldEnable);
            ContentGeneralSub.SetEnabled(shouldEnable);
        }
        
        #endregion

        #region Scene GUI

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!isPlacingSeed) return;
            
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event e = Event.current;

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                isPlacingSeed = false;
                PlaceSeedToggle.value = false;
                e.Use();
                return;
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Handles.color = new Color(0.3f, 1f, 0.3f, 0.8f);
                Handles.DrawSolidDisc(hit.point, hit.normal, 0.2f);
                Handles.DrawLine(hit.point, hit.point + hit.normal * 0.5f);
                
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    CreateNewIvyGameObject(hit.point, hit.normal);

                    // use the newly created ivy info
                    Assert.IsNotNull(currentIvyInfo);
                    Selection.activeGameObject = currentIvyInfo.gameObject;

                    // after create, immediately exit place seed mode
                    PlaceSeedToggle.value = false;
                    
                    e.Use();
                }
            }
        
            sceneView.Repaint();
        }

        #endregion

        #region Ivy Helpers

        private InfoPool CreateNewInfoPool(IvyParameters ivyParameters)
        {
            var infoPool = CreateInstance<InfoPool>();
            
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

        public void CreateNewIvyGameObject(Vector3 position, Vector3 normal)
        {
            Debug.Log("Creating new Ivy GameObject");
            
            InfoPool infoPool = CreateNewInfoPool(new IvyParameters(currentSelectedPreset));
            
            var newIvy = new GameObject("New Ivy");
            newIvy.transform.SetPositionAndRotation(
                position + normal * infoPool.ivyParameters.minDistanceToSurface, 
                Quaternion.LookRotation(normal));
            newIvy.transform.RotateAround(newIvy.transform.position, newIvy.transform.right, 90f);
            
            infoPool.ivyContainer.ivyGO = newIvy;
            
            infoPool.growth.SetGrowing(false);
            
            var mr = newIvy.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new [] {infoPool.ivyParameters.branchesMaterial};

            newIvy.AddComponent<MeshFilter>();

            currentIvyInfo = newIvy.AddComponent<IvyInfo>();
            currentIvyInfo.Setup(infoPool);

            infoPool.meshBuilder.InitLeavesData();

            infoPool.ivyContainer.RecordCreated();
            infoPool.ivyParameters.branchesMaterial = infoPool.GetMeshRenderer().sharedMaterials[0];
        }

        #endregion

        #region General

        private void OnPlaceSeedClicked()
        {
            // TODO: Instantiate seed logic
            Debug.Log("Place Seed Clicked");
        }

        private void OnRandomizeClicked()
        {
            // TODO: Undo.RecordObject -> Randomize params
            Debug.Log("Randomize Clicked");
        }

        private void OnGrowthClicked()
        {
            // TODO: Start coroutine or iteration
            Debug.Log("Growth Clicked");
        }

        private void OnResetClicked()
        {
            // TODO: Reset to default values
            Debug.Log("Reset Clicked");
        }

        private void OnOptimizeClicked()
        {
            // TODO: Run optimization algorithm
            Debug.Log("Optimize Clicked");
        }

        private void OnPresetChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            // TODO: Load values from new preset
            Debug.Log($"Preset Changed to: {evt.newValue}");
        }

        private void OnSavePresetClicked()
        {
            // TODO: Write current values to asset
            Debug.Log("Save Preset Clicked");
        }

        private void OnSaveAsPresetClicked()
        {
            // TODO: Open SaveFilePanel -> Create Asset
            Debug.Log("Save As Preset Clicked");
        }

        #endregion

        #region Branches

        

        #endregion

        #region Leaves

        private void OnAddLeafClicked()
        {
            // TODO: Add null entry to list -> Refresh UI
            Debug.Log("Add Leaf Clicked");
        }

        private void OnGlobalOrientationChanged(ChangeEvent<bool> evt)
        {
            // Toggle visibility of rotation field based on global override
            RotationContainer.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
        }

        #endregion

        #region Growth

        private void HandleGrowthUpdate()
        {
            if (currentIvyInfo != null 
                && currentIvyInfo.infoPool != null
                && currentIvyInfo.infoPool.growth != null 
                && currentIvyInfo.infoPool.growth.IsGrowing())
            {
                if (!IsVertexLimitReached())
                {
                    currentIvyInfo.infoPool.growth.Step();
                    RefreshMesh();
                }
                else
                {
                    if (currentIvyInfo.infoPool.ivyParameters.buffer32Bits)
                        Debug.LogWarning("Vertices limit reached at " + Constants.VERTEX_LIMIT_32 + ".", currentIvyInfo.infoPool.ivyContainer.ivyGO);
                    else
                        Debug.LogWarning("Vertices limit reached at " + Constants.VERTEX_LIMIT_16 + ".", currentIvyInfo.infoPool.ivyContainer.ivyGO);
                    currentIvyInfo.infoPool.growth.SetGrowing(false);
                }
            }
        }
        
        private bool StartGrowthIvy(Vector3 firstPoint, Vector3 firstGrabVector)
        {
            if (currentIvyInfo == null || rootVisualElement == null)
            {
                Debug.LogWarning("No Ivy Info or Root Visual Element");
                return false;
            }

            if (currentIvyInfo.infoPool.ivyContainer.branches.Count == 0)
            {
                currentIvyInfo.infoPool.growth.Initialize(firstPoint, firstGrabVector);
                currentIvyInfo.infoPool.meshBuilder.InitLeavesData();
                currentIvyInfo.infoPool.meshBuilder.InitializeMeshBuilder();
                return true;
            }

            Debug.LogWarning("Ivy already has branches!");
            return false;
        }

        #endregion

        #region Export

        private void OnSaveSceneClicked()
        {
            // TODO: Detach mesh to new GO
            Debug.Log("Save to Scene Clicked");
        }

        private void OnSavePrefabClicked()
        {
            // TODO: Create prefab asset
            Debug.Log("Save as Prefab Clicked");
        }

        private void OnConvertProceduralClicked()
        {
            // TODO: Switch component mode
            Debug.Log("Convert Procedural Clicked");
        }

        private void OnConvertBakedClicked()
        {
            // TODO: Switch component mode
            Debug.Log("Convert Baked Clicked");
        }

        #endregion

        #region Global Callbacks

        private void OnUndoPerformed()
        {
            if (currentIvyInfo == null || rootVisualElement == null) return;
            
            Repaint();
            serializedObject.Update();
            rootVisualElement.Bind(serializedObject);
        }

        private void OnSelectionChanged()
        {
            // could happen when scripts reload
            if (rootVisualElement == null) return;
            
            GameObject selected = Selection.activeGameObject;
            if (selected != null && selected.TryGetComponent(out IvyInfo ivy))
            {
                currentIvyInfo = ivy;
                // Create the SO and store it in the class member
                serializedObject = new SerializedObject(currentIvyInfo);
                rootVisualElement.Bind(serializedObject);
                HeaderLabel.text = $"Editing object: {ivy.name}";
            }
            else
            {
                currentIvyInfo = null;
                serializedObject = null;
                rootVisualElement.Unbind();
                HeaderLabel.text = "Editing object: None";
            }
        }

        #endregion

        #region Mesh Helpers

        private void RefreshMesh()
        {
            if (currentIvyInfo != null)
            {
                currentIvyInfo.infoPool.meshBuilder.InitLeavesData();
                currentIvyInfo.infoPool.meshBuilder.BuildGeometry();

                var mr = currentIvyInfo.infoPool.GetMeshRenderer();
                var newMaterials = mr.sharedMaterials;
                newMaterials[0] = currentIvyInfo.infoPool.ivyParameters.branchesMaterial;
                mr.sharedMaterials = newMaterials;

                var mf = currentIvyInfo.infoPool.GetMeshFilter();
                mf.mesh = currentIvyInfo.infoPool.meshBuilder.ivyMesh;
            }
        }
        
        private bool IsVertexLimitReached()
        {
            var numVertices = currentIvyInfo.infoPool.meshBuilder.verts.Length + 
                              currentIvyInfo.infoPool.ivyParameters.sides + 1;
            int limit = currentIvyInfo.infoPool.ivyParameters.buffer32Bits 
                ? Constants.VERTEX_LIMIT_32 
                : Constants.VERTEX_LIMIT_16;
            return numVertices >= limit;
        }

        #endregion
    }
}