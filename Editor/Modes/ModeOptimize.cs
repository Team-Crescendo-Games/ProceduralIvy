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
            Handles.BeginGUI();
            GetBranchesPointsSS();
            SelectBranchPointSS(currentEvent.mousePosition, brushSize);

            if (cursorSelectedBranch != null && cursorSelectedPoint != null)
                if (cursorSelectedPoint.index >= 1 && cursorSelectedPoint.index <= cursorSelectedBranch.branchPoints.Count - 2 &&
                    !cursorSelectedPoint.newBranch)
                {
                    if (toolPaintingAllowed) DrawPoint(cursorSelectedPoint, Color.red);

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
            cursorSelectedBranch.RemoveBranchPoint(cursorSelectedPoint.index);
        }

        private void DrawPoint(BranchPoint pointToDraw, Color color)
        {
            EditorGUI.DrawRect(new Rect(pointToDraw.pointSS - Vector2.one * 2f, Vector2.one * 4f), color);
        }
    }
}