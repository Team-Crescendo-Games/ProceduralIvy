#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ProceduralIvySceneGui
    {
        private enum ToolMode
        {
            None,
            Paint,
            Move,
            Smooth,
            Refine,
            Optimize,
            Cut,
            Delete,
            Shave,
            AddLeave
        }
        
        private Texture2D modePaintTex,
            modeMoveTex,
            modeSmoothTex,
            modeRefineTex,
            modeOptimizeTex,
            modeCutTex,
            modeDeleteTex,
            modeShaveTex,
            modeAddLeavesTex,
            downArrowTex,
            upArrowTex;

        private GUISkin windowSkin;
        public InfoPool infoPool;

        private Dictionary<int, List<BranchContainer>> branchesUndos;
        private AnimationCurve brushCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private float brushSize = 100f;
        private int controlID;

        private Event current;

        private AMode currentMode;

        private Rect forbiddenRect;
        private float highlightX;
        private ModeAddLeaves modeAddLeaves;
        private ModeCut modeCut;
        private ModeDelete modeDelete;
        private string modeLabel = "None";
        private ModeMove modeMove;
        private ModeOptimize modeOptimize;
        private ModePaint modePaint;
        private ModeRefine modeRefine;
        private ModeShave modeShave;
        private ModeSmooth modeSmooth;
        private Vector3 mousePoint, mouseNormal;
        private bool rayCast;
        private readonly float smoothIntensity = 1f;
        private Rect toggleVisibilityButton;
        private ToolMode toolMode = ToolMode.None;
        private bool toolsShown = true;

        public void Init(InfoPool infoPool)
        {
            this.infoPool = infoPool;

            modePaint = new ModePaint();
            modeMove = new ModeMove();
            modeSmooth = new ModeSmooth();
            modeRefine = new ModeRefine();
            modeOptimize = new ModeOptimize();
            modeCut = new ModeCut();
            modeDelete = new ModeDelete();
            modeShave = new ModeShave();
            modeAddLeaves = new ModeAddLeaves();
            
            var res = ProceduralIvyResources.Instance;
            if (res == null) return;

            windowSkin = res.windowSkin;

            // Map the textures from the resource file
            modePaintTex = res.paintTool;
            modeMoveTex = res.moveTool;
            modeSmoothTex = res.smoothTool;
            modeRefineTex = res.refineTool;
            modeOptimizeTex = res.optimizeTool;
            modeCutTex = res.cutTool;
            modeDeleteTex = res.deleteTool;
            modeShaveTex = res.shaveTool;
            modeAddLeavesTex = res.addLeavesTool;
    
            downArrowTex = res.arrowDown;
            upArrowTex = res.arrowUp;

            FillGUIContent();

            ProceduralIvyWindowController.OnIvyGoCreated += OnIvyGoCreated;
        }

        private void FillGUIContent()
        {
            EditorConstants.TOOL_PAINT_GUICONTENT = new GUIContent(modePaintTex, "Paint tool");
            EditorConstants.TOOL_MOVE_GUICONTENT = new GUIContent(modeMoveTex, "Move tool");
            EditorConstants.TOOL_SMOOTH_GUICONTENT = new GUIContent(modeSmoothTex, "Smooth tool");
            EditorConstants.TOOL_REFINE_GUICONTENT = new GUIContent(modeRefineTex, "Refine tool");
            EditorConstants.TOOL_OPTIMIZE_GUICONTENT = new GUIContent(modeOptimizeTex, "Optimize tool");
            EditorConstants.TOOL_CUT_GUICONTENT = new GUIContent(modeCutTex, "Cut tool");
            EditorConstants.TOOL_DELETE_GUICONTENT = new GUIContent(modeDeleteTex, "Delete tool");
            EditorConstants.TOOL_SHAVE_GUICONTENT = new GUIContent(modeShaveTex, "Shave tool");
            EditorConstants.TOOL_ADDLEAVE_GUICONTENT = new GUIContent(modeAddLeavesTex, "Add leave tool");
            EditorConstants.TOOL_TOGGLEPANEL_GUICONTENT = new GUIContent(downArrowTex, "Hide panel");
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            current = Event.current;
            controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (currentMode != null) currentMode.Update(current, forbiddenRect);
            //Después con este switch, llamamos a cada uno de los bucles de las tools una vez por update del scenegui
            switch (toolMode)
            {
                case ToolMode.None:
                {
                    ModeNone();
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
                    modeSmooth.UpdateMode(current, forbiddenRect, brushSize, brushCurve, smoothIntensity);
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

            if (ProceduralIvyWindow.Instance.placingSeed)
            {
                Handles.color = new Color(0.2f, 1f, 0.3f);
                Handles.DrawSolidDisc(mousePoint, mouseNormal, 0.1f);
                Handles.DrawLine(mousePoint, mousePoint + mouseNormal * 0.2f);
                if (current.type == EventType.MouseDown)
                    if (current.button == 0)
                        if (!current.control && !current.shift && !current.alt)
                            if (rayCast)
                            {
                                ProceduralIvyWindow.Controller.CreateNewIvy();
                                ProceduralIvyWindow.Instance.CreateIvyGO(mousePoint, mouseNormal);
                                ProceduralIvyWindow.Instance.placingSeed = false;
                            }

                if (current.type == EventType.MouseMove) RayCastSceneView();

                SceneView.RepaintAll();
            }

            TakeControl();

            DrawGUI(sceneView);
        }

        public void Cleanup()
        {
            ProceduralIvyWindowController.OnIvyGoCreated -= OnIvyGoCreated;
        }

        private void OnTogglePanel()
        {
            toolsShown = !toolsShown;
            if (toolsShown)
                EditorConstants.TOOL_TOGGLEPANEL_GUICONTENT = new GUIContent(downArrowTex, "Hide panel");
            else
                EditorConstants.TOOL_TOGGLEPANEL_GUICONTENT = new GUIContent(upArrowTex, "Show panel");
        }

        private void DrawGUI(SceneView sceneView)
        {
            //Tengo métodos que triggerean los modos, son los que llaman los botones. En estos métodos hay que meter cualquier configuración o seteo necesarios para entrar en dicho modo
            forbiddenRect = new Rect(sceneView.position.width / 2f - 200f, sceneView.position.height - 116f, 418f, 98f);
            toggleVisibilityButton = new Rect(sceneView.position.width / 2f + 218f, sceneView.position.height - 42f,
                24f, 24f);

            Handles.BeginGUI();

            if (GUI.Button(toggleVisibilityButton, EditorConstants.TOOL_TOGGLEPANEL_GUICONTENT,
                    windowSkin.GetStyle("sceneviewbutton"))) OnTogglePanel();

            if (toolsShown)
            {
                EditorGUI.DrawRect(
                    new Rect(sceneView.position.width / 2f - 202f, sceneView.position.height - 118f, 422f, 100f),
                    new Color(0.1f, 0.1f, 0.1f));

                GUILayout.BeginArea(forbiddenRect);

                EditorGUI.DrawRect(new Rect(0f, 0f, 418f, 98f), new Color(0.45f, 0.45f, 0.45f));

                GUI.Label(new Rect(4f, 4f, 104f, 20f), "Tool:", windowSkin.label);
                GUI.Label(new Rect(4f, 24f, 104f, 24f), modeLabel, windowSkin.GetStyle("sceneviewselected"));

                GUI.Label(new Rect(118f, 4f, 140f, 20f), "Radius:", windowSkin.label);
                brushSize = GUI.HorizontalSlider(new Rect(118f, 31f, 140f, 24f), brushSize, 0f, 2000f);

                GUI.Label(new Rect(274f, 4f, 140f, 20f), "Curve:", windowSkin.label);
                brushCurve = EditorGUI.CurveField(new Rect(274f, 24f, 140f, 24f), brushCurve);

                var XSpace = 14f;

                if (toolMode != ToolMode.None)
                    EditorGUI.DrawRect(new Rect(highlightX, 54f, 40f, 40f), new Color(0.4f, 0.8f, 0f, 1));

                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), EditorConstants.TOOL_PAINT_GUICONTENT,
                        windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Paint)
                        ToModeNone();
                    else
                        ToModePaint();
                }

                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), EditorConstants.TOOL_MOVE_GUICONTENT,
                        windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Move)
                        ToModeNone();
                    else
                        ToModeMove();
                }

                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), EditorConstants.TOOL_SMOOTH_GUICONTENT,
                        windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Smooth)
                        ToModeNone();
                    else
                        ToModeSmooth();
                }

                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), EditorConstants.TOOL_REFINE_GUICONTENT,
                        windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Refine)
                        ToModeNone();
                    else
                        ToModeRefine();
                }

                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), EditorConstants.TOOL_OPTIMIZE_GUICONTENT,
                        windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Optimize)
                        ToModeNone();
                    else
                        ToModeOptimize();
                }

                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), EditorConstants.TOOL_CUT_GUICONTENT,
                        windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Cut)
                        ToModeNone();
                    else
                        ToModeCut();
                }

                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), EditorConstants.TOOL_ADDLEAVE_GUICONTENT,
                        windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.AddLeave)
                        ToModeNone();
                    else
                        ToModeAddLeave();
                }

                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), EditorConstants.TOOL_SHAVE_GUICONTENT,
                        windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Shave)
                        ToModeNone();
                    else
                        ToModeShave();
                }

                XSpace += 44f;
                if (GUI.Button(new Rect(XSpace, 56f, 36f, 36f), EditorConstants.TOOL_DELETE_GUICONTENT,
                        windowSkin.GetStyle("sceneviewbutton")))
                {
                    if (toolMode == ToolMode.Delete)
                        ToModeNone();
                    else
                        ToModeDelete();
                }

                if (toolMode != ToolMode.None)
                    currentMode.Init(ProceduralIvyWindow.Controller.infoPool, ProceduralIvyWindow.Controller.mf);

                GUILayout.EndArea();
            }

            Handles.EndGUI();
        }

        private void OnIvyGoCreated()
        {
            if (toolMode != ToolMode.None)
                currentMode.Init(ProceduralIvyWindow.Controller.infoPool, ProceduralIvyWindow.Controller.mf);
        }

        private void ModeNone()
        {
        }

        private void ToModeNone()
        {
            toolMode = ToolMode.None;
            modeLabel = "None";
        }

        private void ToModePaint()
        {
            Tools.current = Tool.None;
            toolMode = ToolMode.Paint;
            modeLabel = "Paint";
            highlightX = 12f;

            currentMode = modePaint;
        }

        private void ToModeMove()
        {
            Tools.current = Tool.None;
            toolMode = ToolMode.Move;
            modeLabel = "Move";
            highlightX = 56f;

            currentMode = modeMove;
        }

        private void ToModeSmooth()
        {
            Tools.current = Tool.None;
            toolMode = ToolMode.Smooth;
            modeLabel = "Smooth";
            highlightX = 100f;

            currentMode = modeSmooth;
        }

        private void ToModeRefine()
        {
            Tools.current = Tool.None;
            toolMode = ToolMode.Refine;
            modeLabel = "Refine";
            highlightX = 144f;

            currentMode = modeRefine;
        }

        private void ToModeOptimize()
        {
            Tools.current = Tool.None;
            toolMode = ToolMode.Optimize;
            modeLabel = "Optimize";
            highlightX = 188f;

            currentMode = modeOptimize;
        }

        private void ToModeCut()
        {
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

        private void ToModeShave()
        {
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
        private void TakeControl()
        {
            if (toolMode != ToolMode.None || ProceduralIvyWindow.Instance.placingSeed ||
                forbiddenRect.Contains(Event.current.mousePosition))
                switch (current.type)
                {
                    case EventType.Layout:
                        HandleUtility.AddDefaultControl(controlID);
                        break;
                }
        }

        private void RayCastSceneView()
        {
            var mouseScreenPos = Event.current.mousePosition;
            var ray = HandleUtility.GUIPointToWorldRay(mouseScreenPos);
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