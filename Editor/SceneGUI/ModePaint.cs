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

        private const float SnapSensitivity = 45f;
        private float brushDistance = 5f;
        
        private void TryAutoSelectIvy(Vector2 mousePosition, float sensitivity)
        {
            // 1. Find all Ivy components in the scene (This can be slow if you have hundreds, consider caching)
            // Replace 'ProceduralIvy' with the actual name of your MonoBehaviour if different
            var allIvies = Object.FindObjectsByType<IvyInfo>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); 
    
            IvyInfo bestCandidate = null;
            float closestDist = sensitivity;

            foreach (var ivy in allIvies)
            {
                // Skip the one we are already editing
                if (ivy.ivyData == IvyData) continue;
                if (ivy.ivyData == null || ivy.ivyData.ivyContainer == null) continue;

                foreach (var branch in ivy.ivyData.ivyContainer.branches)
                {
                    for (int i = 0; i < branch.branchPoints.Count - 1; i++)
                    {
                        var p1 = branch.branchPoints[i];
                        var p2 = branch.branchPoints[i + 1];

                        // Check distance to segment in Screen Space
                        float dist = HandleUtility.DistancePointLine(mousePosition, 
                            p1.GetScreenspacePosition(), 
                            p2.GetScreenspacePosition());

                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            bestCandidate = ivy;
                        }
                    }
                }
            }

            if (bestCandidate != null)
            {
                // this forces editor window to refresh (could be slow in scenes with large ivies)
                Selection.activeGameObject = bestCandidate.gameObject;
                SceneView.RepaintAll();
            }
        }

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, bool allowAutoSelect)
        {
            if (allowAutoSelect && !painting && !currentEvent.alt && currentEvent.type == EventType.MouseMove)
            {
                TryAutoSelectIvy(currentEvent.mousePosition, 45f);
            }
            
            if (!painting)
            {
                SelectBranchPointSS(currentEvent.mousePosition, SnapSensitivity);
                
                if (toolPaintingAllowed)
                {
                    // Calculate World Position Logic
                    if (cursorSelectedBranch != null)
                    {
                        // SNAP MODE: Lock to the calculated point on the branch
                        mousePointWS = cursorSelectedPoint != null 
                            ? cursorSelectedPoint.point 
                            : GetMousePointOverBranch(currentEvent, SnapSensitivity);
                    }
                    else
                    {
                        // SURFACE MODE: Raycast against world to find new root position
                        if (RayCastSceneView(2000f)) 
                        {
                            mousePointWS = mousePoint;
                        }
                    }

                    DrawPaintPreview(currentEvent);
                }

                SceneView.RepaintAll();
            }

            if (!forbiddenRect.Contains(currentEvent.mousePosition) && !currentEvent.alt && currentEvent.button == 0)
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    HandleMouseDown(currentEvent);
                }
                else if (currentEvent.type == EventType.MouseDrag)
                {
                    HandleMouseDrag(currentEvent);
                }
                else if (currentEvent.type == EventType.MouseUp && painting)
                {
                    StopPainting();
                    RefreshMesh(true, true);
                }
            }

            if (!currentEvent.alt && currentEvent.type == EventType.MouseUp && painting) StopPainting();

            if (currentEvent.type == EventType.MouseLeaveWindow || currentEvent.type == EventType.MouseEnterWindow)
                StopPainting();
        }

        private void DrawPaintPreview(Event currentEvent)
        {
            Handles.BeginGUI();
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.fontSize = 12;
            labelStyle.alignment = TextAnchor.MiddleLeft;
            
            Vector2 labelPos = currentEvent.mousePosition + new Vector2(20, -10);

            // branch mode
            if (cursorSelectedBranch != null)
            {
                string actionText = (cursorSelectedPoint != null && cursorSelectedPoint.index == cursorSelectedBranch.branchPoints.Count - 1) 
                    ? "Extend Branch" 
                    : "Add Branch";

                // Draw Text
                Rect labelRect = new Rect(labelPos.x, labelPos.y, 150, 20);
                GUI.Label(labelRect, actionText, labelStyle);
                
                Handles.EndGUI(); // Switch to 3D Handles

                // Draw Connection Line (Visual feedback of the bridge)
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Handles.color = EditorConstants.PaintBrushColor; 
                    Handles.DrawDottedLine(mousePointWS, hit.point, 4f);
                }

                // Draw Snap Target (Where the branch will actually start)
                Handles.color = Color.cyan;
                Handles.DrawWireCube(mousePointWS, Vector3.one * 0.04f);
                
                Vector3[] orphanPath = GetPathFromBranch(cursorSelectedBranch);
                for(int k=0; k < orphanPath.Length -1; k++)
                    Handles.DrawDottedLine(orphanPath[k], orphanPath[k+1], 4f);
            }
            else // new root mode
            {
                // Only show this if we are hitting a surface and NOT near a branch
                
                if (mouseNormal != Vector3.zero) // Ensure we actually hit something in RayCastSceneView
                {
                    GUI.Label(new Rect(labelPos.x, labelPos.y, 150, 20), "Start New Ivy", labelStyle);
                    Handles.EndGUI();

                    Handles.color = EditorConstants.PaintBrushColor;

                    // Draw a 3D sphere to show precise world position/depth
                    Handles.SphereHandleCap(0, mousePointWS, Quaternion.LookRotation(mouseNormal), 0.1f, EventType.Repaint);
    
                    // Draw the normal stick so you can see surface orientation
                    Handles.DrawLine(mousePointWS, mousePointWS + mouseNormal * 0.5f);
                }
                else
                {
                    Handles.EndGUI();
                }
            }
        }
        
        private Vector3 GetMousePointOverBranch(Event currentEvent, float brushSize)
        {
            var nearestSegment = IvyData.ivyContainer.GetNearestSegmentSS(currentEvent.mousePosition);

            Vector2 segment1PointSS = nearestSegment[0].GetScreenspacePosition();
            Vector2 segment2PointSS = nearestSegment[1].GetScreenspacePosition();
            
            var segmentDir = segment2PointSS - segment1PointSS;
            var initToMouse = currentEvent.mousePosition - segment1PointSS;

            var distanceMouseToFirstPoint = initToMouse.magnitude;

            var normalizedSegmentOffset = distanceMouseToFirstPoint / segmentDir.magnitude;
            return Vector3.Lerp(nearestSegment[0].point, nearestSegment[1].point, normalizedSegmentOffset);
        }
        
        private void RefreshBrushDistance()
        {
            var cam = SceneView.currentDrawingSceneView.camera;
            if (cursorSelectedBranch != null && cursorSelectedPoint != null)
                brushDistance = Vector3.Distance(cam.transform.position, cursorSelectedPoint.point);
            else
                brushDistance = 5f;
        }
        
        private Vector3 RefreshBrushWS(Event currentEvent)
        {
            var cam = SceneView.currentDrawingSceneView.camera;
            var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            return cam.transform.position + ray.direction * brushDistance;
        }

        private void HandleMouseDown(Event currentEvent)
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
                    
                    cursorSelectedPoint = cursorSelectedBranch.InsertBranchPoint(mousePointWS, nearestPoint.grabVector, newIndex);
                    RefreshMesh(true, true);
                }

                rayCastHit = RayCastSceneView(brushDistance + IvyData.ivyParameters.maxDistanceToSurface * 1.5f);
            }
            else
            {
                rayCastHit = RayCastSceneView(2000f);
            }

            if (!rayCastHit)
            {
                RefreshBrushDistance();
                mousePoint = RefreshBrushWS(currentEvent);
                mouseNormal = -SceneView.currentDrawingSceneView.camera.transform.forward;
            }

            var needToCreateNewIvy = IvyData == null 
                                     || IvyData.ivyContainer.branches.Count == 0
                                     || cursorSelectedBranch == null;
            
            if (needToCreateNewIvy)
            {
                float minDist = IvyData ? IvyData.ivyParameters.minDistanceToSurface : 0.1f;
                Vector3 originPoint = mousePoint + mouseNormal * minDist;
                Vector3 originNormal = -mouseNormal;
                ProceduralIvyEditorWindow.Instance.CreateNewIvyGameObject(originPoint, originNormal);
                ProceduralIvyEditorWindow.Instance.StartGrowthIvy(originPoint, originNormal);
                IvyData = ProceduralIvyEditorWindow.Instance.CurrentIvyInfo.ivyData;
                Assert.IsNotNull(IvyData);
            }

            if (!needToCreateNewIvy)
            {
                Assert.IsNotNull(cursorSelectedPoint);
                
                if (cursorSelectedPoint.index != cursorSelectedBranch.branchPoints.Count - 1)
                {
                    EditorIvyGrowth.AddBranch(IvyData, cursorSelectedBranch, cursorSelectedPoint, mouseNormal);
                    cursorSelectedBranch = IvyData.ivyContainer.branches[^1];
                    cursorSelectedPoint = cursorSelectedBranch.branchPoints[0];
                }
            }
            else
            {
                cursorSelectedBranch = IvyData.ivyContainer.branches[0];
                cursorSelectedPoint = cursorSelectedBranch.branchPoints[0];
            }
            
            painting = true;
            RefreshMesh(true, true);
        }

        private void HandleMouseDrag(Event currentEvent)
        {
            if (!RayCastSceneView(brushDistance + IvyData.ivyParameters.maxDistanceToSurface * 1.5f))
            {
                RefreshBrushDistance();
                mousePoint = RefreshBrushWS(currentEvent);
                mouseNormal = -SceneView.currentDrawingSceneView.camera.transform.forward;
            }

            CheckPainting();
            RefreshMesh(true, true);
        }

        private void StopPainting()
        {
            painting = false;
        }

        private void CheckPainting()
        {
            if (cursorSelectedPoint != null && Vector3.Distance(mousePoint, cursorSelectedPoint.point) > IvyData.ivyParameters.stepSize)
            {
                ProcessPoints();
            }
        }

        private void ProcessPoints()
        {
            cursorSelectedBranch.currentHeight = 0.001f;
            var distance = Vector3.Distance(mousePoint, cursorSelectedPoint.point);
            var numPoints = Mathf.CeilToInt(distance / IvyData.ivyParameters.stepSize);
            var newGrowDirection = (mousePoint - cursorSelectedPoint.point).normalized;
            var srcPoint = cursorSelectedPoint.point;

            if (dirDrag == Vector3.zero) dirDrag = Vector3.forward;

            for (var i = 1; i < numPoints; i++)
            {
                var intermediatePoint = srcPoint + i * IvyData.ivyParameters.stepSize * newGrowDirection;
                EditorIvyGrowth.AddPoint(IvyData, cursorSelectedBranch, intermediatePoint, mouseNormal);
                cursorSelectedPoint = cursorSelectedPoint.GetNextPoint();
            }

            cursorSelectedBranch.growDirection = newGrowDirection;
        }
    }
}