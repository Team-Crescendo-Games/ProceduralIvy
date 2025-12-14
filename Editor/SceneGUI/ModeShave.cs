using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeShave : AMode
    {
        private bool shaving;
        
        private readonly List<LeafPoint> overLeaves = new();
        
        private void SelectLeavesSS(Vector2 mousePosition, float brushSize)
        {
            if (cursorSelectedBranch != null)
            {
                overLeaves.Clear();
                for (var i = 0; i < cursorSelectedBranch.leaves.Count; i++)
                {
                    if ((cursorSelectedBranch.leaves[i].GetScreenspacePosition() - mousePosition).magnitude < brushSize * 0.1f)
                        overLeaves.Add(cursorSelectedBranch.leaves[i]);
                }
            }
        }

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (toolPaintingAllowed)
            {
                SelectBranchPointSS(currentEvent.mousePosition, brushSize);

                if (cursorSelectedBranch != null)
                {
                    DrawBrushPreview(currentEvent, brushSize);
                    SelectLeavesSS(currentEvent.mousePosition, brushSize);
                    
                    if (overLeaves.Count > 0)
                        DrawOverLeaves();
                    
                    bool canUseTool = !forbiddenRect.Contains(currentEvent.mousePosition) || GUIUtility.hotControl == controlID;

                    if (canUseTool && !currentEvent.alt && currentEvent.button == 0)
                    {
                        switch (currentEvent.type)
                        {
                            case EventType.MouseDown:
                                GUIUtility.hotControl = controlID;
                                shaving = true;
                                
                                PerformShave();
                                currentEvent.Use();
                                break;

                            case EventType.MouseDrag:
                                if (GUIUtility.hotControl == controlID && shaving)
                                {
                                    // Update selection while dragging to catch new leaves
                                    SelectLeavesSS(currentEvent.mousePosition, brushSize);
                                    if (overLeaves.Count > 0)
                                    {
                                        PerformShave();
                                    }
                                    currentEvent.Use();
                                }
                                break;

                            case EventType.MouseUp:
                                if (GUIUtility.hotControl == controlID)
                                {
                                    shaving = false;
                                    GUIUtility.hotControl = 0;
                                    currentEvent.Use();
                                }
                                break;
                        }
                    }
                }
            }
            
            // Safety release
            if (currentEvent.type == EventType.MouseLeaveWindow && shaving)
            {
                shaving = false;
                GUIUtility.hotControl = 0;
            }
            
            SceneView.RepaintAll();
        }

        private void PerformShave()
        {
            if (overLeaves.Count > 0)
            {
                cursorSelectedBranch.RemoveLeaves(overLeaves);
                RefreshMesh(true, true);
                
                // Clear list immediately so we don't try to delete them again next frame
                overLeaves.Clear(); 
            }
        }

        private void DrawOverLeaves()
        {
            Handles.color = EditorConstants.ShaveBrushColor;
            foreach (var leaf in overLeaves)
                Handles.CubeHandleCap(0, leaf.point, Quaternion.identity, 0.04f, EventType.Repaint);
        }

        private void DrawBrushPreview(Event currentEvent, float brushSize)
        {
            Vector3 brushCenter;
            if (cursorSelectedPoint != null)
                brushCenter = cursorSelectedPoint.point;
            else
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                float dist = 5f;
                if(cursorSelectedBranch != null)
                   dist = Vector3.Distance(SceneView.currentDrawingSceneView.camera.transform.position, cursorSelectedBranch.branchPoints[0].point);
                brushCenter = ray.GetPoint(dist);
            }

            Color c = EditorConstants.ShaveBrushColor;
            c.a = 0.15f;
            Handles.color = c;

            float worldSize = HandleUtility.GetHandleSize(brushCenter) * (brushSize / 80f);
            Handles.SphereHandleCap(0, brushCenter, Quaternion.identity, worldSize, EventType.Repaint);
            
            c.a = 0.3f;
            Handles.color = c;
            Handles.DrawWireDisc(brushCenter, SceneView.currentDrawingSceneView.camera.transform.forward, worldSize * 0.5f);
        }
    }
}