using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeDelete : AMode
    {
        private List<BranchContainer> branchesToRemove = new List<BranchContainer>();

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (toolPaintingAllowed)
            {
                SelectBranchPointSS(currentEvent.mousePosition, brushSize);

                if (cursorSelectedBranch != null)
                {
                    // root cannot be deleted
                    bool isDeletable = cursorSelectedBranch.branchNumber > 0;

                    DrawBrushPreview(currentEvent, brushSize, isDeletable);

                    if (!isDeletable)
                    {
                        DrawWarningLabel(currentEvent.mousePosition);
                    }
                    else
                    {
                        // Calculate deletion targets
                        branchesToRemove.Clear();
                        branchesToRemove.Add(cursorSelectedBranch);
                        CheckOrphanBranches(cursorSelectedBranch.branchPoints);

                        DrawDeletePreview();

                        bool canUseTool = !forbiddenRect.Contains(currentEvent.mousePosition) || GUIUtility.hotControl == controlID;

                        if (canUseTool && !currentEvent.alt && currentEvent.button == 0)
                        {
                            if (currentEvent.type == EventType.MouseDown)
                            {
                                GUIUtility.hotControl = controlID;
                                PerformDelete();
                                RefreshMesh(false, false);
                                currentEvent.Use();
                            }
                            else if (currentEvent.type == EventType.MouseUp)
                            {
                                GUIUtility.hotControl = 0;
                                currentEvent.Use();
                            }
                        }
                    }
                }
            }
            
            SceneView.RepaintAll();
        }

        private void PerformDelete()
        {
            for (var i = 0; i < branchesToRemove.Count; i++)
                infoPool.ivyContainer.RemoveBranch(branchesToRemove[i]);
            branchesToRemove.Clear();
        }

        private void CheckOrphanBranches(List<BranchPoint> pointsToCheck)
        {
            for (var i = 0; i < pointsToCheck.Count; i++)
            {
                if (pointsToCheck[i].newBranch && pointsToCheck[i].newBranchNumber != cursorSelectedBranch.branchNumber)
                {
                    var orphanBranch = infoPool.ivyContainer.GetBranchContainerByBranchNumber(pointsToCheck[i].newBranchNumber);
                    if (orphanBranch != null && !branchesToRemove.Contains(orphanBranch))
                    {
                        branchesToRemove.Add(orphanBranch);
                        CheckOrphanBranches(orphanBranch.branchPoints);
                    }
                }
            }
        }

        private void DrawWarningLabel(Vector2 mousePosition)
        {
            Handles.BeginGUI();
            
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = new Color(1f, 0.4f, 0.4f); // Light Red text
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 12;
            
            // Draw slightly offset from cursor
            Rect labelRect = new Rect(mousePosition.x + 15, mousePosition.y - 15, 200, 20);
            GUI.Label(labelRect, "Cannot Delete Root Branch", style);
            
            Handles.EndGUI();
        }

        private void DrawDeletePreview()
        {
            // Draw main branch (Solid)
            if (branchesToRemove.Count > 0)
            {
                Handles.color = EditorConstants.DeleteBrushColor;
                Vector3[] mainPath = GetPathFromBranch(branchesToRemove[0]);
                Handles.DrawPolyLine(mainPath);
                
                Handles.CubeHandleCap(0, mainPath[0], Quaternion.identity, 0.05f, EventType.Repaint);
                Handles.CubeHandleCap(0, mainPath[^1], Quaternion.identity, 0.05f, EventType.Repaint);
            }

            // Draw orphan branches (Dotted)
            if (branchesToRemove.Count > 1)
            {
                Handles.color = EditorConstants.DeleteBrushColor;
                for (int i = 1; i < branchesToRemove.Count; i++)
                {
                    var branch = branchesToRemove[i];
                    Vector3[] orphanPath = GetPathFromBranch(branch);
                    for(int k=0; k < orphanPath.Length -1; k++)
                    {
                        Handles.DrawDottedLine(orphanPath[k], orphanPath[k+1], 4f);
                    }
                }
            }

            // Highlight origin
            if (cursorSelectedBranch.originPointOfThisBranch != null)
            {
                 Handles.color = Color.blue;
                 Handles.SphereHandleCap(0, cursorSelectedBranch.originPointOfThisBranch.point, Quaternion.identity, 0.08f, EventType.Repaint);
            }
        }

        private Vector3[] GetPathFromBranch(BranchContainer branch)
        {
            Vector3[] path = new Vector3[branch.branchPoints.Count];
            for (int i = 0; i < branch.branchPoints.Count; i++)
                path[i] = branch.branchPoints[i].point;
            return path;
        }

        private void DrawBrushPreview(Event currentEvent, float brushSize, bool isDeletable)
        {
            Vector3 brushCenter;
            if (cursorSelectedPoint != null)
            {
                brushCenter = cursorSelectedPoint.point;
            }
            else
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                float dist = 5f;
                if(cursorSelectedBranch != null)
                   dist = Vector3.Distance(SceneView.currentDrawingSceneView.camera.transform.position, cursorSelectedBranch.branchPoints[0].point);
                brushCenter = ray.GetPoint(dist);
            }

            Color c = isDeletable ? EditorConstants.DeleteBrushColor : EditorConstants.CannotDeleteBrushColor;
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