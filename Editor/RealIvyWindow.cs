using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Dynamite3D.RealIvy
{
    public class RealIvyWindow : EditorWindow
    {
		private const string KEY_WINDOW_OPENED = "RealIvyProWindow_Opened";
		private const string GUID_DEFAULT_PRESET = "d022a91abdaf78e429b3aa20f69127dd";

        public static RealIvyWindow instance;
		public static RealIvyTools realIvyProToolsWindow;

		public static RealIvyProWindowController controller;


        public static IvyParametersGUI ivyParametersGUI;
		//private IvyParameters preset;
		//private string presetGUID;

		public bool placingSeed = false;

		private UIZone_MainButtons mainButtonsZone = new UIZone_MainButtons();
		private UIZone_GeneralSettings generalSettingsZone = new UIZone_GeneralSettings();
		private UIZone_BranchesSettings branchesSettingsZone = new UIZone_BranchesSettings();
		private UIZone_LeavesSettings leavesSettingsZone = new UIZone_LeavesSettings();
		private UIZone_GrowthSettings growthSettings = new UIZone_GrowthSettings();
		//private UIZone_Presets presetsZone = new UIZone_Presets();

		#region GUIVariables
		//GUI variables
		public GUISkin oldSkin;

		public Vector2 generalScrollView;
        public float YSpace = 0f;
        public Rect controlsArea;
        public Rect generalArea;
        public Color bckgColor = new Color(0.45f, 0.45f, 0.45f);
        public Color bckgColor2 = new Color(0.40f, 0.40f, 0.40f);
        public static GUISkin windowSkin;
        public static Texture2D downArrowTex, materialTex, leaveTex, dropdownShadowTex, presetTex, infoTex;
        public Vector2 leavesPrefabsScrollView;
        public bool valueUpdated;
        
		//Todas estas variables son para las zonas escondibles de la interfaz
        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
		//Variables para el funcionamiento de los slider de los inputfield
        public bool updatingValue;
        public float originalUpdatingValue;
        public float updatingValueMultiplier;
        public float mouseStartPoint;
        public IvyParameter updatingParameter;
        //Variables de gestión de presets
        //public List<IvyParameters> presets = new List<IvyParameters>();
        public List<string> presetsPaths = new List<string>();
        public int presetSelected;
        public bool parametersChanged;
        public List<int> presetsChanged = new List<int>();
        #endregion

        [MenuItem("Tools/3Dynamite/Real Ivy")]
        public static void Init()
        {
			Init(true);
		}

		public static void Init(bool createNewIvy)
		{
			instance = (RealIvyWindow)EditorWindow.GetWindow(typeof(RealIvyWindow));

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

			ivyParametersGUI = ScriptableObject.CreateInstance<IvyParametersGUI>();
			controller = ScriptableObject.CreateInstance<RealIvyProWindowController>();
			controller.Init(instance, ivyParametersGUI);

			windowSkin = (GUISkin)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("b0545e8c97ca8684182a76c2fb22c7ff"), typeof(GUISkin));
			downArrowTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("8ee6aee77df7d3e4485148aa889f9b6b"), typeof(Texture2D));
			materialTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("eb3b714e29c31744888e1bc4bcfe23d6"), typeof(Texture2D));
			leaveTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("14bbaf6e0a8b00f4ea30434e5eeeaf8c"), typeof(Texture2D));
			dropdownShadowTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("9cd9a16c9e229684983f50ff07427219"), typeof(Texture2D));
            presetTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("9dd821bf05e345d4a8a501a8768c7144"), typeof(Texture2D));
            infoTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("d73d5146604f9594996de4e08eec4bdf"), typeof(Texture2D));

            Undo.undoRedoPerformed += MyUndoCallback;

			IvyPreset defaultPresset = GetDefaultPreset();


			if (realIvyProToolsWindow != null)
			{
				realIvyProToolsWindow.QuitWindow();
			}
            CreateTools();

            if (createNewIvy)
			{
				controller.CreateNewIvy(defaultPresset);
				ivyParametersGUI.CopyFrom(controller.infoPool.ivyParameters);
			}			
		}

        public static void AssignLabel(GameObject g)
        {
            Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("a1ca40cfe045c6c4a80354e9c26cd083"), typeof(Texture2D));
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
            realIvyProToolsWindow = ScriptableObject.CreateInstance<RealIvyTools>();
            realIvyProToolsWindow.Init(instance, controller.infoPool);

#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui -= realIvyProToolsWindow.OnSceneGUI;
			SceneView.duringSceneGui += realIvyProToolsWindow.OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= realIvyProToolsWindow.OnSceneGUI;
            SceneView.onSceneGUIDelegate += realIvyProToolsWindow.OnSceneGUI;
#endif
        }

        void OnDestroy()
        {
			realIvyProToolsWindow.QuitWindow();
			DestroyImmediate(realIvyProToolsWindow);
			controller.Destroy();
			
			SceneView.RepaintAll();

			EditorPrefs.SetBool(KEY_WINDOW_OPENED, false);
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

        void OnGUI()
        {
            if (!realIvyProToolsWindow)
            {
                CreateTools();
            }
            oldSkin = GUI.skin;
            GUI.skin = windowSkin;

            EditorGUI.BeginChangeCheck();
            DrawGUI();

            if (EditorGUI.EndChangeCheck() || valueUpdated)
            {
                if (controller.GenerateLightmapUVsActivated())
                {
                    CustomDisplayDialog.Init(windowSkin, Constants.LIGHTMAP_UVS_WARNING, "Lightmap UVs warning", RealIvyWindow.infoTex, 370f, 155f, null);
                }
                valueUpdated = false;
                SaveParameters();
                controller.RefreshMesh();
				Repaint();
            }

            GUI.skin = oldSkin;
        }

        void DrawGUI()
        {
            EditorGUI.DrawRect(new Rect(0f, 0f, this.position.width, this.position.height), bckgColor);
            generalScrollView = GUI.BeginScrollView(new Rect(0f, 0f, this.position.width, this.position.height), generalScrollView, new Rect(0f, 0f, this.position.width - 17f, YSpace), false, false);

            //Pequeña lógica para la responsividad en caso de tener barra de scroll o no 
            float generalAreaWidth;
            if (YSpace > this.position.height)
            {
                generalAreaWidth = this.position.width - 34f;
            }
            else
            {
                generalAreaWidth = this.position.width - 20f;
            }            

            YSpace = 0f;

			float presetDropDownYSpace = 0f;


			mainButtonsZone.DrawZone(this, ivyParametersGUI, windowSkin, controller, ref YSpace, generalArea, bckgColor2);

			generalArea = new Rect(10f, 10f, generalAreaWidth, 520f);

			float generalSettingsYSpace = 0f;

			generalSettingsZone.DrawZone("General settings", 265f, this, ivyParametersGUI, windowSkin,
				controller, ref YSpace, ref presetDropDownYSpace,
				ref generalSettingsYSpace, generalArea, bckgColor2, animationCurve);

			float branchesAreaYSpace = 0f;

			branchesSettingsZone.DrawZone("Branches settings", 185f, this, ivyParametersGUI, windowSkin,
				controller, ref YSpace, ref presetDropDownYSpace,
				ref branchesAreaYSpace, generalArea, bckgColor2, animationCurve);

			float leavesAreaYSpace = 0f;

			leavesSettingsZone.DrawZone("Leaves settings", 230f, this, ivyParametersGUI, windowSkin, controller,
				ref YSpace, ref presetDropDownYSpace,
				ref leavesAreaYSpace, generalArea, bckgColor2, animationCurve);

			float growthAreaYSpace = 0f;

			growthSettings.DrawZone("Growth settings", 260f, this, ivyParametersGUI, windowSkin, controller,
				ref YSpace, ref presetDropDownYSpace, ref growthAreaYSpace,
				generalArea, bckgColor2, animationCurve);


            GUI.EndScrollView();

			if (updatingValue)
			{
				UpdateValue();
			}
		}

        public void OrientationToggle(float XSpace, float YSpace)
        {
            Rect rect = new Rect(XSpace, YSpace, 100f, 50f);
            GUIStyle globalStyle = windowSkin.GetStyle("toggleroundoff");
            GUIStyle localStyle = windowSkin.GetStyle("toggleroundon");
            if (ivyParametersGUI.globalOrientation)
            {
                globalStyle = windowSkin.GetStyle("toggleroundon");
                localStyle = windowSkin.GetStyle("toggleroundoff");
            }
            GUI.Label(new Rect(rect.x, rect.y + 4f, rect.width / 2f, rect.height / 3f), "Global", windowSkin.GetStyle("intfloatfieldlabel"));
            if (GUI.Button(new Rect(rect.x + 15f, rect.y + rect.height / 8f + 20f, rect.height / 3f * 2f, rect.height / 3f * 2f), "", globalStyle))
            {
                ivyParametersGUI.globalOrientation = true;
                valueUpdated = true;
            }
            GUI.Label(new Rect(rect.x + rect.width / 2f + 15f, rect.y + 4f, rect.width / 2f, rect.height / 3f), "Local", windowSkin.GetStyle("intfloatfieldlabel"));
            if (GUI.Button(new Rect(rect.x + rect.width / 2f + 28f, rect.y + rect.height / 8f + 20f, rect.height / 3f * 2f, rect.height / 3f * 2f), "", localStyle))
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

		void UpdateValue()
        {
            Event evt = Event.current;
            if (updatingValue && evt != null)
            {
                switch (evt.rawType)
                {
                    case EventType.MouseUp:
                        updatingValue = false;
                        break;
                }
            }

            float delta = GUIUtility.GUIToScreenPoint(Event.current.mousePosition).x - mouseStartPoint;
            float value = originalUpdatingValue + delta * updatingValueMultiplier;

            updatingParameter.UpdateValue(value);
            valueUpdated = true;
            Repaint();
        }

		private void ValueUpdated()
		{
			valueUpdated = true;
			Repaint();
		}

        void UpdateArea(ref float areaHeight, float areaMaxHeight, ref bool areaAnim, ref bool areaState, ref float areaTime)
        {
            if (areaAnim)
            {
                if (areaState)
                {
                    areaTime += 0.04f;
                }
                else
                {
                    areaTime -= 0.04f;
                }

                areaHeight = Mathf.Lerp(0, areaMaxHeight, animationCurve.Evaluate(areaTime));

                if (areaTime >= 1f)
                {
                    areaTime = 1f;
                    areaHeight = areaMaxHeight;
                    areaAnim = false;
                }
                else if (areaTime <= 0f)
                {
                    areaTime = 0f;
                    areaHeight = 0f;
                    areaAnim = false;
                }
                Repaint();
            }
        }


		private static IvyPreset GetDefaultPreset()
		{
			IvyPreset res = null;
			string defaultPresetGUID = EditorPrefs.GetString("RealIvyDefaultGUID", GUID_DEFAULT_PRESET);

			res = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(defaultPresetGUID), typeof(IvyPreset)) as IvyPreset;

			if(res == null)
			{
				defaultPresetGUID = GUID_DEFAULT_PRESET;
				res = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(defaultPresetGUID), typeof(IvyPreset)) as IvyPreset;
			}

			return res;
		}

        void Update()
        {
            controller.Update();
        }

		[DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			if (EditorPrefs.GetBool(KEY_WINDOW_OPENED, false))
			{
				Init();
				IvyPreset preset = GetDefaultPreset(); 
				controller.OnScriptReloaded(preset);
			}
		}

		private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
		{
			OnScriptsReloaded();
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if(state == PlayModeStateChange.EnteredEditMode)
			{
				OnScriptsReloaded();
			}
		}
	}
}