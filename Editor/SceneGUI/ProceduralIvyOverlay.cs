using System.Collections.Generic;
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
        
        private readonly Dictionary<ProceduralIvySceneGui.ToolMode, string> toolTips = new()
        {
            { ProceduralIvySceneGui.ToolMode.Paint, "Instantiate new ivy/branch" },
            { ProceduralIvySceneGui.ToolMode.Move, "Drag points on a paint brush" },
            { ProceduralIvySceneGui.ToolMode.Smooth, "Smooth branch curves" },
            { ProceduralIvySceneGui.ToolMode.Prune, "Prune: Merge nearby branch points" },
            { ProceduralIvySceneGui.ToolMode.Cut, "Cut branches" },
            { ProceduralIvySceneGui.ToolMode.AddLeaf, "Add leaves manually" },
            { ProceduralIvySceneGui.ToolMode.Shave, "Shave: Delete leaves" },
            { ProceduralIvySceneGui.ToolMode.Delete, "Delete entire branches" }
        };

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
            
            var radiusField = new FloatField("Radius")
            {
                value = controller.BrushSize,
                style = { flexGrow = 1, marginRight = 10 }
            };
            radiusField.RegisterValueChangedCallback(evt => controller.BrushSize = evt.newValue);
            settingsRow.Add(radiusField);

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

            var toolButtons = new Dictionary<ProceduralIvySceneGui.ToolMode, Button>();

            void CreateToolButton(string name, Texture2D icon, ProceduralIvySceneGui.ToolMode mode)
            {
                string tip = toolTips.ContainsKey(mode) ? toolTips[mode] : "";
                
                var btn = new Button(() => controller.SetToolMode(mode))
                {
                    tooltip = $"{name}: {tip}", // Tooltip shows Name + Description
                    style =
                    {
                        width = 32,
                        height = 32,
                        backgroundImage = icon,
                        marginRight = 2,
                        marginLeft = 2,
                        backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f),
                    }
                };
                
                toolbar.Add(btn);
                toolButtons.Add(mode, btn);
            }

            var res = ProceduralIvyResources.Instance;
            if (res != null)
            {
                CreateToolButton("Paint", res.paintTool, ProceduralIvySceneGui.ToolMode.Paint);
                CreateToolButton("Move", res.moveTool, ProceduralIvySceneGui.ToolMode.Move);
                CreateToolButton("Smooth", res.smoothTool, ProceduralIvySceneGui.ToolMode.Smooth);
                CreateToolButton("Prune", res.pruneTool, ProceduralIvySceneGui.ToolMode.Prune);
                CreateToolButton("Cut", res.cutTool, ProceduralIvySceneGui.ToolMode.Cut);
                CreateToolButton("Add Leaf", res.addLeavesTool, ProceduralIvySceneGui.ToolMode.AddLeaf);
                CreateToolButton("Shave", res.shaveTool, ProceduralIvySceneGui.ToolMode.Shave);
                CreateToolButton("Delete", res.deleteTool, ProceduralIvySceneGui.ToolMode.Delete);
            }
            else
                toolbar.Add(new Label("Resources missing."));

            root.Add(toolbar);
            
            var statusLabel = new Label 
            { 
                text = "Mode: None", 
                style = 
                { 
                    alignSelf = Align.Center, 
                    marginTop = 5, 
                    fontSize = 11,
                    color = new Color(0.8f, 0.8f, 0.8f, 1f),
                    whiteSpace = WhiteSpace.Normal,
                    alignItems = Align.Center
                } 
            };
            root.Add(statusLabel);

            root.schedule.Execute(() => 
            {
                var currentMode = controller.CurrentToolMode;
                
                // Update Label Text with Mode + Tip
                if (toolTips.TryGetValue(currentMode, out string tip))
                    statusLabel.text = $"{currentMode.ToString().ToUpper()}\n{tip}";
                else
                    statusLabel.text = $"Mode: {currentMode}";

                // Update Button Visuals
                foreach (var kvp in toolButtons)
                {
                    var mode = kvp.Key;
                    var btn = kvp.Value;

                    if (mode == currentMode)
                    {
                        Color activeColor;
                        switch (mode)
                        {
                            case ProceduralIvySceneGui.ToolMode.Paint:
                                activeColor = EditorConstants.PaintBrushColor;
                                break;
                            case ProceduralIvySceneGui.ToolMode.Move:
                                activeColor = EditorConstants.MoveBrushColor;
                                break;
                            case ProceduralIvySceneGui.ToolMode.Smooth:
                                activeColor = EditorConstants.SmoothBrushColor;
                                break;
                            case ProceduralIvySceneGui.ToolMode.Prune:
                                activeColor = EditorConstants.PruneBrushColor;
                                break;
                            case ProceduralIvySceneGui.ToolMode.Cut:
                                activeColor = EditorConstants.CutBrushColor;
                                break;
                            case ProceduralIvySceneGui.ToolMode.AddLeaf:
                                activeColor = EditorConstants.AddLeavesBrushColor;
                                break;
                            case ProceduralIvySceneGui.ToolMode.Shave:
                                activeColor = EditorConstants.ShaveBrushColor;
                                break;
                            case ProceduralIvySceneGui.ToolMode.Delete:
                                activeColor = EditorConstants.DeleteBrushColor;
                                break;
                            default:
                                activeColor = new Color(0.0f, 0.5f, 0.8f, 1f);
                                break;
                        }

                        btn.style.backgroundColor = activeColor;
                    }
                    else
                    {
                        btn.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                    }
                }
            }).Every(100);

            return root;
        }
    }
}