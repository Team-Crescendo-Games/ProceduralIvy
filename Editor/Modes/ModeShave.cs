using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
    public class ModeShave : AbstractMode
    {
        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            //Empezamos la gui para pintar los puntos en screen space

            //Con este método guardamos en un array predeclarado todos los puntos de la enredadera en screen space
            GetBranchesPointsSS();
            //Si no estamos moviendo ningún punto, buscamos el overbranch, overpoint y pintamos la textura del brush en la pantalla
            //if (!shaving)
            //{
            //	SelectBranchPointSS(currentEvent.mousePosition, brushSize);
            //}

            SelectBranchPointSS(currentEvent.mousePosition, brushSize);

            if (toolPaintingAllowed)
            {
                DrawBrush(currentEvent, brushSize);

                Handles.BeginGUI();
                if (overBranch != null)
                {
                    SelectLeavesSS(currentEvent.mousePosition, brushSize);

                    //Al levantar click, si estábamos moviendo y no orbitando la cámara guardamos el estado de las enredaderas y ponemos la flag moving en falso
                    /*if (!currentEvent.alt && currentEvent.type == EventType.MouseUp && shaving)
                    {
                        StopShaving();
                    }*/

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
            for (int i = 0; i < overLeaves.Count; i++)
            {
                EditorGUI.DrawRect(new Rect(overLeaves[i].pointSS - Vector2.one * 2f, Vector2.one * 4f), Color.red);
            }
        }
    }
}