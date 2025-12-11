using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeMove : AMode
    {
        private readonly List<LeafPoint> leavesForDebug = new();
        private readonly List<LeafPoint> leavesInInfluences = new();

        private Vector3 mouseOriginWS = Vector3.zero;
        private Vector3 mouseTargetWS = Vector3.zero;
        private bool moving;

        private readonly List<Vector3> overPoints = new();
        private readonly List<int> overPointsIndex = new();
        private readonly List<float> overPointsInfluences = new();
        private readonly List<Vector3> overPointsLeaves = new();
        private readonly List<int> overPointsLeavesIndex = new();
        private readonly List<float> overPointsLeavesInfluences = new();

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize, AnimationCurve brushCurve)
        {
            if (currentEvent.type == EventType.MouseLeaveWindow ||
                currentEvent.type == EventType.MouseEnterWindow) StopMoving();


            RefreshBrushWS(currentEvent);
            if (!moving && toolPaintingAllowed) DrawBrush(currentEvent, brushSize);

            if (toolPaintingAllowed)
            {
                //Empezamos la gui para pintar los puntos en screen space
                Handles.BeginGUI();
                //Con este método guardamos en un array predeclarado todos los puntos de la enredadera en screen space
                GetBranchesPointsSS();
                //Si no estamos moviendo ningún punto, buscamos el overbranch, overpoint y pintamos la textura del brush en la pantalla
                if (!moving) SelectBranchPointSS(currentEvent.mousePosition, brushSize);

                //Si tenemos algún overbranch y no estamos moviendo ningún punto limpiamos las listas de los puntos que encontramos en la iteración anterior al alcance del brush, y llenamos 
                //estas mismas listas con los puntos para este nuevo fotograma, además pintamos los puntos al alcance del brush
                if (overBranch != null && !moving)
                {
                    overPoints.Clear();
                    overPointsIndex.Clear();
                    overPointsLeaves.Clear();
                    overPointsLeavesIndex.Clear();
                    overPointsInfluences.Clear();
                    overPointsLeavesInfluences.Clear();
                    leavesInInfluences.Clear();

                    for (var p = 0; p < overBranch.branchPoints.Count; p++)
                    {
                        var currenBranchPoint = overBranch.branchPoints[p];
                        if (Vector2.Distance(currentEvent.mousePosition, currenBranchPoint.pointSS) < brushSize / 2f)
                        {
                            overPointsIndex.Add(p);
                            overPoints.Add(overBranch.branchPoints[p].point);
                            overPointsInfluences.Add(brushCurve.Evaluate(1f -
                                                                         Vector2.Distance(currenBranchPoint.pointSS,
                                                                             currentEvent.mousePosition) / brushSize *
                                                                         2f));


                            //if (currenBranchPoint.HasLeave())
                            //{
                            //	overPointsLeavesIndex.Add(p);
                            //	overPointsLeaves.Add(currenBranchPoint.point);
                            //	overPointsLeavesInfluences.Add(brushCurve.Evaluate(1f - Vector2.Distance(currenBranchPoint.pointSS, currentEvent.mousePosition) / brushSize * 2f));
                            //}


                            EditorGUI.DrawRect(new Rect(currenBranchPoint.pointSS - Vector2.one * 2f, Vector2.one * 4f),
                                new Color(1f, 1f, 1f, overPointsInfluences[overPointsInfluences.Count - 1]));
                        }
                    }
                    //for (int l = 0; l < branchesLeavesPointsSS[overBranch].Length; l++)
                    //{
                    //	if (Vector2.Distance(current.mousePosition, branchesLeavesPointsSS[overBranch][l]) < brushSize / 2f)
                    //	{
                    //		overPointsLeavesIndex.Add(l);
                    //		overPointsLeaves.Add(infoPool.ivyContainer.branches[overBranch].leavesPoints[l]);
                    //		overPointsLeavesInfluences.Add(brushCurve.Evaluate(1f - Vector2.Distance(branchesLeavesPointsSS[overBranch][l], current.mousePosition) / brushSize * 2f));
                    //	}
                    //}
                }

                Handles.EndGUI();
            }


            //Al levantar click, si estábamos moviendo y no orbitando la cámara guardamos el estado de las enredaderas y ponemos la flag moving en falso
            if (!currentEvent.alt && currentEvent.type == EventType.MouseUp && moving) StopMoving();

            //si no estamos en el forbiddenrect, y no estamos pulsando alt y es el ratón pcipal del ratón
            if (!forbiddenRect.Contains(currentEvent.mousePosition) && !currentEvent.alt && currentEvent.button == 0)
            {
                //después, si hacemos clic con el ratón....
                if (currentEvent.type == EventType.MouseDown && overBranch != null)
                {
                    //Refrescamos 
                    RefreshBrushDistance();
                    RefreshBrushWS(currentEvent);
                    //guardamos el worldspace del ratón y lo igualamos al target para que el primer delta sea 0, y ponemos moving a true
                    mouseOriginWS = brushWS;
                    mouseTargetWS = mouseOriginWS;
                    moving = true;

                    SaveIvy();
                }

                //al arrastrar calculamos el delta actualizando el worldspace del target y aplicamos el delta transformado en relación a la distancia al overpoint a los vértices guardados como afectados
                if (currentEvent.type == EventType.MouseDrag && overBranch != null)
                {
                    mouseTargetWS = brushWS;
                    var delta = mouseTargetWS - mouseOriginWS;

                    leavesInInfluences.Clear();
                    for (var i = 0; i < overPointsIndex.Count; i++)
                    {
                        overBranch.GetLeavesInSegment(overBranch.branchPoints[overPointsIndex[i]], leavesInInfluences);
                        overBranch.branchPoints[overPointsIndex[i]]
                            .Move(overPoints[i] + delta * overPointsInfluences[i]);
                    }


                    overBranch.RepositionLeaves02(leavesInInfluences, true);


                    RefreshMesh(true, true);
                }
            }


            SceneView.RepaintAll();
            DrawVectors();
        }

        private void DrawVectors()
        {
            if (overBranch != null)
            {
                leavesForDebug.Clear();
                for (var i = 0; i < overPointsIndex.Count; i++)
                    overBranch.GetLeavesInSegment(overBranch.branchPoints[overPointsIndex[i]], leavesForDebug);
            }
        }

        private void StopMoving()
        {
            moving = false;
            overPoints.Clear();
            overPointsInfluences.Clear();
            overPointsLeavesInfluences.Clear();
        }
    }
}