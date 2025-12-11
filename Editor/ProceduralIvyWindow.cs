using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeamCrescendo.ProceduralIvy
{
    public class ProceduralIvyWindow : EditorWindow
    {
        private const string KEY_WINDOW_OPENED = "ProceduralIvyWindow_Opened";

        public static ProceduralIvyWindow Instance;
        public static ProceduralIvySceneGui SceneGuiController;
        public static ProceduralIvyWindowController Controller;

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
        
        [MenuItem("Tools/Team Crescendo/Procedural Ivy")]
        public static void Init()
        {
            Instance = (ProceduralIvyWindow)GetWindow(typeof(ProceduralIvyWindow));

            Instance.minSize = new Vector2(450f, 455f);
            Instance.titleContent = new GUIContent("Procedural Ivy");

            ivyParametersGUI = CreateInstance<IvyParametersGUI>();
            Controller = new ProceduralIvyWindowController();
            Controller.Init(Instance, ivyParametersGUI);

            var res = ProceduralIvyResources.Instance;
            if (res != null)
            {
                windowSkin = res.windowSkin;
                downArrowTex = res.arrowDown;
                materialTex = res.materialIcon;
                leaveTex = res.leafIcon;
                dropdownShadowTex = res.dropdownShadow;
                presetTex = res.presetIcon;
                infoTex = res.infoIcon;
            }

            if (SceneGuiController != null)
                SceneGuiController.Cleanup();

            SceneGuiController = new ProceduralIvySceneGui();
            SceneGuiController.Init(Controller.infoPool);
            
            SceneView.duringSceneGui -= SceneGuiController.OnSceneGUI;
            SceneView.duringSceneGui += SceneGuiController.OnSceneGUI;

            Controller.CreateNewIvy(ProceduralIvyResources.Instance.defaultPreset);
            ivyParametersGUI.CopyFrom(Controller.infoPool.ivyParameters);

            Undo.undoRedoPerformed += MyUndoCallback;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorPrefs.SetBool(KEY_WINDOW_OPENED, true);
        }

        private static void MyUndoCallback()
        {
            Controller.RefreshMesh();
            RefreshEditorValues();
        }

        private void Update()
        {
            if (Controller != null) 
                Controller.Update();
        }

        private void OnDestroy()
        {
            SceneGuiController.Cleanup();
            SceneGuiController = null;
            Controller.Destroy();

            SceneView.RepaintAll();

            EditorPrefs.SetBool(KEY_WINDOW_OPENED, false);
        }

        private void OnGUI()
        {
            oldSkin = GUI.skin;
            GUI.skin = windowSkin;

            EditorGUI.BeginChangeCheck();
            DrawGUI();

            if (EditorGUI.EndChangeCheck() || valueUpdated)
            {
                if (Controller.GenerateLightmapUVsActivated())
                    CustomDisplayDialog.Init(windowSkin, EditorConstants.LIGHTMAP_UVS_WARNING, "Lightmap UVs warning",
                        infoTex, 370f, 155f, null);
                valueUpdated = false;
                SaveParameters();
                Controller.RefreshMesh();
                Repaint();
            }

            GUI.skin = oldSkin;
        }

        private static void AssignLabel(GameObject g)
        {
            var res = ProceduralIvyResources.Instance;
            if (res != null && res.labelIcon != null)
                EditorGUIUtility.SetIconForObject(g, res.labelIcon);
        }

        public void CreateIvyGO(Vector3 position, Vector3 normal)
        {
            Controller.CreateIvyGO(position, normal);
            Selection.activeGameObject = Controller.ivyGO;
            AssignLabel(Controller.ivyGO);
        }

        private static void RefreshEditorValues()
        {
            ivyParametersGUI.CopyFrom(Controller.infoPool.ivyParameters);
        }

        public void SaveParameters()
        {
            Controller.RegisterUndo();
            Controller.infoPool.ivyParameters.DeepCopy(ivyParametersGUI);
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

            mainButtonsZone.DrawZone(this, ivyParametersGUI, windowSkin, Controller, ref YSpace, generalArea,
                bckgColor2);

            generalArea = new Rect(10f, 10f, generalAreaWidth, 520f);

            var generalSettingsYSpace = 0f;

            generalSettingsZone.DrawZone("General settings", 265f, this, ivyParametersGUI, windowSkin,
                Controller, ref YSpace, ref presetDropDownYSpace,
                ref generalSettingsYSpace, generalArea, bckgColor2, animationCurve);

            var branchesAreaYSpace = 0f;

            branchesSettingsZone.DrawZone("Branches settings", 185f, this, ivyParametersGUI, windowSkin,
                Controller, ref YSpace, ref presetDropDownYSpace,
                ref branchesAreaYSpace, generalArea, bckgColor2, animationCurve);

            var leavesAreaYSpace = 0f;

            leavesSettingsZone.DrawZone("Leaves settings", 230f, this, ivyParametersGUI, windowSkin, Controller,
                ref YSpace, ref presetDropDownYSpace,
                ref leavesAreaYSpace, generalArea, bckgColor2, animationCurve);

            var growthAreaYSpace = 0f;

            growthSettings.DrawZone("Growth settings", 260f, this, ivyParametersGUI, windowSkin, Controller,
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

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (EditorPrefs.GetBool(KEY_WINDOW_OPENED, false))
            {
                Init();
                Controller.OnScriptReloaded(ProceduralIvyResources.Instance.defaultPreset);
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