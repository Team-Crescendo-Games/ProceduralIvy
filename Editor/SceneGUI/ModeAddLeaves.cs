using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeAddLeaves : AMode
    {
        private LeafPoint lastLeafPoint;
        private bool painting;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (toolPaintingAllowed)
            {
                SelectBranchPointSS(currentEvent.mousePosition, brushSize);

                if (cursorSelectedBranch != null)
                {
                    DrawBrushPreview(currentEvent, brushSize);
                    var leafInfo = GetLeafPosition(currentEvent, brushSize);
                    DrawLeafPreview(leafInfo);
                    
                    bool canUseTool = !forbiddenRect.Contains(currentEvent.mousePosition) || GUIUtility.hotControl == controlID;

                    if (canUseTool && !currentEvent.alt && currentEvent.button == 0)
                    {
                        switch (currentEvent.type)
                        {
                            case EventType.MouseDown:
                                GUIUtility.hotControl = controlID;
                                painting = true;
                                
                                AddLeaf(leafInfo);
                                currentEvent.Use();
                                break;

                            case EventType.MouseDrag:
                                if (GUIUtility.hotControl == controlID && painting)
                                {
                                    // Prevent stacking leaves on top of each other
                                    if (lastLeafPoint != null)
                                    {
                                        var sqrDistanceToLastLeaf = (leafInfo.pointWS - lastLeafPoint.point).sqrMagnitude;
                                        if (sqrDistanceToLastLeaf > 0.025f) 
                                        {
                                            AddLeaf(leafInfo);
                                        }
                                    }
                                    else
                                    {
                                        AddLeaf(leafInfo);
                                    }
                                    currentEvent.Use();
                                }
                                break;

                            case EventType.MouseUp:
                                if (GUIUtility.hotControl == controlID)
                                {
                                    painting = false;
                                    GUIUtility.hotControl = 0;
                                    currentEvent.Use();
                                }
                                break;
                        }
                    }
                }
            }
            
            // Safety release
            if (currentEvent.type == EventType.MouseLeaveWindow && painting)
            {
                painting = false;
                GUIUtility.hotControl = 0;
            }
            
            SceneView.RepaintAll();
        }

        private void AddLeaf(LeafInfo leafInfo)
        {
            // etermine where in the leaf list this new leaf belongs based on its distance along the branch
            var nextLeaf = GetNextLeaf(leafInfo);
            var leafIndex = (nextLeaf != null) ? cursorSelectedBranch.leaves.IndexOf(nextLeaf) : -1;
            
            // Note: AddRandomLeaf handles the actual creation and mesh update
            lastLeafPoint = cursorSelectedBranch.AddRandomLeaf(leafInfo.pointWS, overSegment[0], overSegment[1], leafIndex, infoPool);

            RefreshMesh(true, true);
        }

        private LeafInfo GetLeafPosition(Event currentEvent, float brushSize)
        {
            // Projects mouse onto the segment line to find position
            var nearestSegment = infoPool.ivyContainer.GetNearestSegmentSS(currentEvent.mousePosition);

            var segment0PointSS = nearestSegment[0].GetScreenspacePosition();
            var segment1PointSS = nearestSegment[1].GetScreenspacePosition();
            
            var segmentDir = segment1PointSS - segment0PointSS;
            var initToMouse = currentEvent.mousePosition - segment0PointSS;

            var distanceMouseToFirstPoint = initToMouse.magnitude;

            // Calculate T (0 to 1) along segment
            var t = Mathf.Clamp01(distanceMouseToFirstPoint / segmentDir.magnitude);
            
            var leafPositionWS = Vector3.Lerp(nearestSegment[0].point, nearestSegment[1].point, t);

            return new LeafInfo(leafPositionWS);
        }

        private LeafPoint GetNextLeaf(LeafInfo leafInfo)
        {
            // Logic preserved: Finds the next leaf in the sequence based on length
            LeafPoint res = null;
            
            // Calculate absolute length/distance from root to this new point
            var length = overSegment[0].length + Vector3.Distance(leafInfo.pointWS, overSegment[0].point);

            for (var i = 0; i < cursorSelectedBranch.leaves.Count; i++)
            {
                if (cursorSelectedBranch.leaves[i].lpLength >= length)
                {
                    res = cursorSelectedBranch.leaves[i];
                    break;
                }
            }
            return res;
        }

        private void DrawLeafPreview(LeafInfo leafInfo)
        {
            Handles.color = EditorConstants.AddLeavesBrushColor;
            Handles.CubeHandleCap(0, leafInfo.pointWS, Quaternion.identity, 0.03f, EventType.Repaint);
        }

        private void DrawBrushPreview(Event currentEvent, float brushSize)
        {
            Vector3 brushCenter = Vector3.zero;
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

            Color c = EditorConstants.AddLeavesBrushColor;
            c.a = 0.15f;
            Handles.color = c;

            float worldSize = HandleUtility.GetHandleSize(brushCenter) * (brushSize / 80f);
            Handles.SphereHandleCap(0, brushCenter, Quaternion.identity, worldSize, EventType.Repaint);
            
            c.a = 0.3f;
            Handles.color = c;
            Handles.DrawWireDisc(brushCenter, SceneView.currentDrawingSceneView.camera.transform.forward, worldSize * 0.5f);
        }

        private struct LeafInfo
        {
            public readonly Vector3 pointWS;

            public LeafInfo(Vector3 pointWS)
            {
                this.pointWS = pointWS;
            }
        }
    }
}