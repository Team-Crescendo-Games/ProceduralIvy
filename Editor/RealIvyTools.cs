#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Dynamite3D.RealIvy{
	public class RealIvyTools : EditorWindow {
		#region variables
		//public static RealIvyProTools instance;
		//public RealIvyProWindow realIvyProWindow;
        public InfoPool infoPool;

		//Enumerador para las tools
		enum ToolMode{None, Paint, Move, Smooth, Refine, Optimize, Cut, Delete, Shave, AddLeave}
		private ToolMode toolMode = ToolMode.None;
		private string modeLabel = "None";

		//Control de interfaz y intup
		private Event current;
		private int controlID;
        private Vector3 mousePoint, mouseNormal;
        private bool rayCast;
        private bool toolsShown = true;

		//el rect de la pantalla que hay que ignorar
		private Rect forbiddenRect;
        private Rect togglevisibilityButton;

		Dictionary<int, List<BranchContainer>> branchesUndos;

		private float brushSize = 100f;
		private AnimationCurve brushCurve = AnimationCurve.EaseInOut(0f, 0f, 1f,1f);
		private float smoothInsensity = 1f;
		private float highlightX;

		private AbstractMode currentMode;
		private ModePaint modePaint;
		private ModeMove modeMove;
		private ModeSmooth modeSmooth;
		private ModeRefine modeRefine;
		private ModeOptimize modeOptimize;
		private ModeCut modeCut;
		private ModeDelete modeDelete;
		private ModeShave modeShave;
		private ModeAddLeaves modeAddLeaves;

        private static Texture2D modePaintTex, modeMoveTex, modeSmoothTex, modeRefineTex, modeOptimizeTex, modeCutTex, modeDeleteTex, modeShaveTex, modeAddLeavesTex, downArrowTex, upArrowTex;
        private static GUISkin windowSkin;

		#endregion
		public void Init(RealIvyWindow realIvyProWindow, InfoPool infoPool)
		{
			//instance = (RealIvyProTools)EditorWindow.GetWindow(typeof(RealIvyProTools));

			//this.realIvyProWindow = realIvyProWindow;
			this.infoPool = infoPool;

			modePaint = new ModePaint();
			modePaint.realIvyProWindow = realIvyProWindow;
			modeMove = new ModeMove();
			modeSmooth = new ModeSmooth();
			modeRefine = new ModeRefine();
			modeOptimize = new ModeOptimize();
			modeCut = new ModeCut();
			modeDelete = new ModeDelete();
			modeShave = new ModeShave();
			modeAddLeaves = new ModeAddLeaves();

			windowSkin = RealIvyWindow.windowSkin;
			modePaintTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("7433ea681d2ea9a43a91283aec7779dc"), typeof(Texture2D));
			modeMoveTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("baa7fb0fea158214195003054cbdc848"), typeof(Texture2D));
			modeSmoothTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("da81d4c81a2ef904cb9ec2f020f9f0e4"), typeof(Texture2D));
			modeRefineTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("58c807f93561e014ba108036e7ac3788"), typeof(Texture2D));
			modeOptimizeTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("d901243b259e1fb40883227b5a342b16"), typeof(Texture2D));
			modeCutTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("be698aee7a1cd6e4bbac6ef223909851"), typeof(Texture2D));
			modeDeleteTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("4cd137b68f1323f4484094d30b90f926"), typeof(Texture2D));
			modeShaveTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("46994a3c472a4d142964853d08614149"), typeof(Texture2D));
			modeAddLeavesTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("4845df643447cae4499501434c4147b4"), typeof(Texture2D));
            downArrowTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("8ee6aee77df7d3e4485148aa889f9b6b"), typeof(Texture2D));
            upArrowTex = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("47e4d534b75170047b81b09581c4f3d9"), typeof(Texture2D));

            FillGUIContent();

            RealIvyProWindowController.OnIvyGoCreated += OnIvyGoCreated;
		}

        private void FillGUIContent()
        {
            Constants.TOOL_PAINT_GUICONTENT = new GUIContent(modePaintTex, "Paint tool");
            Constants.TOOL_MOVE_GUICONTENT = new GUIContent(modeMoveTex, "Move tool");
            Constants.TOOL_SMOOTH_GUICONTENT = new GUIContent(modeSmoothTex, "Smooth tool");
            Constants.TOOL_REFINE_GUICONTENT = new GUIContent(modeRefineTex, "Refine tool");
            Constants.TOOL_OPTIMIZE_GUICONTENT = new GUIContent(modeOptimizeTex, "Optimize tool");
            Constants.TOOL_CUT_GUICONTENT = new GUIContent(modeCutTex, "Cut tool");
            Constants.TOOL_DELETE_GUICONTENT = new GUIContent(modeDeleteTex, "Delete tool");
            Constants.TOOL_SHAVE_GUICONTENT = new GUIContent(modeShaveTex, "Shave tool");
            Constants.TOOL_ADDLEAVE_GUICONTENT = new GUIContent(modeAddLeavesTex, "Add leave tool");
            Constants.TOOL_TOGGLEPANEL_GUICONTENT = new GUIContent(downArrowTex, "Hide panel");
        }

		public void OnSceneGUI(SceneView sceneView)
		{
			current = Event.current;
			controlID = GUIUtility.GetControlID (FocusType.Passive);

			if(currentMode != null)
			{
				currentMode.Update(current, forbiddenRect); 
			}
            //Después con este switch, llamamos a cada uno de los bucles de las tools una vez por update del scenegui
            switch (toolMode)
			{
				case ToolMode.None:
				{
					ModeNone ();
					break;
				}
				case ToolMode.Paint:
				{
					modePaint.UpdateMode(current, forbiddenRect, brushSize);
					break;
				}
				case ToolMode.Move:
				{
					modeMove.UpdateMode(current, forbiddenRect, brushSize, brushCurve);
					break;
				}
				case ToolMode.Smooth:
				{
					modeSmooth.UpdateMode(current, forbiddenRect, brushSize, brushCurve, smoothInsensity);
					break;
				}
				case ToolMode.Refine:
				{
					modeRefine.UpdateMode(current, forbiddenRect, brushSize);
					break;
				}
				case ToolMode.Optimize:
				{
					modeOptimize.UpdateMode(current, forbiddenRect, brushSize);
					break;
				}
				case ToolMode.Cut:
				{
					modeCut.UpdateMode(current, forbiddenRect, brushSize);
					break;
				}
				case ToolMode.Delete:
				{
					modeDelete.UpdateMode(current, forbiddenRect, brushSize);
					break;
				}
				case ToolMode.Shave:
				{
					modeShave.UpdateMode(current, forbiddenRect, brushSize);
					break;
				}
				case ToolMode.AddLeave:
				{
					modeAddLeaves.UpdateMode(current, forbiddenRect, brushSize);
					break;
				}
			}

            if (RealIvyWindow.instance.placingSeed)
            {
                Handles.color = new Color(0.2f, 1f, 0.3f);
                Handles.DrawSolidDisc(mousePoint, mouseNormal, 0.1f);
                Handles.DrawLine(mousePoint, mousePoint + mouseNormal * 0.2f);
                if (current.type == EventType.MouseDown)
                {
                    if (current.button == 0)
                    {
                        if (!current.control && !current.shift && !current.alt)
                        {
                            if (rayCast)
                            {
								RealIvyWindow.instance.CreateNewIvy();
								RealIvyWindow.instance.CreateIvyGO(mousePoint, mouseNormal);
								RealIvyWindow.instance.placingSeed = false;
                            }
                        }
                    }
                }
                if (current.type == EventType.MouseMove)
                {
                    RayCastSceneView();
                }

				SceneView.RepaintAll();
			}

            //Y con este método tomamos control del sceneviewAA
            TakeControl();


			DrawGUI(sceneView);
		}

		public void QuitWindow()
		{
#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui -= OnSceneGUI;
#else
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif

			RealIvyProWindowController.OnIvyGoCreated -= OnIvyGoCreated;
		}

        private void OnTogglePanel()
        {
            toolsShown = !toolsShown;
            if (toolsShown)
            {
                Constants.TOOL_TOGGLEPANEL_GUICONTENT = new GUIContent(downArrowTex, "Hide panel");
            }
            else
            {
                Constants.TOOL_TOGGLEPANEL_GUICONTENT = new GUIContent(upArrowTex, "Show panel");
            }
        }

		private void DrawGUI(SceneView sceneView)
        {
            //Tengo métodos que triggerean los modos, son los que llaman los botones. En estos métodos hay que meter cualquier configuración o seteo necesarios para entrar en dicho modo
            forbiddenRect = new Rect(sceneView.position.width / 2f - 200f, sceneView.position.height - 116f, 418f, 98f);
            togglevisibilityButton = new Rect(sceneView.position.width / 2f + 218f, sceneView.position.height - 42f, 24f, 24f);
            Texture2D minimizeTex;
            if (toolsShown)
            {
                minimizeTex = downArrowTex;
            }
            else
            {
                minimizeTex = upArrowTex;
            }

            Handles.BeginGUI();

            if (GUI.Button(togglevisibilityButton, Constants.TOOL_TOGGLEPANEL_GUICONTENT, windowSkin.GetStyle("sceneviewbutton")))
            {
                OnTogglePanel();
            }

            if (toolsShown)
            {
                EditorGUI.DrawRect(new Rect(sceneView.position.width / 2f - 202f, sceneView.position.height - 118f, 422f, 100f), new Color(0.1f, 0.1f, 0.1f));

                GUILayout.BeginArea(forbiddenRect);

                EditorGUI.DrawRect(new Rect(0f, 0f, 418f, 98f), new Color(0.45f, 0.45f, 0.45f));

                GUI.Label(new Rect(4f, 4f, 104f, 20f), "Tool:", windowSkin.label);
                GUI.Label(new Rect(4f, 24f, 104f, 24f), modeLabel, windowSkin.GetStyle("sceneviewselected"));

                GUI.Label(new Rect(118f, 4f, 140f, 20f), "Radius:", windowSkin.label);
                brushSize = GUI.HorizontalSlider(new Rect(118f, 31f, 140f, 24f), brushSize, 0f, 2000f);

                GUI.Label(new Rect(274f, 4f, 140f, 20f), "Curve:", windowSkin.label);
                brushCurve = EditorGUI.CurveField(new Rect(274f, 24f, 140f, 24f), brushCurve);

                float XSpace = 14f;

                if (toolMode != ToolMode.None)
                {
                    EditorGUI.DrawRect(new Rect(highlightX, 54f, 40f, 40f), new Color(0.4f, 0.8f, 0f, 1));
                }

                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), Constants.TOOL_PAINT_GUICONTENT, windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Paint)
                    {
                        ToModeNone();
                    }
                    else
                    {
                        ToModePaint();
                    }
                }
                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), Constants.TOOL_MOVE_GUICONTENT, windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Move)
                    {
                        ToModeNone();
                    }
                    else
                    {
                        ToModeMove();
                    }
                }
                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), Constants.TOOL_SMOOTH_GUICONTENT, windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Smooth)
                    {
                        ToModeNone();
                    }
                    else
                    {
                        ToModeSmooth();
                    }
                }
                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), Constants.TOOL_REFINE_GUICONTENT, windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Refine)
                    {
                        ToModeNone();
                    }
                    else
                    {
                        ToModeRefine();
                    }
                }
                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), Constants.TOOL_OPTIMIZE_GUICONTENT, windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Optimize)
                    {
                        ToModeNone();
                    }
                    else
                    {
                        ToModeOptimize();
                    }
                }
                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), Constants.TOOL_CUT_GUICONTENT, windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Cut)
                    {
                        ToModeNone();
                    }
                    else
                    {
                        ToModeCut();
                    }
                }
                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), Constants.TOOL_ADDLEAVE_GUICONTENT, windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.AddLeave)
                    {
                        ToModeNone();
                    }
                    else
                    {
                        ToModeAddLeave();
                    }
                }
                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), Constants.TOOL_SHAVE_GUICONTENT, windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Shave)
                    {
                        ToModeNone();
                    }
                    else
                    {
                        ToModeShave();
                    }
                }
                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), Constants.TOOL_DELETE_GUICONTENT, windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Delete)
                    {
                        ToModeNone();
                    }
                    else
                    {
                        ToModeDelete();
                    }
                }

                if (toolMode != ToolMode.None)
                {
                    currentMode.Init(RealIvyWindow.controller.infoPool, RealIvyWindow.controller.mf, this);
                }

                GUILayout.EndArea();
            }
            Handles.EndGUI();
		}

		private void OnIvyGoCreated()
		{
			if (toolMode != ToolMode.None)
			{
				currentMode.Init(RealIvyWindow.controller.infoPool, RealIvyWindow.controller.mf, this);
			}
		}

		//Método para el modo none
		private void ModeNone(){

		}
		private void ToModeNone(){
			toolMode = ToolMode.None;
			modeLabel = "None";
		}
		private void ToModePaint(){
			Tools.current = Tool.None;
			toolMode = ToolMode.Paint;
			modeLabel = "Paint";
            highlightX = 12f;

            currentMode = modePaint;
		}
		private void ToModeMove(){
			Tools.current = Tool.None;
			toolMode = ToolMode.Move;
			modeLabel = "Move";
            highlightX = 56f;

            currentMode = modeMove;
		}
		private void ToModeSmooth(){
			Tools.current = Tool.None;
			toolMode = ToolMode.Smooth;
			modeLabel = "Smooth";
            highlightX = 100f;

            currentMode = modeSmooth;
		}
		private void ToModeRefine(){
			Tools.current = Tool.None;
			toolMode = ToolMode.Refine;
			modeLabel = "Refine";
            highlightX = 144f;

            currentMode = modeRefine;
		}
		private void ToModeOptimize(){
			Tools.current = Tool.None;
			toolMode = ToolMode.Optimize;
			modeLabel = "Optimize";
            highlightX = 188f;

            currentMode = modeOptimize;
		}
		private void ToModeCut(){
			Tools.current = Tool.None;
			toolMode = ToolMode.Cut;
			modeLabel = "Cut";
            highlightX = 232f;

            currentMode = modeCut;
		}
		private void ToModeAddLeave()
        {
            Tools.current = Tool.None;
            toolMode = ToolMode.AddLeave;
            modeLabel = "Add Leave";
            highlightX = 276f;

            currentMode = modeAddLeaves;
        }
		private void ToModeShave(){
			Tools.current = Tool.None;
			toolMode = ToolMode.Shave;
			modeLabel = "Shave";
            highlightX = 320f;

            currentMode = modeShave;
        }
		private void ToModeDelete()
        {
            Tools.current = Tool.None;
            toolMode = ToolMode.Delete;
            modeLabel = "Remove";
            highlightX = 364f;

            currentMode = modeDelete;
        }

        //Si estamos en algún modo de herramienta, el control es de realivypro
        private void TakeControl(){
			if (toolMode != ToolMode.None || RealIvyWindow.instance.placingSeed || forbiddenRect.Contains(Event.current.mousePosition)) {
				switch (current.type) {
				case EventType.Layout:
					HandleUtility.AddDefaultControl (controlID);
					break;
				}
			}
		}

        private void RayCastSceneView()
        {
            Vector2 mouseScreenPos = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mouseScreenPos);
            RaycastHit RC;
            if (Physics.Raycast(ray, out RC, 2000f, infoPool.ivyParameters.layerMask.value))
            {
                //SceneView.lastActiveSceneView.Repaint();
                mousePoint = RC.point;
                mouseNormal = RC.normal;

                rayCast = true;
            }
            else
            {
                rayCast = false;
            }
        }
    }
}
#endif