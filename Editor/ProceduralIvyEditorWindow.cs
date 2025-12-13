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
using Random = UnityEngine.Random;

namespace TeamCrescendo.ProceduralIvy
{
    public class ProceduralIvyEditorWindow : EditorWindow
    {
        public static ProceduralIvyEditorWindow Instance { get; private set; }
        
        private SerializedObject serializedInfoPool;
        private SerializedObject serializedEditorObject;
        
        private IvyInfo currentIvyInfo;
        public IvyInfo CurrentIvyInfo
        {
            get => currentIvyInfo;
            private set
            {
                currentIvyInfo = value;
                OnIvyInfoChanged?.Invoke(currentIvyInfo);
            }
        }
        public static event Action<IvyInfo> OnIvyInfoChanged;
        
        public ProceduralIvySceneGui SceneGuiController { get; private set; }
        
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
        private VisualElement PresetActions => rootVisualElement.Q<VisualElement>("preset-actions");
        private ToolbarToggle PlaceSeedToggle => rootVisualElement.Q<ToolbarToggle>("place-seed-toggle");
        private ToolbarToggle GrowthToggle => rootVisualElement.Q<ToolbarToggle>("growth-toggle");
        private Button RandomizeBtn => rootVisualElement.Q<Button>("randomize-btn");
        private Button ResetBtn => rootVisualElement.Q<Button>("reset-btn");
        private Button DeleteBtn => rootVisualElement.Q<Button>("delete-btn");
        private Button OptimizeBtn => rootVisualElement.Q<Button>("optimize-btn");
        private ObjectField PresetDropdown => rootVisualElement.Q<ObjectField>("preset-dropdown");
        private Button ReloadPresetsBtn => rootVisualElement.Q<Button>("reload-presets-btn");
        private Button PresetSelectFromDropdownButton => rootVisualElement.Q<Button>("preset-select-from-dropdown");
        private ObjectField SelectedPresetObjectField => rootVisualElement.Q<ObjectField>("selected-preset-objectfield");
        private Button SavePresetBtn => rootVisualElement.Q<Button>("save-preset-btn");
        private Button SaveAsPresetBtn => rootVisualElement.Q<Button>("save-as-preset-btn");
        
        // mesh preview
        private IMGUIContainer MeshPreviewContainer => rootVisualElement.Q<IMGUIContainer>("mesh-preview-display");
        private Label MeshStatsLabel => rootVisualElement.Q<Label>("mesh-stats-label");

        // Leaves Tab Controls
        private Toggle GlobalOrientationToggle => rootVisualElement.Q<Toggle>("global-orientation-toggle");
        private VisualElement RotationContainer => rootVisualElement.Q<VisualElement>("rotation-container");
        private Vector3Field RotField => rootVisualElement.Q<Vector3Field>("rot-field");

        // Export Tab Controls
        private Button SaveSceneBtn => rootVisualElement.Q<Button>("save-scene-btn");
        private Button SavePrefabBtn => rootVisualElement.Q<Button>("save-prefab-btn");
        private Button ConvertProcBtn => rootVisualElement.Q<Button>("convert-proc-btn");
        private Button ConvertBakedBtn => rootVisualElement.Q<Button>("convert-baked-btn");
        
        #endregion
        
        public bool IsPlacingSeed { get; private set; }
        private IvyPreset presetSelectedInDropdown; // preset selected in the dropdown menu
        [SerializeField] private IvyPreset currentSelectedPreset; // the preset in the object selector
        
        // mesh preview fields
        private PreviewRenderUtility previewUtility;
        private Mesh currentMesh;
        private Material previewMaterial;
        private Vector2 meshPreviewDrag = new (180, 0);
        
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
            var visualTree = LoadAssetByName<VisualTreeAsset>("ProceduralIvyEditorWindow");
    
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
            var styleSheet = LoadAssetByName<StyleSheet>("ProceduralIvyWindow");
            if (styleSheet != null && !rootVisualElement.styleSheets.Contains(styleSheet))
                rootVisualElement.styleSheets.Add(styleSheet);
            
            if (PresetDropdown != null) PresetDropdown.objectType = typeof(IvyPreset);
            if (SelectedPresetObjectField != null) SelectedPresetObjectField.objectType = typeof(IvyPreset);

            SetupTabs();
            RegisterTabCallbacks();
            RegisterCallbacks();
            OnSelectionChanged();
            ReloadPresets();
            UpdateDisabledScopes();
            RebindEditorProperties();
            SetupMeshPreview();
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
        
        private void RegisterTabCallbacks()
        {
            var buttons = rootVisualElement.Query<Button>(className: "tab-button").ToList();

            foreach (var btn in buttons)
            {
                btn.clicked += () => 
                {
                    buttons.ForEach(b => b.RemoveFromClassList("tab-button--active"));
                    btn.AddToClassList("tab-button--active");
                };
            }
        }

        #region Asset Helpers
        
        private T LoadAssetByName<T>(string name) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"{name} t:{typeof(T).Name}");
    
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
    
            return null;
        }
        
        private T[] LoadAllAssets<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
    
            if (guids.Length > 0)
            {
                List<T> assets = new List<T>();
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    assets.Add(AssetDatabase.LoadAssetAtPath<T>(path));
                }
                return assets.ToArray();
            }
    
            return null;
        }
        
        #endregion

        private void ReloadPresets()
        {
            var loadedPresets = LoadAllAssets<IvyPreset>();
            var presetMap = new Dictionary<string, IvyPreset>();

            foreach (var preset in loadedPresets)
            {
                string path = AssetDatabase.GetAssetPath(preset);
        
                // REPLACEMENT: Change forward slashes to a safe visual separator (like ' > ')
                // This prevents Unity from creating nested sub-menus in the dropdown.
                string safePath = path.Replace("/", " > ");

                // Example result: "MyPreset (Assets > Ivy > Presets > MyPreset.asset)"
                string uniqueDisplay = $"{preset.name} ({safePath})";

                if (!presetMap.ContainsKey(uniqueDisplay))
                {
                    presetMap.Add(uniqueDisplay, preset);
                }
            }

            var presetDropdown = rootVisualElement.Q<DropdownField>("preset-dropdown");

            if (presetDropdown != null)
            {
                presetDropdown.choices = presetMap.Keys.ToList();

                // Handle selection logic (keep previous selection if valid, else select first)
                if (presetDropdown.choices.Count > 0)
                {
                    if (presetSelectedInDropdown != null && presetMap.ContainsValue(presetSelectedInDropdown))
                    {
                        string currentKey = presetMap.FirstOrDefault(x => x.Value == presetSelectedInDropdown).Key;
                        presetDropdown.value = currentKey;
                    }
                    else
                    {
                        presetDropdown.value = presetDropdown.choices[0];
                        presetSelectedInDropdown = presetMap[presetDropdown.value];
                    }
                }

                presetDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (presetMap.TryGetValue(evt.newValue, out var selectedPreset))
                    {
                        presetSelectedInDropdown = selectedPreset;
                    }
                });
            }
        }
        
        private void OnEnable()
        {
            if (Instance != null)
                throw new InvalidOperationException("ProceduralIvyEditorWindow instance already exists.");
            Instance = this;
            
            if (SceneGuiController != null)
                throw new InvalidOperationException("ProceduralIvySceneGui instance already exists.");
            SceneGuiController = new ProceduralIvySceneGui();
            
            Undo.undoRedoPerformed += OnUndoPerformed;
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui += OnSceneGUI;
            
            if (previewUtility == null)
            {
                previewUtility = new PreviewRenderUtility();
                previewMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")); 
            }
        }

        private void OnDisable()
        {
            if (Instance != this)
                throw new InvalidOperationException("ProceduralIvyEditorWindow instance does not match.");
            Instance = null;

            SceneGuiController.Cleanup();
            SceneGuiController = null;
            
            Undo.undoRedoPerformed -= OnUndoPerformed;
            Selection.selectionChanged -= OnSelectionChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
            
            IsPlacingSeed = false;
            
            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }

            if (previewMaterial != null)
            {
                DestroyImmediate(previewMaterial);
                previewMaterial = null;
            }
        }

        #region Mesh Preview
        
        private void SetupMeshPreview()
        {
            if (MeshPreviewContainer != null)
                MeshPreviewContainer.onGUIHandler = RenderPreview;
        }

        private void RenderPreview()
        {
            if (previewUtility == null || currentMesh == null)
            {
                GUILayout.Label("No Mesh Generated", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Rect rect = MeshPreviewContainer.contentRect;
            if (rect.width <= 1 || rect.height <= 1) return;

            HandlePreviewInput(rect);

            previewUtility.BeginPreview(rect, GUIStyle.none);

            // Setup Camera (Automatic Fit)
            // Calculate bounds to keep the object centered and visible
            Bounds bounds = currentMesh.bounds;
            float magnitude = bounds.extents.magnitude;
            float distance = magnitude * 3.5f; // Adjust zoom factor here
            
            previewUtility.camera.transform.position = bounds.center + new Vector3(0, 0, -distance);
            previewUtility.camera.transform.rotation = Quaternion.identity;
            previewUtility.camera.nearClipPlane = 0.1f;
            previewUtility.camera.farClipPlane = distance + magnitude * 2;

            // We rotate the mesh itself based on drag input
            Quaternion rot = Quaternion.Euler(meshPreviewDrag.y, meshPreviewDrag.x, 0);
            previewUtility.DrawMesh(currentMesh, Matrix4x4.TRS(Vector3.zero, rot, Vector3.one), previewMaterial, 0);

            // Render
            previewUtility.camera.Render();
            Texture result = previewUtility.EndPreview();
            GUI.DrawTexture(rect, result, ScaleMode.StretchToFill, false);
        }
        
        private void HandlePreviewInput(Rect rect)
        {
            Event e = Event.current;
        
            // Only handle drag if mouse is inside the preview box
            if (rect.Contains(e.mousePosition) && e.type == EventType.MouseDrag && e.button == 0) // Left Click Drag
            {
                meshPreviewDrag.x -= e.delta.x * 1f;
                meshPreviewDrag.y -= e.delta.y * 1f;
            
                // Force the UI to redraw immediately
                MeshPreviewContainer.MarkDirtyRepaint();
                e.Use();
            }
        }
        
        private void UpdatePreviewMesh(Mesh newMesh)
        {
            currentMesh = newMesh;
            
            if (MeshStatsLabel != null)
            {
                if (newMesh != null)
                {
                    // Format numbers with commas (e.g. 1,024)
                    string verts = newMesh.vertexCount.ToString("N0");
            
                    // Triangles is index count / 3
                    // (Note: use .triangles.Length if you want precise count, 
                    // but .vertexCount is usually enough for a quick check)
                    // Using GetIndexCount is faster than accessing .triangles array
                    long tris = 0;
                    for (int i = 0; i < newMesh.subMeshCount; i++)
                    {
                        tris += newMesh.GetIndexCount(i) / 3;
                    }

                    MeshStatsLabel.text = $"Verts: {verts}\nTris: {tris:N0}\nSubs: {newMesh.subMeshCount}";
                }
                else
                    MeshStatsLabel.text = "No Mesh Data";
            }

            if (MeshPreviewContainer != null) 
                MeshPreviewContainer.MarkDirtyRepaint();
        }

        #endregion
        
        private void RegisterCallbacks()
        {
            // setup place seed button
            if (PlaceSeedToggle != null)
            {
                PlaceSeedToggle.RegisterValueChangedCallback(evt => 
                {
                    IsPlacingSeed = evt.newValue;
                    
                    if (IsPlacingSeed)
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
                    if (CurrentIvyInfo == null) return;
                    
                    bool wasGrowing = CurrentIvyInfo.infoPool.growth.IsGrowing();
                    CurrentIvyInfo.infoPool.growth.SetGrowing(evt.newValue);
                    if (CurrentIvyInfo.infoPool.growth.IsGrowing())
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
                    if (!wasGrowing && CurrentIvyInfo.infoPool.growth.IsGrowing())
                    {
                        bool success = StartGrowthIvy(CurrentIvyInfo.transform.position,
                            -CurrentIvyInfo.transform.up);
                        
                        if (!success) CurrentIvyInfo.infoPool.growth.SetGrowing(false);
                    }
                    
                    SceneView.RepaintAll();
                });
            }
            
            RandomizeBtn.clicked += OnRandomizeClicked;
            ResetBtn.clicked += OnResetClicked;
            DeleteBtn.clicked += OnDeleteClicked; 
            OptimizeBtn.clicked += OnOptimizeClicked;

            PresetDropdown.RegisterValueChangedCallback(OnPresetChanged);
            ReloadPresetsBtn.clicked += OnReloadPresetsClicked;
            SavePresetBtn.clicked += OnSavePresetClicked;
            SaveAsPresetBtn.clicked += OnSaveAsPresetClicked;
            PresetSelectFromDropdownButton.clicked += OnPresetSelectFromDropdownClicked;

            // Leaves Tab
            GlobalOrientationToggle.RegisterValueChangedCallback(OnGlobalOrientationChanged);
    
            // Export Tab
            SaveSceneBtn.clicked += OnSaveToSceneClicked;
            SavePrefabBtn.clicked += OnSavePrefabClicked;
            ConvertProcBtn.clicked += OnConvertProceduralClicked;
            ConvertBakedBtn.clicked += OnConvertBakedClicked;
        }

        private void RebindEditorProperties()
        {
            serializedEditorObject = new SerializedObject(this);
            
            if (SelectedPresetObjectField != null)
            {
                SerializedProperty prop = serializedEditorObject.FindProperty("currentSelectedPreset");
                SelectedPresetObjectField.BindProperty(prop);
            }
        }
        
        private void Update()
        {
            HandleGrowthUpdate();
        }
        
        private void OnInspectorUpdate()
        {
            UpdateDisabledScopes();
        }
        
        private void UpdateDisabledScopes()
        {
            bool hasCurrentObj = CurrentIvyInfo != null;

            ContentBranches.SetEnabled(hasCurrentObj);
            ContentLeaves.SetEnabled(hasCurrentObj);
            ContentGrowth.SetEnabled(hasCurrentObj);
            ContentExport.SetEnabled(hasCurrentObj);
            ContentGeneralSub.SetEnabled(hasCurrentObj);
            PresetActions.SetEnabled(hasCurrentObj);
            
            bool notGrowingAndNotInPlantingSeedMode = hasCurrentObj && !CurrentIvyInfo.infoPool.growth.IsGrowing() && !IsPlacingSeed;
            ResetBtn.SetEnabled(notGrowingAndNotInPlantingSeedMode);
            RandomizeBtn.SetEnabled(notGrowingAndNotInPlantingSeedMode);
            OptimizeBtn.SetEnabled(notGrowingAndNotInPlantingSeedMode);
        }
        
        #endregion

        #region Scene GUI

        private void OnSceneGUI(SceneView sceneView)
        {
            if (IsPlacingSeed)
            {

                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                Event e = Event.current;

                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
                {
                    IsPlacingSeed = false;
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
                        Assert.IsNotNull(CurrentIvyInfo);
                        Selection.activeGameObject = CurrentIvyInfo.gameObject;

                        // after create, immediately exit place seed mode
                        PlaceSeedToggle.value = false;

                        e.Use();
                    }
                }

                sceneView.Repaint();
            }
            else
            {
                if (SceneGuiController != null)
                    SceneGuiController.OnSceneGUI(sceneView);
            }
        }

        #endregion

        #region Ivy Helpers

        private InfoPool CreateNewInfoPool(IvyParameters ivyParameters)
        {
            // 1. Determine the save path based on the active scene
            var scene = SceneManager.GetActiveScene();
            string parentFolder = "Assets";
            string sceneName = "UnsavedScene";
            
            if (!string.IsNullOrEmpty(scene.path))
            {
                // Unity paths use forward slashes, Path.GetDirectoryName might return backslashes on Windows
                parentFolder = Path.GetDirectoryName(scene.path).Replace("\\", "/");
                sceneName = Path.GetFileNameWithoutExtension(scene.path);
            }

            string subFolderPath = $"{parentFolder}/{sceneName}";
            if (!AssetDatabase.IsValidFolder(subFolderPath))
                AssetDatabase.CreateFolder(parentFolder, sceneName);

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{subFolderPath}/IvyData.asset");
            var infoPool = CreateInstance<InfoPool>();
            AssetDatabase.CreateAsset(infoPool, assetPath);

            // embed IvyContainer
            infoPool.ivyContainer = CreateInstance<IvyContainer>();
            infoPool.ivyContainer.name = "IvyContainer";
            infoPool.ivyContainer.branches = new List<BranchContainer>();
            AssetDatabase.AddObjectToAsset(infoPool.ivyContainer, infoPool);

            infoPool.ivyParameters = ivyParameters;

            // embed EditorIvyGrowth
            infoPool.growth = CreateInstance<EditorIvyGrowth>();
            infoPool.growth.name = "Growth";
            infoPool.growth.infoPool = infoPool;
            AssetDatabase.AddObjectToAsset(infoPool.growth, infoPool);

            // embed EditorMeshBuilder
            infoPool.meshBuilder = CreateInstance<EditorMeshBuilder>();
            infoPool.meshBuilder.name = "MeshBuilder";
            infoPool.meshBuilder.infoPool = infoPool;
            AssetDatabase.AddObjectToAsset(infoPool.meshBuilder, infoPool);

            // embed Mesh
            infoPool.meshBuilder.ivyMesh = new Mesh();
            infoPool.meshBuilder.ivyMesh.name = "IvyMesh";
            AssetDatabase.AddObjectToAsset(infoPool.meshBuilder.ivyMesh, infoPool);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return infoPool;
        }
        
        public void CreateNewIvyGameObject(Vector3 rootPosition, Vector3 normal)
        {
            Debug.Log("Creating new Ivy GameObject");
            
            InfoPool infoPool = CreateNewInfoPool(new IvyParameters(currentSelectedPreset));
            
            var newIvy = new GameObject("Ivy Container");
            newIvy.transform.SetPositionAndRotation(
                rootPosition + normal * infoPool.ivyParameters.minDistanceToSurface, 
                Quaternion.LookRotation(normal));
            newIvy.transform.RotateAround(newIvy.transform.position, newIvy.transform.right, 90f);
            
            infoPool.growth.SetGrowing(false);
            
            var mr = newIvy.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new [] {infoPool.ivyParameters.branchesMaterial};

            newIvy.AddComponent<MeshFilter>();

            CurrentIvyInfo = newIvy.AddComponent<IvyInfo>();
            CurrentIvyInfo.infoPool = infoPool;

            infoPool.meshBuilder.InitLeavesData(CurrentIvyInfo.transform, CurrentIvyInfo.GetComponent<MeshRenderer>());
        }

        #endregion

        #region General

        private void OnRandomizeClicked()
        {
            Assert.IsNotNull(CurrentIvyInfo);
            
            var newSeed = Environment.TickCount;
            CurrentIvyInfo.infoPool.ivyParameters.randomSeed = newSeed;
            Random.InitState(newSeed);
            
            CurrentIvyInfo.infoPool.meshBuilder.InitLeavesData(CurrentIvyInfo.transform, CurrentIvyInfo.GetComponent<MeshRenderer>());
            
            RefreshMesh();
        }

        private void OnResetClicked()
        {
            Assert.IsNotNull(CurrentIvyInfo);
            
            CurrentIvyInfo.infoPool.growth.SetGrowing(false);
            CurrentIvyInfo.infoPool.ivyContainer.Clear();
            
            CurrentIvyInfo.infoPool.meshBuilder.InitLeavesData(CurrentIvyInfo.transform, CurrentIvyInfo.GetComponent<MeshRenderer>());
            
            RefreshMesh();
        }
        
        private void OnDeleteClicked()
        {
            Assert.IsNotNull(CurrentIvyInfo);
            
            DestroyImmediate(CurrentIvyInfo.infoPool);
            DestroyImmediate(CurrentIvyInfo.gameObject);
            CurrentIvyInfo = null;

            UpdatePreviewMesh(null);
        }

        private void OnOptimizeClicked()
        {
            Assert.IsNotNull(CurrentIvyInfo);
            
            CurrentIvyInfo.infoPool.ivyContainer.RecordUndo();
            
            for (var b = 0; b < CurrentIvyInfo.infoPool.ivyContainer.branches.Count; b++)
            {
                var branch = CurrentIvyInfo.infoPool.ivyContainer.branches[b];
                for (var p = 1; p < branch.branchPoints.Count - 1; p++)
                {
                    var segment1 = branch.branchPoints[p].point - branch.branchPoints[p - 1].point;
                    var segment2 = branch.branchPoints[p + 1].point - branch.branchPoints[p].point;
                    if (Vector3.Angle(segment1, segment2) < CurrentIvyInfo.infoPool.ivyParameters.optAngleBias)
                    {
                        branch.RemoveBranchPoint(p);
                    }
                }
            }
            
            RefreshMesh();
        }

        private void OnPresetChanged(ChangeEvent<Object> evt)
        {
            presetSelectedInDropdown = evt.newValue as IvyPreset;
            Assert.IsNotNull(presetSelectedInDropdown);
        }
        
        private void OnReloadPresetsClicked()
        {
            ReloadPresets();
        }
        
        private void OnPresetSelectFromDropdownClicked()
        {
            if (presetSelectedInDropdown == null) return;
            
            currentSelectedPreset = presetSelectedInDropdown;
            RebindEditorProperties();
        }

        private void OnSavePresetClicked()
        {
            Assert.IsNotNull(CurrentIvyInfo);
            
            // set the preset's ivy parameters to the current monobehavior's parameters
            currentSelectedPreset.ivyParameters.DeepCopy(CurrentIvyInfo.infoPool.ivyParameters);

            EditorUtility.SetDirty(currentSelectedPreset);
            AssetDatabase.SaveAssets();
            
            Debug.Log("Saved preset: " + currentSelectedPreset.name);
        }

        private void OnSaveAsPresetClicked()
        {
            Assert.IsNotNull(CurrentIvyInfo);
            
            var filePath = EditorUtility.SaveFilePanelInProject("Save preset as...", "Procedural Ivy Preset", "asset", "");
            if (filePath != "")
            {
                var newPreset = CreateInstance<IvyPreset>();
                newPreset.ivyParameters = new IvyParameters(CurrentIvyInfo.infoPool.ivyParameters);

                AssetDatabase.CreateAsset(newPreset, filePath);
                AssetDatabase.SaveAssets();
            }
            
            Debug.Log("Created new preset: " + filePath);
        }

        #endregion

        #region Leaves

        private void OnGlobalOrientationChanged(ChangeEvent<bool> evt)
        {
            // Toggle visibility of rotation field based on global override
            RotationContainer.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
        }

        #endregion

        #region Growth

        private void HandleGrowthUpdate()
        {
            if (CurrentIvyInfo != null 
                && CurrentIvyInfo.infoPool != null
                && CurrentIvyInfo.infoPool.growth != null 
                && CurrentIvyInfo.infoPool.growth.IsGrowing())
            {
                if (!IsVertexLimitReached())
                {
                    CurrentIvyInfo.infoPool.growth.Step();
                    RefreshMesh();
                }
                else
                {
                    if (CurrentIvyInfo.infoPool.ivyParameters.buffer32Bits)
                        Debug.LogWarning("Vertices limit reached at " + Constants.VERTEX_LIMIT_32 + ".", CurrentIvyInfo.gameObject);
                    else
                        Debug.LogWarning("Vertices limit reached at " + Constants.VERTEX_LIMIT_16 + ".", CurrentIvyInfo.gameObject);
                    CurrentIvyInfo.infoPool.growth.SetGrowing(false);
                }
            }
        }
        
        public bool StartGrowthIvy(Vector3 firstPoint, Vector3 firstGrabVector)
        {
            if (CurrentIvyInfo == null || rootVisualElement == null)
            {
                Debug.LogWarning("No Ivy Info or Root Visual Element");
                return false;
            }

            if (CurrentIvyInfo.infoPool.ivyContainer.branches.Count == 0)
            {
                CurrentIvyInfo.infoPool.growth.Initialize(CurrentIvyInfo.transform, firstPoint, firstGrabVector);
                CurrentIvyInfo.infoPool.meshBuilder.InitLeavesData(CurrentIvyInfo.transform, CurrentIvyInfo.GetComponent<MeshRenderer>());
                CurrentIvyInfo.infoPool.meshBuilder.InitializeMeshBuilder();
                return true;
            }

            Debug.LogWarning("Ivy already has branches. Should reset and grow again!");
            return false;
        }

        #endregion

        #region Export
        
        private GameObject RemoveAllScripts()
        {
            if (CurrentIvyInfo.infoPool.ivyParameters.generateLightmapUVs) 
                CurrentIvyInfo.infoPool.meshBuilder.GenerateLMUVs();
            
            // need to cache since CurrentIvyInfo will be destroyed
            GameObject go = CurrentIvyInfo.gameObject;
            
            // Destroy all components
            foreach (var component in go.GetComponentsInChildren<MonoBehaviour>())
                DestroyImmediate(component);
            
            CurrentIvyInfo = null;

            // Instantiate the object
            var newGameObject = Instantiate(go);
            newGameObject.name = newGameObject.name.Remove(newGameObject.name.Length - 7, 7); // Remove " (Clone)"
            newGameObject.name += " - Static";
            
            // Destroy the original object
            DestroyImmediate(go);
            
            return newGameObject;
        }

        private void SaveToSceneTask()
        {
            GameObject go = RemoveAllScripts();
            Selection.activeGameObject = go;
            OnSelectionChanged();
        }

        private void OnSaveToSceneClicked()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Save Ivy into Scene",
                "Are you sure you want to save the current ivy into the scene? This cannot be undone.",
                "Yes, Save it",
                "Cancel"
            );

            if (confirmed)
                SaveToSceneTask();
        }
        
        private void SaveAsPrefabTask(string fileName)
        {
            var filePath = EditorUtility.SaveFilePanelInProject("Save Ivy as prefab", fileName, "prefab", "");
            if (!string.IsNullOrEmpty(filePath))
            {
                var go = RemoveAllScripts();
                var prefabSaved = PrefabUtility.SaveAsPrefabAssetAndConnect(go, filePath, InteractionMode.AutomatedAction);
                EditorGUIUtility.PingObject(prefabSaved);
            }
        }

        private void OnSavePrefabClicked()
        {
            if (CurrentIvyInfo.GetComponent<RuntimeIvy>())
            {
                Debug.LogWarning("Cannot save prefab since a Runtime Ivy component is present");
                return;
            }
            
            bool confirmed = EditorUtility.DisplayDialog(
                "Save Ivy as Prefab",
                "Are you sure you want to save the current ivy as a prefab? This cannot be undone.",
                "Yes, Save it",
                "Cancel"
            );

            if (confirmed)
                SaveAsPrefabTask(CurrentIvyInfo.gameObject.name);
        }

        private void OnConvertBakedClicked()
        {
            if (CurrentIvyInfo.TryGetComponent(out RuntimeIvy existingIvy))
            {
                EditorUtility.DisplayDialog(
                    "Warning",
                    $"A RuntimeIvy component ({existingIvy.GetType().Name}) already exists on '{CurrentIvyInfo.gameObject.name}'.",
                    "OK"
                );
                return;
            }
            
            bool confirmed = EditorUtility.DisplayDialog(
                "Convert to Runtime Baked Ivy",
                "Are you sure you want to convert the current ivy to a Runtime Baked Ivy? " +
                "This will replace the current Ivy component with a Runtime Baked Ivy component.",
                "Yes, Convert it",
                "Cancel"
            );
            
            if (confirmed)
                ConvertToRuntimeIvy<RuntimeBakedIvy>(CurrentIvyInfo.gameObject);
        }

        private void OnConvertProceduralClicked()
        {
            if (CurrentIvyInfo.TryGetComponent(out RuntimeIvy existingIvy))
            {
                EditorUtility.DisplayDialog(
                    "Warning",
                    $"A RuntimeIvy component ({existingIvy.GetType().Name}) already exists on '{CurrentIvyInfo.gameObject.name}'.",
                    "OK"
                );
                return;
            }
            
            bool confirmed = EditorUtility.DisplayDialog(
                "Convert to Runtime Procedural Ivy",
                "Are you sure you want to convert the current ivy to a Runtime Procedural Ivy? " +
                "This will replace the current Ivy component with a Runtime Procedural Ivy component.",
                "Yes, Convert it",
                "Cancel"
            );
            
            if (confirmed)
                ConvertToRuntimeIvy<RuntimeProceduralIvy>(CurrentIvyInfo.gameObject);
        }
        
        private void ConvertToRuntimeIvy<T>(GameObject target) where T : RuntimeIvy
        {
            // Get or Add the specific Ivy component (Baked or Procedural)
            if (!target.TryGetComponent(out T specificIvy))
                specificIvy = target.AddComponent<T>();

            // Get or Add IvyController
            if (!target.TryGetComponent(out IvyController ivyController))
                ivyController = target.AddComponent<IvyController>();

            // Setup Controller
            ivyController.runtimeIvy = specificIvy;
            ivyController.ivyContainer = CurrentIvyInfo.infoPool.ivyContainer;
            ivyController.ivyParameters = CurrentIvyInfo.infoPool.ivyParameters;
    
            // Reset growth parameters for new components
            if (ivyController.growthParameters == null)
                ivyController.growthParameters = new RuntimeGrowthParameters();

            SetupProcessedMesh(specificIvy);
        }
        
        private void SetupProcessedMesh(RuntimeIvy rtBakedIvy)
        {
            var processedMesh = new GameObject("ProcessedMesh");
    
            processedMesh.transform.SetParent(rtBakedIvy.transform);
            processedMesh.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            rtBakedIvy.mrProcessedMesh = processedMesh.AddComponent<MeshRenderer>();
            rtBakedIvy.mfProcessedMesh = processedMesh.AddComponent<MeshFilter>();
        }

        #endregion

        #region Global Callbacks

        private void OnUndoPerformed()
        {
            if (CurrentIvyInfo == null || rootVisualElement == null) return;
            
            Repaint();
            serializedInfoPool.Update();
            rootVisualElement.Bind(serializedInfoPool);
        }

        private void OnSelectionChanged()
        {
            // could happen when scripts reload
            if (rootVisualElement == null) return;
            
            GameObject selected = Selection.activeGameObject;
            if (selected != null && selected.TryGetComponent(out IvyInfo ivy))
            {
                CurrentIvyInfo = ivy;

                if (CurrentIvyInfo.infoPool == null)
                {
                    Debug.LogWarning($"No InfoPool found on IvyInfo {ivy.name}.");
                    return;
                }
                
                // Create the SO and store it in the class member
                serializedInfoPool = new SerializedObject(CurrentIvyInfo.infoPool);
                rootVisualElement.Bind(serializedInfoPool);
                if (HeaderLabel != null) HeaderLabel.text = $"Editing object: {ivy.name}";
                UpdatePreviewMesh(CurrentIvyInfo.infoPool.meshBuilder.ivyMesh);
            }
            else
            {
                CurrentIvyInfo = null;
                serializedInfoPool = null;
                rootVisualElement.Unbind();
                if (HeaderLabel != null) HeaderLabel.text = "Editing object: None";
                UpdatePreviewMesh(null);
            }
        }

        #endregion

        #region Mesh Helpers

        private void RefreshMesh()
        {
            if (CurrentIvyInfo != null)
            {
                CurrentIvyInfo.infoPool.meshBuilder.InitLeavesData(CurrentIvyInfo.transform, CurrentIvyInfo.GetComponent<MeshRenderer>());
                CurrentIvyInfo.infoPool.meshBuilder.BuildGeometry();

                if (!CurrentIvyInfo.TryGetComponent<MeshRenderer>(out var mr)) 
                    throw new InvalidOperationException("No MeshRenderer found on IvyInfo");
                var newMaterials = mr.sharedMaterials;
                newMaterials[0] = CurrentIvyInfo.infoPool.ivyParameters.branchesMaterial;
                mr.sharedMaterials = newMaterials;

                if (!CurrentIvyInfo.TryGetComponent<MeshFilter>(out var mf)) 
                    throw new InvalidOperationException("No MeshFilter found on IvyInfo");
                Mesh newMesh = CurrentIvyInfo.infoPool.meshBuilder.ivyMesh;
                mf.mesh = newMesh;

                // do not call mf.mesh, will leak!
                UpdatePreviewMesh(newMesh);
            }
        }
        
        private bool IsVertexLimitReached()
        {
            var numVertices = CurrentIvyInfo.infoPool.meshBuilder.verts.Length + 
                              CurrentIvyInfo.infoPool.ivyParameters.sides + 1;
            int limit = CurrentIvyInfo.infoPool.ivyParameters.buffer32Bits 
                ? Constants.VERTEX_LIMIT_32 
                : Constants.VERTEX_LIMIT_16;
            return numVertices >= limit;
        }

        #endregion
    }
}