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
            Prune,
            Cut,
            Delete,
            Shave,
            AddLeaf
        }

        public float BrushSize { get; set; } = 100f;
        public AnimationCurve BrushCurve { get; set; } = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public ToolMode CurrentToolMode => toolMode;

        private ToolMode toolMode = ToolMode.None;
        private AMode currentMode;
        
        private readonly ModePaint modePaint = new();
        private readonly ModeMove modeMove = new();
        private readonly ModeSmooth modeSmooth = new();
        private readonly ModePrune modePrune = new();
        private readonly ModeCut modeCut = new();
        private readonly ModeDelete modeDelete = new();
        private readonly ModeShave modeShave = new();
        private readonly ModeAddLeaves modeAddLeaves = new();

        private Event current;
        private int controlID;
        
        private Vector3 mousePoint, mouseNormal;
        private bool rayCast;
        private readonly float smoothIntensity = 1f;

        // "ForbiddenRect" is now effectively zero because the Overlay consumes clicks automatically.
        // We keep the rect parameter in UpdateMode signatures for compatibility, 
        // but pass an empty rect.
        private Rect dummyRect = new Rect(0,0,0,0);

        public ProceduralIvySceneGui()
        {
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
                case ToolMode.Prune: currentMode = modePrune; break;
                case ToolMode.Cut: currentMode = modeCut; break;
                case ToolMode.Delete: currentMode = modeDelete; break;
                case ToolMode.Shave: currentMode = modeShave; break;
                case ToolMode.AddLeaf: currentMode = modeAddLeaves; break;
            }

            IvyInfo info = ProceduralIvyEditorWindow.Instance.CurrentIvyInfo;
            InfoPool pool = info?.infoPool;
            currentMode?.Init(pool);
            
            SceneView.RepaintAll();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            Assert.IsNotNull(ProceduralIvyEditorWindow.Instance);
            Assert.IsFalse(ProceduralIvyEditorWindow.Instance.IsPlacingSeed);
            
            current = Event.current;
            controlID = GUIUtility.GetControlID(FocusType.Passive);

            currentMode?.Update(current, dummyRect);

            switch (toolMode)
            {
                case ToolMode.Paint:
                    modePaint.UpdateMode(current, dummyRect);
                    break;
                case ToolMode.Move:
                    modeMove.UpdateMode(current, dummyRect, BrushSize, BrushCurve);
                    break;
                case ToolMode.Smooth:
                    modeSmooth.UpdateMode(current, dummyRect, BrushSize, BrushCurve, smoothIntensity);
                    break;
                case ToolMode.Prune:
                    modePrune.UpdateMode(current, dummyRect, BrushSize);
                    break;
                case ToolMode.Cut:
                    modeCut.UpdateMode(current, dummyRect, BrushSize);
                    break;
                case ToolMode.Delete:
                    modeDelete.UpdateMode(current, dummyRect, BrushSize);
                    break;
                case ToolMode.Shave:
                    modeShave.UpdateMode(current, dummyRect, BrushSize);
                    break;
                case ToolMode.AddLeaf:
                    modeAddLeaves.UpdateMode(current, dummyRect, BrushSize);
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
                currentMode.Init(info.infoPool);
        }
    }
}
#endif