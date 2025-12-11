using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeamCrescendo.ProceduralIvy
{
    public class RealIvyWindow : EditorWindow
    {
        private const string KEY_WINDOW_OPENED = "RealIvyProWindow_Opened";
        private const string GUID_DEFAULT_PRESET = "d022a91abdaf78e429b3aa20f69127dd";

        public static RealIvyWindow instance;
        public static RealIvyTools realIvyProToolsWindow;
        public static RealIvyProWindowController controller;

        public static IvyParametersGUI ivyParametersGUI;
        public static GUISkin windowSkin;
        public static Texture2D downArrowTex, materialTex, leaveTex, dropdownShadowTex, presetTex, infoTex;

        public bool placingSeed;

        public GUISkin oldSkin;

        public Vector2 generalScrollView;
        public float YSpace;
        public Rect generalArea;
        public Color bckgColor = new(0.45f, 0.45f, 0.45f);
        public Color bckgColor2 = new(0.40f, 0.40f, 0.40f);
        public Vector2 leavesPrefabsScrollView;
        public bool valueUpdated;

        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public bool updatingValue;
        public float originalUpdatingValue;
        public float updatingValueMultiplier;
        public float mouseStartPoint;
        public int presetSelected;
        public List<int> presetsChanged = new();
        private readonly UIZone_BranchesSettings branchesSettingsZone = new();
        private readonly UIZone_GeneralSettings generalSettingsZone = new();
        private readonly UIZone_GrowthSettings growthSettings = new();
        private readonly UIZone_LeavesSettings leavesSettingsZone = new();

        private readonly UIZone_MainButtons mainButtonsZone = new();
        public IvyParameter updatingParameter;

        private void Update()
        {
            controller.Update();
        }

        private void OnDestroy()
        {
            realIvyProToolsWindow.QuitWindow();
            DestroyImmediate(realIvyProToolsWindow);
            controller.Destroy();

            SceneView.RepaintAll();

            EditorPrefs.SetBool(KEY_WINDOW_OPENED, false);
        }

        private void OnGUI()
        {
            if (!realIvyProToolsWindow) CreateTools();
            oldSkin = GUI.skin;
            GUI.skin = windowSkin;

            EditorGUI.BeginChangeCheck();
            DrawGUI();

            if (EditorGUI.EndChangeCheck() || valueUpdated)
            {
                if (controller.GenerateLightmapUVsActivated())
                    CustomDisplayDialog.Init(windowSkin, Constants.LIGHTMAP_UVS_WARNING, "Lightmap UVs warning",
                        infoTex, 370f, 155f, null);
                valueUpdated = false;
                SaveParameters();
                controller.RefreshMesh();
                Repaint();
            }

            GUI.skin = oldSkin;
        }

        [MenuItem("Tools/3Dynamite/Real Ivy")]
        public static void Init()
        {
            Init(true);
        }

        public static void Init(bool createNewIvy)
        {
            instance = (RealIvyWindow)GetWindow(typeof(RealIvyWindow));

            instance.minSize = new Vector2(450f, 455f);
            instance.titleContent = new GUIContent("Real Ivy");

            Initialize(createNewIvy);

            EditorSceneManager.sceneOpened += OnSceneOpened;

            EditorPrefs.SetBool(KEY_WINDOW_OPENED, true);
        }

        private static void MyUndoCallback()
        {
            controller.RefreshMesh();
            RefreshEditorValues();
        }

        public static void Initialize(bool createNewIvy)
        {
            Undo.ClearAll();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            ivyParametersGUI = CreateInstance<IvyParametersGUI>();
            controller = CreateInstance<RealIvyProWindowController>();
            controller.Init(instance, ivyParametersGUI);

            windowSkin =
                (GUISkin)AssetDatabase.LoadAssetAtPath(
                    AssetDatabase.GUIDToAssetPath("b0545e8c97ca8684182a76c2fb22c7ff"), typeof(GUISkin));
            downArrowTex =
                (Texture2D)AssetDatabase.LoadAssetAtPath(
                    AssetDatabase.GUIDToAssetPath("8ee6aee77df7d3e4485148aa889f9b6b"), typeof(Texture2D));
            materialTex =
                (Texture2D)AssetDatabase.LoadAssetAtPath(
                    AssetDatabase.GUIDToAssetPath("eb3b714e29c31744888e1bc4bcfe23d6"), typeof(Texture2D));
            leaveTex = (Texture2D)AssetDatabase.LoadAssetAtPath(
                AssetDatabase.GUIDToAssetPath("14bbaf6e0a8b00f4ea30434e5eeeaf8c"), typeof(Texture2D));
            dropdownShadowTex =
                (Texture2D)AssetDatabase.LoadAssetAtPath(
                    AssetDatabase.GUIDToAssetPath("9cd9a16c9e229684983f50ff07427219"), typeof(Texture2D));
            presetTex = (Texture2D)AssetDatabase.LoadAssetAtPath(
                AssetDatabase.GUIDToAssetPath("9dd821bf05e345d4a8a501a8768c7144"), typeof(Texture2D));
            infoTex = (Texture2D)AssetDatabase.LoadAssetAtPath(
                AssetDatabase.GUIDToAssetPath("d73d5146604f9594996de4e08eec4bdf"), typeof(Texture2D));

            Undo.undoRedoPerformed += MyUndoCallback;

            var defaultPresset = GetDefaultPreset();

            if (realIvyProToolsWindow != null) realIvyProToolsWindow.QuitWindow();
            CreateTools();

            if (createNewIvy)
            {
                controller.CreateNewIvy(defaultPresset);
                ivyParametersGUI.CopyFrom(controller.infoPool.ivyParameters);
            }
        }

        public static void AssignLabel(GameObject g)
        {
            var tex = (Texture2D)AssetDatabase.LoadAssetAtPath(
                AssetDatabase.GUIDToAssetPath("a1ca40cfe045c6c4a80354e9c26cd083"), typeof(Texture2D));
            EditorGUIUtility.SetIconForObject(g, tex);
        }

        public InfoPool CreateNewIvy()
        {
            return controller.CreateNewIvy();
        }

        public void CreateIvyGO(Vector3 position, Vector3 normal)
        {
            controller.CreateIvyGO(position, normal);
            Selection.activeGameObject = controller.ivyGO;
            AssignLabel(controller.ivyGO);
        }

        private static void CreateTools()
        {
            realIvyProToolsWindow = CreateInstance<RealIvyTools>();
            realIvyProToolsWindow.Init(instance, controller.infoPool);

            SceneView.duringSceneGui -= realIvyProToolsWindow.OnSceneGUI;
            SceneView.duringSceneGui += realIvyProToolsWindow.OnSceneGUI;
        }

        private static void RefreshEditorValues()
        {
            ivyParametersGUI.CopyFrom(controller.infoPool.ivyParameters);
        }

        public void SaveParameters()
        {
            controller.RegisterUndo();
            controller.infoPool.ivyParameters.CopyFrom(ivyParametersGUI);
        }

        private void DrawGUI()
        {
            EditorGUI.DrawRect(new Rect(0f, 0f, position.width, position.height), bckgColor);
            generalScrollView = GUI.BeginScrollView(new Rect(0f, 0f, position.width, position.height),
                generalScrollView, new Rect(0f, 0f, position.width - 17f, YSpace), false, false);

            //Pequeña lógica para la responsividad en caso de tener barra de scroll o no 
            float generalAreaWidth;
            if (YSpace > position.height)
                generalAreaWidth = position.width - 34f;
            else
                generalAreaWidth = position.width - 20f;

            YSpace = 0f;

            var presetDropDownYSpace = 0f;


            mainButtonsZone.DrawZone(this, ivyParametersGUI, windowSkin, controller, ref YSpace, generalArea,
                bckgColor2);

            generalArea = new Rect(10f, 10f, generalAreaWidth, 520f);

            var generalSettingsYSpace = 0f;

            generalSettingsZone.DrawZone("General settings", 265f, this, ivyParametersGUI, windowSkin,
                controller, ref YSpace, ref presetDropDownYSpace,
                ref generalSettingsYSpace, generalArea, bckgColor2, animationCurve);

            var branchesAreaYSpace = 0f;

            branchesSettingsZone.DrawZone("Branches settings", 185f, this, ivyParametersGUI, windowSkin,
                controller, ref YSpace, ref presetDropDownYSpace,
                ref branchesAreaYSpace, generalArea, bckgColor2, animationCurve);

            var leavesAreaYSpace = 0f;

            leavesSettingsZone.DrawZone("Leaves settings", 230f, this, ivyParametersGUI, windowSkin, controller,
                ref YSpace, ref presetDropDownYSpace,
                ref leavesAreaYSpace, generalArea, bckgColor2, animationCurve);

            var growthAreaYSpace = 0f;

            growthSettings.DrawZone("Growth settings", 260f, this, ivyParametersGUI, windowSkin, controller,
                ref YSpace, ref presetDropDownYSpace, ref growthAreaYSpace,
                generalArea, bckgColor2, animationCurve);


            GUI.EndScrollView();

            if (updatingValue) UpdateValue();
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

        private void UpdateValue()
        {
            var evt = Event.current;
            if (updatingValue && evt != null)
                switch (evt.rawType)
                {
                    case EventType.MouseUp:
                        updatingValue = false;
                        break;
                }

            var delta = GUIUtility.GUIToScreenPoint(Event.current.mousePosition).x - mouseStartPoint;
            var value = originalUpdatingValue + delta * updatingValueMultiplier;

            updatingParameter.UpdateValue(value);
            valueUpdated = true;
            Repaint();
        }

        private static IvyPreset GetDefaultPreset()
        {
            IvyPreset res = null;
            var defaultPresetGUID = EditorPrefs.GetString("RealIvyDefaultGUID", GUID_DEFAULT_PRESET);

            res = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(defaultPresetGUID), typeof(IvyPreset)) as
                IvyPreset;

            if (res == null)
            {
                defaultPresetGUID = GUID_DEFAULT_PRESET;
                res = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(defaultPresetGUID), typeof(IvyPreset))
                    as IvyPreset;
            }

            return res;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (EditorPrefs.GetBool(KEY_WINDOW_OPENED, false))
            {
                Init();
                var preset = GetDefaultPreset();
                controller.OnScriptReloaded(preset);
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            OnScriptsReloaded();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) OnScriptsReloaded();
        }
    }
}