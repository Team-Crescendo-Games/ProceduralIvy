using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModePrune : AMode
    {
        private bool pruning;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (toolPaintingAllowed)
            {
                SelectBranchPointSS(currentEvent.mousePosition, brushSize);

                if (cursorSelectedBranch != null)
                {
                    DrawBrushPreview(currentEvent, brushSize);
                    DrawTargetPreview();
                }
            }

            SceneView.RepaintAll();

            bool canUseTool = !forbiddenRect.Contains(currentEvent.mousePosition) || GUIUtility.hotControl == controlID;

            if (canUseTool && !currentEvent.alt && currentEvent.button == 0)
            {
                switch (currentEvent.type)
                {
                    case EventType.MouseDown:
                        // Only start if we have a valid target
                        if (IsValidTarget())
                        {
                            GUIUtility.hotControl = controlID;
                            pruning = true;
                            
                            ProceedToRemove();
                            RefreshMesh(true, false);
                            
                            currentEvent.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID && pruning)
                        {
                            // Continually remove points as we drag over them
                            if (IsValidTarget())
                            {
                                ProceedToRemove();
                                RefreshMesh(true, false);
                            }
                            currentEvent.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlID)
                        {
                            pruning = false;
                            GUIUtility.hotControl = 0;
                            currentEvent.Use();
                        }
                        break;
                }
            }

            // Safety release
            if (currentEvent.type == EventType.MouseLeaveWindow && pruning)
            {
                pruning = false;
                GUIUtility.hotControl = 0;
            }
        }

        private bool IsValidTarget()
        {
            // We cannot delete the first point (Root) or the last point (Tip)
            // ensuring the branch structure remains valid.
            return cursorSelectedBranch != null && 
                   cursorSelectedPoint != null && 
                   cursorSelectedPoint.index >= 1 && 
                   cursorSelectedPoint.index <= cursorSelectedBranch.branchPoints.Count - 2;
        }

        private void ProceedToRemove()
        {
            if (cursorSelectedBranch != null && cursorSelectedPoint != null)
            {
                cursorSelectedBranch.RemoveBranchPoint(cursorSelectedPoint.index);
                // Force a re-selection in the next frame since indices have shifted
                cursorSelectedPoint = null; 
            }
        }

        private void DrawBrushPreview(Event currentEvent, float brushSize)
        {
            Vector3 brushCenter = Vector3.zero;

            if (cursorSelectedPoint != null)
            {
                brushCenter = cursorSelectedPoint.point;
            }
            else
            {
                // Fallback depth calculation
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                float dist = Vector3.Distance(SceneView.currentDrawingSceneView.camera.transform.position, cursorSelectedBranch.branchPoints[0].point);
                brushCenter = ray.GetPoint(dist);
            }

            // Draw faint ghost sphere
            Color c = EditorConstants.PruneBrushColor;
            c.a = 0.15f;
            Handles.color = c;

            float worldSize = HandleUtility.GetHandleSize(brushCenter) * (brushSize / 80f);

            Handles.SphereHandleCap(0, brushCenter, Quaternion.identity, worldSize, EventType.Repaint);

            // Wireframe outline
            c.a = 0.3f;
            Handles.color = c;
            Handles.DrawWireDisc(brushCenter, SceneView.currentDrawingSceneView.camera.transform.forward, worldSize * 0.5f);
        }

        private void DrawTargetPreview()
        {
            if (IsValidTarget())
            {
                Handles.BeginGUI();
                
                Color c = EditorConstants.PruneBrushColor;
                c.a = 1f;

                Rect pointRect = new Rect(cursorSelectedPoint.GetScreenspacePosition() - Vector2.one * 3f, Vector2.one * 6f);
                EditorGUI.DrawRect(pointRect, c);
                
                // Draw an 'X' or smaller box inside to indicate deletion
                Color dangerColor = new Color(0.8f, 0, 0, 1);
                Rect innerRect = new Rect(cursorSelectedPoint.GetScreenspacePosition() - Vector2.one * 1f, Vector2.one * 2f);
                EditorGUI.DrawRect(innerRect, dangerColor);

                Handles.EndGUI();
            }
        }
    }
}