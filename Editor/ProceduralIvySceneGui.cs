#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace TeamCrescendo.ProceduralIvy
{
    public class ProceduralIvySceneGui
    {
        public enum ToolMode
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

        // Public properties for the Overlay to bind to
        public float BrushSize { get; set; } = 100f;
        public AnimationCurve BrushCurve { get; set; } = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public ToolMode CurrentToolMode => toolMode;

        private ToolMode toolMode = ToolMode.None;
        private AMode currentMode;
        
        // Mode Instances
        private ModePaint modePaint;
        private ModeMove modeMove;
        private ModeSmooth modeSmooth;
        private ModeRefine modeRefine;
        private ModeOptimize modeOptimize;
        private ModeCut modeCut;
        private ModeDelete modeDelete;
        private ModeShave modeShave;
        private ModeAddLeaves modeAddLeaves;

        private Event current;
        private int controlID;
        
        private Vector3 mousePoint, mouseNormal;
        private bool rayCast;
        private readonly float smoothIntensity = 1f;

        // "ForbiddenRect" is now effectively zero because the Overlay consumes clicks automatically.
        // We keep the rect parameter in UpdateMode signatures for compatibility, 
        // but pass an empty rect.
        private Rect _dummyRect = new Rect(0,0,0,0);

        public ProceduralIvySceneGui()
        {
            modePaint = new ModePaint();
            modeMove = new ModeMove();
            modeSmooth = new ModeSmooth();
            modeRefine = new ModeRefine();
            modeOptimize = new ModeOptimize();
            modeCut = new ModeCut();
            modeDelete = new ModeDelete();
            modeShave = new ModeShave();
            modeAddLeaves = new ModeAddLeaves();

            ProceduralIvyEditorWindow.OnIvyInfoChanged += OnIvyInfoChanged;
        }

        // Public method for Overlay to switch modes
        public void SetToolMode(ToolMode newMode)
        {
            if (ProceduralIvyEditorWindow.Instance == null)
            {
                Debug.LogWarning("No Procedural Ivy Editor Window instance.");
                return;
            }

            // Toggle off if clicking the same mode
            if (toolMode == newMode)
            {
                toolMode = ToolMode.None;
                currentMode = null;
                Tools.current = Tool.Move; // Restore Unity default tool
                return;
            }

            toolMode = newMode;
            Tools.current = Tool.None; // Disable Unity transform tools

            switch (toolMode)
            {
                case ToolMode.None: currentMode = null; break;
                case ToolMode.Paint: currentMode = modePaint; break;
                case ToolMode.Move: currentMode = modeMove; break;
                case ToolMode.Smooth: currentMode = modeSmooth; break;
                case ToolMode.Refine: currentMode = modeRefine; break;
                case ToolMode.Optimize: currentMode = modeOptimize; break;
                case ToolMode.Cut: currentMode = modeCut; break;
                case ToolMode.Delete: currentMode = modeDelete; break;
                case ToolMode.Shave: currentMode = modeShave; break;
                case ToolMode.AddLeave: currentMode = modeAddLeaves; break;
            }

            IvyInfo info = ProceduralIvyEditorWindow.Instance.CurrentIvyInfo;
            InfoPool pool = info?.infoPool;
            MeshFilter meshFilter = info?.GetComponent<MeshFilter>();
            currentMode?.Init(pool, meshFilter);
            
            SceneView.RepaintAll();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            Assert.IsNotNull(ProceduralIvyEditorWindow.Instance);
            Assert.IsFalse(ProceduralIvyEditorWindow.Instance.IsPlacingSeed);
            
            current = Event.current;
            controlID = GUIUtility.GetControlID(FocusType.Passive);

            currentMode?.Update(current, _dummyRect);

            switch (toolMode)
            {
                case ToolMode.Paint:
                    modePaint.UpdateMode(current, _dummyRect, BrushSize);
                    break;
                case ToolMode.Move:
                    modeMove.UpdateMode(current, _dummyRect, BrushSize, BrushCurve);
                    break;
                case ToolMode.Smooth:
                    modeSmooth.UpdateMode(current, _dummyRect, BrushSize, BrushCurve, smoothIntensity);
                    break;
                case ToolMode.Refine:
                    modeRefine.UpdateMode(current, _dummyRect, BrushSize);
                    break;
                case ToolMode.Optimize:
                    modeOptimize.UpdateMode(current, _dummyRect, BrushSize);
                    break;
                case ToolMode.Cut:
                    modeCut.UpdateMode(current, _dummyRect, BrushSize);
                    break;
                case ToolMode.Delete:
                    modeDelete.UpdateMode(current, _dummyRect, BrushSize);
                    break;
                case ToolMode.Shave:
                    modeShave.UpdateMode(current, _dummyRect, BrushSize);
                    break;
                case ToolMode.AddLeave:
                    modeAddLeaves.UpdateMode(current, _dummyRect, BrushSize);
                    break;
            }

            if (toolMode != ToolMode.None)
            {
                if (current.type == EventType.Layout)
                {
                    HandleUtility.AddDefaultControl(controlID);
                }
            }
        }

        public void Cleanup()
        {
            ProceduralIvyEditorWindow.OnIvyInfoChanged -= OnIvyInfoChanged;
        }

        private void OnIvyInfoChanged(IvyInfo info)
        {
            if (info == null) return;
            if (toolMode != ToolMode.None && currentMode != null && ProceduralIvyEditorWindow.Instance != null)
                currentMode.Init(info.infoPool, info.GetComponent<MeshFilter>());
        }
    }
}
#endif