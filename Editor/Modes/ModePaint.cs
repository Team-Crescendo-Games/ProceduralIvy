using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModePaint : AMode
    {
        private Vector3 dirDrag = Vector3.zero;

        private Vector3 lastMousePointWS = Vector3.zero;
        private Vector3 mousePointWS = Vector3.zero;
        private bool painting;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            GetBranchesPointsSS();
            if (!painting)
            {
                SelectBranchPointSS(currentEvent.mousePosition, brushSize);
                if (cursorSelectedBranch != null && toolPaintingAllowed)
                {
                    DrawBrush(currentEvent, brushSize);
                    Handles.BeginGUI();

                    var pointColor = Color.black;
                    if (cursorSelectedPoint == null)
                    {
                        mousePointWS = GetMousePointOverBranch(currentEvent, brushSize);
                        pointColor = Color.green;
                    }
                    else
                    {
                        mousePointWS = cursorSelectedPoint.point;
                        pointColor = Color.yellow;

                        if (cursorSelectedPoint.index == cursorSelectedBranch.branchPoints.Count - 1) pointColor = Color.cyan;
                    }

                    EditorGUI.DrawRect(
                        new Rect(HandleUtility.WorldToGUIPoint(mousePointWS) - Vector2.one * 2f, Vector2.one * 4f),
                        pointColor);
                    
                    Handles.EndGUI();
                }

                SceneView.RepaintAll();
            }

            if (!forbiddenRect.Contains(currentEvent.mousePosition) && !currentEvent.alt && currentEvent.button == 0)
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    dirDrag = (mousePointWS - lastMousePointWS).normalized;
                    lastMousePointWS = mousePointWS;

                    bool rayCastHit;

                    if (cursorSelectedBranch != null)
                    {
                        if (cursorSelectedPoint == null)
                        {
                            var nearestPoint = cursorSelectedBranch.GetNearestPointWSFrom(mousePointWS);

                            var newIndex = overSegment[1].index;

                            var nextPoint = cursorSelectedBranch.branchPoints[overSegment[1].index + 1];
                            var newLength = Mathf.Lerp(overSegment[0].length, nextPoint.length,
                                normalizedSegmentOffset);
                            cursorSelectedPoint = cursorSelectedBranch.InsertBranchPoint(mousePointWS, nearestPoint.grabVector, newIndex);

                            RefreshMesh(true, true);
                        }

                        rayCastHit = RayCastSceneView(brushDistance + infoPool.ivyParameters.maxDistanceToSurface * 1.5f);
                    }
                    else
                    {
                        rayCastHit = RayCastSceneView(2000f);
                    }

                    if (!rayCastHit)
                    {
                        RefreshBrushDistance();
                        RefreshBrushWS(currentEvent);
                        mousePoint = brushWS;
                        mouseNormal = -SceneView.currentDrawingSceneView.camera.transform.forward;
                    }

                    var needToCreateNewIvy = infoPool == null 
                                             || infoPool.ivyContainer.branches.Count == 0
                                             || cursorSelectedBranch == null;
                    
                    if (needToCreateNewIvy)
                    {
                        float minDist = infoPool ? infoPool.ivyParameters.minDistanceToSurface : 0.1f;
                        Vector3 originPoint = mousePoint + mouseNormal * minDist;
                        Vector3 originNormal = -mouseNormal;
                        ProceduralIvyEditorWindow.Instance.CreateNewIvyGameObject(originPoint, originNormal);
                        ProceduralIvyEditorWindow.Instance.StartGrowthIvy(originPoint, originNormal);
                        infoPool = ProceduralIvyEditorWindow.Instance.CurrentIvyInfo.infoPool;
                        Assert.IsNotNull(infoPool);
                    }

                    if (!needToCreateNewIvy)
                    {
                        Assert.IsNotNull(cursorSelectedBranch);
                        Assert.IsNotNull(cursorSelectedPoint);
                        
                        if (cursorSelectedPoint.index != cursorSelectedBranch.branchPoints.Count - 1)
                        {
                            GetGrowthController().AddBranch(cursorSelectedBranch, cursorSelectedPoint, cursorSelectedPoint.point, mouseNormal);
                            cursorSelectedBranch = infoPool.ivyContainer.branches[^1];
                            cursorSelectedPoint = cursorSelectedBranch.branchPoints[0];
                        }
                    }
                    else
                    {
                        cursorSelectedBranch = infoPool.ivyContainer.branches[0];
                        cursorSelectedPoint = cursorSelectedBranch.branchPoints[0];
                    }
                    
                    painting = true;
                    RefreshMesh(true, true);
                    SaveIvy(!needToCreateNewIvy);
                }
                //Si arrastramos el ratón, checkeamos el painting (se explica en su propio método.
                else if (currentEvent.type == EventType.MouseDrag)
                {
                    if (!RayCastSceneView(brushDistance + infoPool.ivyParameters.maxDistanceToSurface * 1.5f))
                    {
                        RefreshBrushDistance();
                        RefreshBrushWS(currentEvent);
                        mousePoint = brushWS;
                        mouseNormal = -SceneView.currentDrawingSceneView.camera.transform.forward;
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
            if (!currentEvent.alt && currentEvent.type == EventType.MouseUp && painting) StopPainting();

            if (currentEvent.type == EventType.MouseLeaveWindow || currentEvent.type == EventType.MouseEnterWindow)
                StopPainting();
        }

        private void StopPainting()
        {
            painting = false;
        }

        private void CheckPainting()
        {
            if (cursorSelectedPoint != null && Vector3.Distance(mousePoint, cursorSelectedPoint.point) > infoPool.ivyParameters.stepSize)
            {
                ProcessPoints();
            }
        }

        private void ProcessPoints()
        {
            cursorSelectedBranch.currentHeight = 0.001f;

            var distance = Vector3.Distance(mousePoint, cursorSelectedPoint.point);

            var numPoints = Mathf.CeilToInt(distance / infoPool.ivyParameters.stepSize);
            var newGrowDirection = (mousePoint - cursorSelectedPoint.point).normalized;

            var srcPoint = cursorSelectedPoint.point;

            if (dirDrag == Vector3.zero) dirDrag = Vector3.forward;

            for (var i = 1; i < numPoints; i++)
            {
                var intermediatePoint = srcPoint + i * infoPool.ivyParameters.stepSize * newGrowDirection;
                GetGrowthController().AddPoint(cursorSelectedBranch, intermediatePoint, mouseNormal);
                cursorSelectedPoint = cursorSelectedPoint.GetNextPoint();
            }

            cursorSelectedBranch.growDirection = newGrowDirection;
        }

        private bool StartIvy(Vector3 firstPoint, Vector3 firstGrabVector)
        {
            var needToCreateNewIvy = infoPool == null || infoPool.ivyContainer.branches.Count == 0;

            if (needToCreateNewIvy)
            {
                ProceduralIvyEditorWindow.Instance.CreateNewIvyGameObject(firstPoint, firstGrabVector);
                ProceduralIvyEditorWindow.Instance.StartGrowthIvy(firstPoint, firstGrabVector);
                infoPool = ProceduralIvyEditorWindow.Instance.CurrentIvyInfo.infoPool;
                Assert.IsNotNull(infoPool);
            }

            return needToCreateNewIvy;
        }
    }
}