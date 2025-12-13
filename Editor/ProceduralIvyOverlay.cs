using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamCrescendo.ProceduralIvy
{
    [Overlay(typeof(SceneView), "Procedural Ivy Tool", true)]
    public class ProceduralIvyOverlay : Overlay
    {
        private ProceduralIvySceneGui controller;

        public override VisualElement CreatePanelContent()
        {
            controller = ProceduralIvyEditorWindow.Instance?.SceneGuiController;

            var root = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingBottom = 5,
                    paddingTop = 5,
                    paddingLeft = 5,
                    paddingRight = 5,
                    minWidth = 350,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f),
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5,
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5
                }
            };

            if (controller == null)
            {
                root.Add(new Label("Open Procedural Ivy Window first."));
                return root;
            }

            var settingsRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5 } };
            
            var slider = new Slider("Radius", 0f, 2000f)
            {
                value = controller.BrushSize,
                style = { flexGrow = 1, marginRight = 10 }
            };
            slider.RegisterValueChangedCallback(evt => controller.BrushSize = evt.newValue);
            settingsRow.Add(slider);

            var curveContainer = new IMGUIContainer(() =>
            {
                controller.BrushCurve = EditorGUILayout.CurveField("", controller.BrushCurve, GUILayout.Width(50), GUILayout.Height(15));
            })
            {
                style = { width = 60, justifyContent = Justify.Center }
            };
            settingsRow.Add(new Label("Falloff:"));
            settingsRow.Add(curveContainer);

            root.Add(settingsRow);

            var toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    justifyContent = Justify.Center
                }
            };

            void CreateToolButton(string name, Texture2D icon, ProceduralIvySceneGui.ToolMode mode)
            {
                var btn = new Button(() => controller.SetToolMode(mode))
                {
                    tooltip = name,
                    style =
                    {
                        width = 32,
                        height = 32,
                        backgroundImage = icon,
                        marginRight = 2,
                        marginLeft = 2
                    }
                };
                
                toolbar.Add(btn);
            }

            var res = ProceduralIvyResources.Instance;
            if (res != null)
            {
                CreateToolButton("Paint", res.paintTool, ProceduralIvySceneGui.ToolMode.Paint);
                CreateToolButton("Move", res.moveTool, ProceduralIvySceneGui.ToolMode.Move);
                CreateToolButton("Smooth", res.smoothTool, ProceduralIvySceneGui.ToolMode.Smooth);
                CreateToolButton("Refine", res.refineTool, ProceduralIvySceneGui.ToolMode.Refine);
                CreateToolButton("Optimize", res.optimizeTool, ProceduralIvySceneGui.ToolMode.Optimize);
                CreateToolButton("Cut", res.cutTool, ProceduralIvySceneGui.ToolMode.Cut);
                CreateToolButton("Add Leaf", res.addLeavesTool, ProceduralIvySceneGui.ToolMode.AddLeave);
                CreateToolButton("Shave", res.shaveTool, ProceduralIvySceneGui.ToolMode.Shave);
                CreateToolButton("Delete", res.deleteTool, ProceduralIvySceneGui.ToolMode.Delete);
            }
            else
            {
                toolbar.Add(new Label("Resources missing."));
            }

            root.Add(toolbar);
            
            var statusLabel = new Label { text = "Current Mode: None", style = { alignSelf = Align.Center, marginTop = 3, fontSize = 10 } };
            root.schedule.Execute(() => statusLabel.text = $"Mode: {controller.CurrentToolMode}").Every(200);
            root.Add(statusLabel);

            return root;
        }
    }
}