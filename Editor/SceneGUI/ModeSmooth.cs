using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeSmooth : AMode
    {
        private readonly List<Vector3> overPoints = new();
        private readonly List<int> overPointsIndex = new();
        private readonly List<float> overPointsInfluences = new();
        private bool smoothing;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize, AnimationCurve brushCurve, float smoothIntensity)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (toolPaintingAllowed)
            {
                if (smoothing)
                {
                    // While dragging, keep showing the points we are smoothing
                    DrawActiveSelection();
                }
                else
                {
                    // While hovering, find candidates and preview
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
                            StartSmoothing(currentEvent, brushSize, brushCurve);
                            currentEvent.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID && smoothing)
                        {
                            PerformSmooth(smoothIntensity);
                            currentEvent.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlID)
                        {
                            StopSmoothing();
                            GUIUtility.hotControl = 0;
                            currentEvent.Use();
                        }
                        break;
                }
            }

            // Safety release
            if (currentEvent.type == EventType.MouseLeaveWindow && smoothing)
            {
                StopSmoothing();
                GUIUtility.hotControl = 0;
            }
        }

        private void DrawBrushPreview(Event currentEvent, float brushSize)
        {
            Vector3 brushCenter;
            if (cursorSelectedPoint != null)
                brushCenter = cursorSelectedPoint.point;
            else
            {
                // Fallback depth calculation
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                float dist = Vector3.Distance(SceneView.currentDrawingSceneView.camera.transform.position, cursorSelectedBranch.branchPoints[0].point);
                brushCenter = ray.GetPoint(dist);
            }

            // Draw faint ghost sphere
            Color c = EditorConstants.SmoothBrushColor;
            c.a = 0.15f; 
            Handles.color = c;
            
            float worldSize = HandleUtility.GetHandleSize(brushCenter) * (brushSize / 80f); 

            Handles.SphereHandleCap(0, brushCenter, Quaternion.identity, worldSize, EventType.Repaint);
            
            // Wireframe outline
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
                    
                    Color c = EditorConstants.SmoothBrushColor;
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
            for (int i = 0; i < overPointsIndex.Count; i++)
            {
                int index = overPointsIndex[i];
                if (index < cursorSelectedBranch.branchPoints.Count)
                {
                    var bp = cursorSelectedBranch.branchPoints[index];
                    float influence = overPointsInfluences[i];

                    Color c = EditorConstants.SmoothBrushColor;
                    c.a = influence;

                    Rect pointRect = new Rect(bp.GetScreenspacePosition() - Vector2.one * 2f, Vector2.one * 4f);
                    EditorGUI.DrawRect(pointRect, c);
                }
            }
            Handles.EndGUI();
        }

        private void StartSmoothing(Event currentEvent, float brushSize, AnimationCurve brushCurve)
        {
            smoothing = true;
            overPoints.Clear();
            overPointsIndex.Clear();
            overPointsInfluences.Clear();

            // Snapshot the points under the brush
            for (var p = 0; p < cursorSelectedBranch.branchPoints.Count; p++)
            {
                var bp = cursorSelectedBranch.branchPoints[p];
                float dist = Vector2.Distance(bp.GetScreenspacePosition(), currentEvent.mousePosition);

                if (dist < brushSize / 2f)
                {
                    overPointsIndex.Add(p);
                    overPoints.Add(bp.point); // Store original position (though Smooth logic often uses dynamic neighbors)
                    
                    float normalizedDist = dist / (brushSize / 2f);
                    overPointsInfluences.Add(brushCurve.Evaluate(1f - normalizedDist));
                }
            }
        }

        private void PerformSmooth(float smoothIntensity)
        {
            if (cursorSelectedBranch == null) return;

            for (var i = 0; i < overPointsIndex.Count; i++)
            {
                int idx = overPointsIndex[i];

                // Skip first and last points to pin the branch ends
                if (idx != 0 && idx != cursorSelectedBranch.branchPoints.Count - 1)
                {
                    Vector3 currentPos = cursorSelectedBranch.branchPoints[idx].point;
                    Vector3 prevPos = cursorSelectedBranch.branchPoints[idx - 1].point;
                    Vector3 nextPos = cursorSelectedBranch.branchPoints[idx + 1].point;

                    // Calculate smoothed position (average of neighbors)
                    Vector3 targetPos = Vector3.Lerp(prevPos, nextPos, 0.5f);
                    
                    // Apply smoothing based on brush influence and tool intensity
                    // Note: We use the current position as the base, not the original snapshot, 
                    // allowing for iterative smoothing while dragging.
                    Vector3 newPoint = Vector3.Lerp(currentPos, targetPos, smoothIntensity * overPointsInfluences[i]);

                    cursorSelectedBranch.branchPoints[idx].point = newPoint;
                }
            }

            cursorSelectedBranch.RepositionLeaves(true);
            RefreshMesh(true, true);
        }

        private void StopSmoothing()
        {
            smoothing = false;
            overPoints.Clear();
            overPointsIndex.Clear();
            overPointsInfluences.Clear();
        }
    }
}