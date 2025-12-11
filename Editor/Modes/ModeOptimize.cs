using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeOptimize : AMode
    {
        private bool optimizing;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            //Empezamos la gui para pintar los puntos en screen space
            Handles.BeginGUI();
            //Con este método guardamos en un array predeclarado todos los puntos de la enredadera en screen space
            GetBranchesPointsSS();
            //Y con este seleccionamos la rama y el punto mas cercanos al ratón en screen space
            SelectBranchPointSS(currentEvent.mousePosition, brushSize);

            if (overBranch != null && overPoint != null)
                if (overPoint.index >= 1 && overPoint.index <= overBranch.branchPoints.Count - 2 &&
                    !overPoint.newBranch)
                {
                    if (toolPaintingAllowed) DrawPoint(overPoint, Color.red);


                    //después, si hacemos clic con el ratón removemos el punto seleccionado de la rama
                    if (currentEvent.type == EventType.MouseDown && !currentEvent.alt && currentEvent.button == 0)
                    {
                        SaveIvy();
                        optimizing = true;

                        ProceedToRemove();
                        RefreshMesh(true, false);
                    }

                    if (currentEvent.type == EventType.MouseDrag && optimizing && currentEvent.button == 0)
                    {
                        ProceedToRemove();
                        RefreshMesh(true, false);
                    }
                }

            Handles.EndGUI();

            SceneView.RepaintAll();
        }

        private void ProceedToRemove()
        {
            overBranch.RemoveBranchPoint(overPoint.index);
        }

        private void DrawPoint(BranchPoint pointToDraw, Color color)
        {
            EditorGUI.DrawRect(new Rect(pointToDraw.pointSS - Vector2.one * 2f, Vector2.one * 4f), color);
        }

        private void DrawPoints(List<BranchPoint> pointsToDraw, Color color)
        {
            for (var i = 0; i < pointsToDraw.Count; i++) DrawPoint(pointsToDraw[i], color);
        }

        private void DrawLeavesInSegment(BranchPoint point)
        {
            var leaves = overBranch.GetLeavesInSegment(point);

            for (var i = 0; i < leaves.Count; i++)
            {
                leaves[i].CalculatePointSS();
                EditorGUI.DrawRect(new Rect(leaves[i].pointSS - Vector2.one * 2f, Vector2.one * 4f), Color.green);
            }
        }

        private void CheckOrphanBranches(BranchPoint pointToCheck)
        {
            var pointsToCheck = new List<BranchPoint>();
            pointsToCheck.Add(pointToCheck);
        }
    }
}