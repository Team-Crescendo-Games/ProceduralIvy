using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeCut : AMode
    {
        private List<BranchContainer> branchesToRemove;
        private List<BranchPoint> pointsToRemove;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            SelectBranchPointSS(currentEvent.mousePosition, brushSize);

            if (cursorSelectedBranch != null && cursorSelectedPoint != null)
            {
                if (toolPaintingAllowed)
                {
                    pointsToRemove = new List<BranchPoint>();
                    branchesToRemove = new List<BranchContainer>();

                    var initIndex = cursorSelectedPoint.index;
                    initIndex = Mathf.Clamp(initIndex, 2, int.MaxValue);

                    var endIndex = cursorSelectedBranch.branchPoints.Count - initIndex;

                    // Get the range that will be cut
                    if (initIndex < cursorSelectedBranch.branchPoints.Count)
                    {
                        pointsToRemove = cursorSelectedBranch.branchPoints.GetRange(initIndex, endIndex);
                        
                        // Check for child branches that would be orphaned by this cut
                        CheckOrphanBranches(pointsToRemove);
                    }

                    DrawBrushPreview(currentEvent, brushSize);
                    
                    // draw main
                    if (pointsToRemove.Count > 0)
                    {
                        DrawConnections(pointsToRemove, EditorConstants.CutBrushColor);
                        DrawPoints(pointsToRemove, EditorConstants.CutBrushColor);
                    }
                    
                    // draw orphans
                    foreach(var branch in branchesToRemove)
                    {
                        DrawConnections(branch.branchPoints, EditorConstants.DeleteBrushColor);
                        DrawPoints(branch.branchPoints, EditorConstants.DeleteBrushColor);
                    }
                }

                if (!forbiddenRect.Contains(currentEvent.mousePosition) && !currentEvent.alt && currentEvent.button == 0)
                {
                    if (currentEvent.type == EventType.MouseDown)
                    {
                        GUIUtility.hotControl = controlID;

                        ProceedToRemove();
                        RefreshMesh(true, true);
                        
                        currentEvent.Use();
                    }
                    else if (currentEvent.type == EventType.MouseUp)
                    {
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                }
            }
            
            SceneView.RepaintAll();
        }

        private void ProceedToRemove()
        {
            if (pointsToRemove != null && pointsToRemove.Count > 0)
                cursorSelectedBranch.RemoveRange(pointsToRemove[0].index, pointsToRemove.Count);

            for (var i = 0; i < branchesToRemove.Count; i++) 
                infoPool.ivyContainer.RemoveBranch(branchesToRemove[i]);
        }

        private void CheckOrphanBranches(List<BranchPoint> pointsToCheck)
        {
            for (var i = 0; i < pointsToCheck.Count; i++)
            {
                if (pointsToCheck[i].newBranch && pointsToCheck[i].newBranchNumber != cursorSelectedBranch.branchNumber)
                {
                    var orphanBranch =
                        infoPool.ivyContainer.GetBranchContainerByBranchNumber(pointsToCheck[i].newBranchNumber);
                    if (orphanBranch != null)
                    {
                        branchesToRemove.Add(orphanBranch);
                        CheckOrphanBranches(orphanBranch.branchPoints);
                    }
                }
            }
        }

        private void DrawConnections(List<BranchPoint> points, Color color)
        {
            if (points.Count < 2) return;

            Handles.color = color;
            Vector3[] path = new Vector3[points.Count];
            for(int i = 0; i < points.Count; i++)
                path[i] = points[i].point;
            Handles.DrawPolyLine(path);
        }

        private void DrawPoints(List<BranchPoint> pointsToDraw, Color color)
        {
            Color c = color;
            c.a = 0.6f; 
            
            Handles.BeginGUI();
            for (var i = 0; i < pointsToDraw.Count; i++)
                EditorGUI.DrawRect(new Rect(pointsToDraw[i].GetScreenspacePosition() - Vector2.one * 2f, Vector2.one * 4f), c);
            Handles.EndGUI();
        }

        private void DrawBrushPreview(Event currentEvent, float brushSize)
        {
            Vector3 brushCenter;
            if (cursorSelectedPoint != null)
            {
                brushCenter = cursorSelectedPoint.point;
            }
            else
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                float dist = Vector3.Distance(SceneView.currentDrawingSceneView.camera.transform.position, cursorSelectedBranch.branchPoints[0].point);
                brushCenter = ray.GetPoint(dist);
            }

            Color c = EditorConstants.CutBrushColor;
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