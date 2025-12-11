using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeAddLeaves : AMode
    {
        private LeafPoint lastLeafPoint;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            GetBranchesPointsSS();
            SelectBranchPointSS(currentEvent.mousePosition, brushSize);

            if (toolPaintingAllowed)
            {
                DrawBrush(currentEvent, brushSize);
                if (overBranch != null)
                {
                    Handles.BeginGUI();

                    RefreshBrushDistance();
                    RefreshBrushWS(currentEvent);

                    mousePoint = brushWS;

                    var leafInfo = GetLeafPosition(currentEvent, brushSize);
                    DrawLeafPointSS(leafInfo);

                    if (currentEvent.button == 0)
                    {
                        if (currentEvent.type == EventType.MouseDown)
                        {
                            SaveIvy();
                            AddLeaf(leafInfo);
                        }
                        else if (currentEvent.type == EventType.MouseDrag)
                        {
                            var sqrDistanceToLastLeaf = (leafInfo.pointWS - lastLeafPoint.point).sqrMagnitude;

                            if (sqrDistanceToLastLeaf > 0.025f) AddLeaf(leafInfo);
                        }
                    }
                }

                SceneView.RepaintAll();
            }


            Handles.EndGUI();
        }

        private void AddLeaf(LeafInfo leafInfo)
        {
            var branchPoint = overBranch.GetNearestPointFrom(mousePoint);

            var nextLeaf = GetNextLeaf(leafInfo);
            var leafIndex = overBranch.leaves.IndexOf(nextLeaf);
            lastLeafPoint =
                overBranch.AddRandomLeaf(leafInfo.pointWS, overSegment[0], overSegment[1], leafIndex, infoPool);

            RefreshMesh(true, true);
        }

        private LeafInfo GetLeafPosition(Event currentEvent, float brushSize)
        {
            var nearestSegment = infoPool.ivyContainer.GetNearestSegmentSS(currentEvent.mousePosition);

            var segmentDir = nearestSegment[1].pointSS - nearestSegment[0].pointSS;
            var initSegmentToMousePoint = currentEvent.mousePosition - nearestSegment[0].pointSS;
            var initToMouse = currentEvent.mousePosition - nearestSegment[0].pointSS;

            var distanceMouseToFirstPoint = initToMouse.magnitude;

            var t = distanceMouseToFirstPoint / segmentDir.magnitude;
            var leafPositionSS = Vector2.Lerp(nearestSegment[0].pointSS, nearestSegment[1].pointSS,
                distanceMouseToFirstPoint / segmentDir.magnitude);
            var leafPositionWS = Vector3.Lerp(nearestSegment[0].point, nearestSegment[1].point, t);

            var res = new LeafInfo(leafPositionSS, leafPositionWS, t);
            return res;
        }

        private void DrawLeafPointSS(LeafInfo leafInfo)
        {
            EditorGUI.DrawRect(new Rect(leafInfo.pointSS - Vector2.one * 2f, Vector2.one * 4f), Color.red);
        }

        private LeafPoint GetNextLeaf(LeafInfo leafInfo)
        {
            LeafPoint res = null;

            var length = overSegment[0].length + Vector3.Distance(leafInfo.pointWS, overSegment[0].point);

            for (var i = 0; i < overBranch.leaves.Count; i++)
                if (res == null)
                {
                    res = overBranch.leaves[i];
                }
                else if (overBranch.leaves[i].lpLength < length)
                {
                    res = overBranch.leaves[i];
                }
                else
                {
                    res = overBranch.leaves[i];
                    break;
                }

            return res;
        }

        private struct LeafInfo
        {
            public readonly Vector2 pointSS;
            public readonly Vector3 pointWS;
            public float normalizedOffset;

            public LeafInfo(Vector2 pointSS, Vector3 pointWS, float normalizedOffset)
            {
                this.pointSS = pointSS;
                this.pointWS = pointWS;
                this.normalizedOffset = normalizedOffset;
            }
        }
    }
}