using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeMove : AMode
    {
        private bool moving;
        private Plane dragPlane;
        private Vector3 mouseOriginWS;
        private Vector3 mouseTargetWS;

        private readonly List<Vector3> originalPositions = new();
        private readonly List<int> affectedIndices = new();
        private readonly List<float> affectedInfluences = new();
        private readonly List<LeafPoint> affectedLeaves = new();

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize, AnimationCurve brushCurve)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (toolPaintingAllowed)
            {
                if (moving)
                {
                    DrawActiveSelection();
                }
                else
                {
                    SelectBranchPointSS(currentEvent.mousePosition, brushSize);
                    
                    if (cursorSelectedBranch != null)
                    {
                        DrawBrushPreview(currentEvent, brushSize);
                        DrawSelectionPreview(currentEvent, brushSize, brushCurve);
                    }
                }
            }
            
            SceneView.RepaintAll();

            bool canUseTool = !forbiddenRect.Contains(currentEvent.mousePosition) || GUIUtility.hotControl == controlID;

            if (canUseTool && !currentEvent.alt && currentEvent.button == 0)
            {
                switch (currentEvent.type)
                {
                    case EventType.MouseDown:
                        if (cursorSelectedBranch != null)
                        {
                            GUIUtility.hotControl = controlID;
                            StartMoving(currentEvent, brushSize, brushCurve);
                            currentEvent.Use(); 
                        }
                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID && moving)
                        {
                            PerformMove(currentEvent);
                            currentEvent.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlID)
                        {
                            StopMoving();
                            GUIUtility.hotControl = 0;
                            currentEvent.Use();
                        }
                        break;
                }
            }
            
            if (currentEvent.type == EventType.MouseLeaveWindow && moving)
            {
                 StopMoving();
                 GUIUtility.hotControl = 0;
            }
        }

        private void DrawBrushPreview(Event currentEvent, float brushSize)
        {
            // Determine the depth of the brush based on the selected branch
            // This makes the sphere sit "on" the ivy in 3D space
            Vector3 brushCenter = Vector3.zero;
            
            if (cursorSelectedPoint != null)
            {
                brushCenter = cursorSelectedPoint.point;
            }
            else
            {
                // Fallback: approximate depth using raycast against the branch plane or screen depth
                 Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                 float dist = Vector3.Distance(SceneView.currentDrawingSceneView.camera.transform.position, cursorSelectedBranch.branchPoints[0].point);
                 brushCenter = ray.GetPoint(dist);
            }

            // Draw faint sphere
            Color c = EditorConstants.MoveBrushColor;
            c.a = 0.15f; // Very faint, ghost-like
            Handles.color = c;
            
            // Note: brushSize is in screen pixels usually, but SphereHandleCap takes world size.
            // We need to calculate a rough world size for the sphere based on camera distance.
            float worldSize = HandleUtility.GetHandleSize(brushCenter) * (brushSize / 80f); // Approximate conversion

            Handles.SphereHandleCap(0, brushCenter, Quaternion.identity, worldSize, EventType.Repaint);
            
            // Optional: Draw a wireframe for better definition
            c.a = 0.3f;
            Handles.color = c;
            Handles.DrawWireDisc(brushCenter, SceneView.currentDrawingSceneView.camera.transform.forward, worldSize * 0.5f);
        }

        private void DrawSelectionPreview(Event currentEvent, float brushSize, AnimationCurve brushCurve)
        {
            Handles.BeginGUI();
            for (var p = 0; p < cursorSelectedBranch.branchPoints.Count; p++)
            {
                var bp = cursorSelectedBranch.branchPoints[p];
                float dist = Vector2.Distance(bp.GetScreenspacePosition(), currentEvent.mousePosition);
                
                if (dist < brushSize / 2f)
                {
                    float normalizedDist = dist / (brushSize / 2f);
                    float influence = brushCurve.Evaluate(1f - normalizedDist);
                    
                    Color c = EditorConstants.MoveBrushColor;
                    c.a = influence; 

                    Rect pointRect = new Rect(bp.GetScreenspacePosition() - Vector2.one * 2f, Vector2.one * 4f);
                    EditorGUI.DrawRect(pointRect, c);
                }
            }
            Handles.EndGUI();
        }

        private void DrawActiveSelection()
        {
            if (cursorSelectedBranch == null) return;

            Handles.BeginGUI();
            for (int i = 0; i < affectedIndices.Count; i++)
            {
                int index = affectedIndices[i];
                if (index < cursorSelectedBranch.branchPoints.Count)
                {
                    var bp = cursorSelectedBranch.branchPoints[index];
                    float influence = affectedInfluences[i];

                    Color c = EditorConstants.MoveBrushColor;
                    c.a = influence;

                    Rect pointRect = new Rect(bp.GetScreenspacePosition() - Vector2.one * 2f, Vector2.one * 4f);
                    EditorGUI.DrawRect(pointRect, c);
                }
            }
            Handles.EndGUI();
        }

        private void StartMoving(Event currentEvent, float brushSize, AnimationCurve brushCurve)
        {
            moving = true;
            
            originalPositions.Clear();
            affectedIndices.Clear();
            affectedInfluences.Clear();
            affectedLeaves.Clear();

            Vector3 centerOfSelection = Vector3.zero;

            for (var p = 0; p < cursorSelectedBranch.branchPoints.Count; p++)
            {
                var bp = cursorSelectedBranch.branchPoints[p];
                float dist = Vector2.Distance(bp.GetScreenspacePosition(), currentEvent.mousePosition);
                
                if (dist < brushSize / 2f)
                {
                    affectedIndices.Add(p);
                    originalPositions.Add(bp.point);
                    
                    float normalizedDist = dist / (brushSize / 2f);
                    affectedInfluences.Add(brushCurve.Evaluate(1f - normalizedDist));
                    
                    centerOfSelection += bp.point;
                }
            }

            if (affectedIndices.Count == 0)
            {
                moving = false;
                return;
            }

            centerOfSelection /= affectedIndices.Count;

            Camera cam = SceneView.currentDrawingSceneView.camera;
            dragPlane = new Plane(-cam.transform.forward, centerOfSelection);

            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                mouseOriginWS = ray.GetPoint(enter);
            }
            else
            {
                mouseOriginWS = centerOfSelection;
            }
        }

        private void PerformMove(Event currentEvent)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                mouseTargetWS = ray.GetPoint(enter);
            }
            
            Vector3 delta = mouseTargetWS - mouseOriginWS;

            affectedLeaves.Clear();

            for (var i = 0; i < affectedIndices.Count; i++)
            {
                int index = affectedIndices[i];
                var bp = cursorSelectedBranch.branchPoints[index];
                
                cursorSelectedBranch.GetLeavesInSegment(bp, affectedLeaves);
                Vector3 newPos = originalPositions[i] + (delta * affectedInfluences[i]);
                bp.Move(newPos);
            }

            cursorSelectedBranch.RepositionLeaves(affectedLeaves, true);
            RefreshMesh(true, true);
        }

        private void StopMoving()
        {
            moving = false;
            originalPositions.Clear();
            affectedIndices.Clear();
            affectedInfluences.Clear();
        }
    }
}