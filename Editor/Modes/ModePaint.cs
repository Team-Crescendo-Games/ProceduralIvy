using UnityEditor;
using UnityEngine;
using Dynamite3D.RealIvy;

namespace Dynamite3D.RealIvy
{
    public class ModePaint : AbstractMode
    {
        private bool painting;

        private Vector3 lastMousePointWS = Vector3.zero;
        private Vector3 mousePointWS = Vector3.zero;
        private Vector3 dirDrag = Vector3.zero;



        public RealIvyWindow realIvyProWindow;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            GetBranchesPointsSS();
            if (!painting)
            {
                SelectBranchPointSS(currentEvent.mousePosition, brushSize);
                if (overBranch != null && this.toolPaintingAllowed)
                {
                    DrawBrush(currentEvent, brushSize);
                    Handles.BeginGUI();

                    Color pointColor = Color.black;
                    if (overPoint == null)
                    {
                        mousePointWS = GetMousePointOverBranch(currentEvent, brushSize);
                        pointColor = Color.green;
                    }
                    else
                    {
                        mousePointWS = overPoint.point;
                        pointColor = Color.yellow;

                        if (overPoint.index == overBranch.branchPoints.Count - 1)
                        {
                            pointColor = Color.cyan;
                        }
                    }

                    EditorGUI.DrawRect(new Rect(HandleUtility.WorldToGUIPoint(mousePointWS) - Vector2.one * 2f, Vector2.one * 4f), pointColor);
                    //if (overPoint.index == overBranch.branchPoints.Count - 1)
                    //{
                    //	EditorGUI.DrawRect(new Rect(HandleUtility.WorldToGUIPoint(overPoint.point) - Vector2.one * 2f, Vector2.one * 4f), Color.yellow);
                    //}
                    //else
                    //{
                    //	EditorGUI.DrawRect(new Rect(HandleUtility.WorldToGUIPoint(overPoint.point) - Vector2.one * 2f, Vector2.one * 4f), Color.cyan);
                    //}
                    Handles.EndGUI();
                }

                SceneView.RepaintAll();
            }

            //si no estamos en el forbiddenrect, el raycast es positivo y no estamos pulsando alt y es el ratón pcipal del ratón
            if (!forbiddenRect.Contains(currentEvent.mousePosition) && !currentEvent.alt && currentEvent.button == 0)
            {
                //después, si hacemos clic con el ratón....
                if (currentEvent.type == EventType.MouseDown)
                {
                    dirDrag = (mousePointWS - lastMousePointWS).normalized;
                    lastMousePointWS = mousePointWS;

                    if (overBranch != null)
                    {
                        if (overPoint == null)
                        {
                            BranchPoint nearestPoint = overBranch.GetNearestPointWSFrom(mousePointWS);

                            int newIndex = overSegment[1].index;

                            BranchPoint nextPoint = overBranch.branchPoints[overSegment[1].index + 1];
                            float newLength = Mathf.Lerp(overSegment[0].length, nextPoint.length, normalizedSegmentOffset);
                            overPoint = overBranch.InsertBranchPoint(mousePointWS, nearestPoint.grabVector, newIndex);

                            RefreshMesh(true, true);
                        }

                        RayCastSceneView(brushDistance + infoPool.ivyParameters.maxDistanceToSurface * 1.5f);
                    }
                    else
                    {
                        RayCastSceneView(2000f);
                    }

                    if (!rayCast)
                    {
                        RefreshBrushDistance();
                        RefreshBrushWS(currentEvent);
                        mousePoint = brushWS;
                        mouseNormal = -SceneView.currentDrawingSceneView.camera.transform.forward;
                    }

                    if (overBranch == null)
                    {
                        this.infoPool = realIvyProWindow.CreateNewIvy();
                    }

                    //iniciamos la ivy (solo lo hace si la ivy aún no está creada
                    bool newIvy = StartIvy(mousePoint + mouseNormal * infoPool.ivyParameters.minDistanceToSurface, -mouseNormal);

                    //y si la ivy ya estaba creada...
                    if (!newIvy)
                    {
                        //en caso de que no estuviera sobre ningún punto, crea una nueva rama 
                        if (overBranch == null)
                        {
                            infoPool.growth.AddBranch(infoPool.ivyContainer.branches[0], overPoint, mousePoint, mouseNormal);
                            overBranch = infoPool.ivyContainer.branches[infoPool.ivyContainer.branches.Count - 1];
                            overPoint = overBranch.branchPoints[0];
                            painting = true;
                        }
                        //En caso de que estuviera sobre un punto que no es el último de la rama, añade también otra rama, pero el punto inicial es dicho punto, en vezde la posición del ratón
                        else if (overPoint.index != overBranch.branchPoints.Count - 1)
                        {
                            infoPool.growth.AddBranch(overBranch, overPoint, overPoint.point, mouseNormal);
                            overBranch = infoPool.ivyContainer.branches[infoPool.ivyContainer.branches.Count - 1];
                            overPoint = overBranch.branchPoints[0];
                            painting = true;
                        }
                        //En caso de que estuviera en el último punto de una rama, simplemenete poniendo a true la variable painting, ya sigue pintando la rama en cuestión
                        else
                        {
                            painting = true;
                        }
                        //RefreshMesh(true, true);
                    }
                    //Si acabamos de crear la ivy, pues no hay mucho que hacer en este caso
                    else
                    {
                        overBranch = infoPool.ivyContainer.branches[0];
                        overPoint = overBranch.branchPoints[0];
                        painting = true;
                        //RefreshMesh(true, true);
                    }

                    RefreshMesh(true, true);
                    SaveIvy(!newIvy);
                }
                //Si arrastramos el ratón, checkeamos el painting (se explica en su propio método.
                else if (currentEvent.type == EventType.MouseDrag)
                {
                    RayCastSceneView(brushDistance + infoPool.ivyParameters.maxDistanceToSurface * 1.5f);
                    if (!rayCast)
                    {
                        RefreshBrushDistance();
                        RefreshBrushWS(currentEvent);
                        mousePoint = brushWS;
                        mouseNormal = -SceneView.currentDrawingSceneView.camera.transform.forward;


                        //Debug.Log("NORMAL: " + mouseNormal);
                    }

                    CheckPainting();

                    RefreshMesh(true, true);
                }
                else if (currentEvent.type == EventType.MouseUp && painting)
                {
                    StopPainting();
                    RefreshMesh(true, true);
                }
            }


            //Si tenemos un mouseup y estábamos pintando y no tenemos el alt pulsado, creamos un undo state y decimos que dejamos de pintar. 
            //Esto está fuera del if gordo de arriba porque queremos que lo haga aunque lo haga dentro del forbidden rect
            if (!currentEvent.alt && currentEvent.type == EventType.MouseUp && painting)
            {
                StopPainting();
            }

            if (currentEvent.type == EventType.MouseLeaveWindow || currentEvent.type == EventType.MouseEnterWindow)
            {
                StopPainting();
            }
        }

        private void StopPainting()
        {
            painting = false;
        }

        void CheckPainting()
        {
            if (overPoint != null)
            {
                if (Vector3.Distance(mousePoint, overPoint.point) > infoPool.ivyParameters.stepSize)
                {
                    Random.state = infoPool.growth.randomstate;
                    ProcessPoints();
                    infoPool.growth.randomstate = Random.state;
                }
            }
        }

        private void ProcessPoints()
        {
            overBranch.currentHeight = 0.001f;

            float distance = Vector3.Distance(mousePoint, overPoint.point);

            int numPoints = Mathf.CeilToInt(distance / infoPool.ivyParameters.stepSize);
            Vector3 newGrowDirection = (mousePoint - overPoint.point).normalized;

            Vector3 srcPoint = overPoint.point;


            if (dirDrag == Vector3.zero)
            {
                dirDrag = Vector3.forward;
            }


            //infoPool.growth.Step();
            //infoPool.growth.AddPoint(overBranch, mousePoint, mouseNormal);
            for (int i = 1; i < numPoints; i++)
            {
                Vector3 intermediatePoint = srcPoint + (i * infoPool.ivyParameters.stepSize * newGrowDirection);

                //infoPool.growth.AddDrawingPoint(overBranch, intermediatePoint, mouseNormal, dirDrag);
                infoPool.growth.AddPoint(overBranch, intermediatePoint, mouseNormal);


                overPoint = overPoint.GetNextPoint();

                //RefreshMesh(true, true);
            }


            overBranch.growDirection = newGrowDirection;
            infoPool.growth.randomstate = Random.state;
        }

        private bool StartIvy(Vector3 firstPoint, Vector3 firstGrabVector)
        {
            bool needToCreateNewIvy = (infoPool == null) || (infoPool.ivyContainer.branches.Count == 0) || (infoPool.ivyContainer.ivyGO == null);


            if (needToCreateNewIvy)
            {
                realIvyProWindow.CreateIvyGO(firstPoint, firstGrabVector);
                mf = RealIvyWindow.controller.mf;
                infoPool.growth.Initialize(firstPoint, firstGrabVector);
                infoPool.meshBuilder.InitLeavesData();
                infoPool.meshBuilder.Initialize();
            }

            return needToCreateNewIvy;
        }
    }
}