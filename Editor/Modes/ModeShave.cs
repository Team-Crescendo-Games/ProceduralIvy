using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeShave : AMode
    {
        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            GetBranchesPointsSS();

            SelectBranchPointSS(currentEvent.mousePosition, brushSize);

            if (toolPaintingAllowed)
            {
                DrawBrush(currentEvent, brushSize);

                Handles.BeginGUI();
                if (overBranch != null)
                {
                    SelectLeavesSS(currentEvent.mousePosition, brushSize);

                    if (overLeaves.Count > 0)
                    {
                        DrawOverLeaves();

                        //después, si hacemos clic con el ratón....
                        if (currentEvent.type == EventType.MouseDown && overBranch != null)
                        {
                            SaveIvy();

                            overBranch.RemoveLeaves(overLeaves);
                            RefreshMesh(true, true);
                        }

                        //al arrastrar calculamos el delta actualizando el worldspace del target y aplicamos el delta transformado en relación a la distancia al overpoint a los vértices guardados como afectados
                        if (currentEvent.type == EventType.MouseDrag)
                        {
                            overBranch.RemoveLeaves(overLeaves);
                            RefreshMesh(true, true);
                        }
                    }
                }

                SceneView.RepaintAll();
            }

            Handles.EndGUI();
        }

        private void DrawOverLeaves()
        {
            for (var i = 0; i < overLeaves.Count; i++)
                EditorGUI.DrawRect(new Rect(overLeaves[i].pointSS - Vector2.one * 2f, Vector2.one * 4f), Color.red);
        }
    }
}