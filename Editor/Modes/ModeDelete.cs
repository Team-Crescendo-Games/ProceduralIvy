using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeDelete : AMode
    {
        private List<BranchContainer> branchesToRemove;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            //Empezamos la gui para pintar los puntos en screen space
            Handles.BeginGUI();
            //Con este método guardamos en un array predeclarado todos los puntos de la enredadera en screen space
            GetBranchesPointsSS();
            //Y con este seleccionamos la rama y el punto mas cercanos al ratón en screen space

            if (toolPaintingAllowed)
            {
                SelectBranchPointSS(currentEvent.mousePosition, brushSize);

                if (cursorSelectedBranch != null && cursorSelectedBranch.branchNumber > 0)
                {
                    branchesToRemove = new List<BranchContainer>();
                    branchesToRemove.Add(cursorSelectedBranch);
                    CheckOrphanBranches(cursorSelectedBranch.branchPoints);
                    DrawPoints(cursorSelectedBranch.branchPoints, Color.red);


                    DrawOriginBranch();

                    if (currentEvent.type == EventType.MouseDown && !currentEvent.alt && currentEvent.button == 0)
                    {
                        SaveIvy();

                        for (var i = 0; i < branchesToRemove.Count; i++)
                            infoPool.ivyContainer.RemoveBranch(branchesToRemove[i]);

                        RefreshMesh(false, false);
                    }
                }

                SceneView.RepaintAll();
            }

            Handles.EndGUI();
        }

        private void CheckOrphanBranches(List<BranchPoint> pointsToCheck)
        {
            for (var i = 0; i < pointsToCheck.Count; i++)
                if (pointsToCheck[i].newBranch && pointsToCheck[i].newBranchNumber != cursorSelectedBranch.branchNumber)
                {
                    var orphanBranch =
                        infoPool.ivyContainer.GetBranchContainerByBranchNumber(pointsToCheck[i].newBranchNumber);
                    if (orphanBranch != null)
                    {
                        branchesToRemove.Add(orphanBranch);
                        DrawPoints(orphanBranch.branchPoints, Color.blue);
                        CheckOrphanBranches(orphanBranch.branchPoints);
                    }
                }
        }

        private void DrawPoints(List<BranchPoint> pointsToDraw, Color color)
        {
            for (var i = 0; i < pointsToDraw.Count; i++)
                EditorGUI.DrawRect(new Rect(pointsToDraw[i].pointSS - Vector2.one * 2f, Vector2.one * 4f), color);
        }

        private void DrawOriginBranch()
        {
            if (cursorSelectedBranch.originPointOfThisBranch != null)
                EditorGUI.DrawRect(
                    new Rect(cursorSelectedBranch.originPointOfThisBranch.pointSS - Vector2.one * 2f, Vector2.one * 4f),
                    Color.blue);
        }
    }
}