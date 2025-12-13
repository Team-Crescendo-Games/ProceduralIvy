using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class ModeRefine : AMode
    {
        private Vector3 mousePointWS = Vector3.zero;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            Handles.BeginGUI();
            GetBranchesPointsSS();
            SelectBranchPointSS(currentEvent.mousePosition, brushSize);

            if (cursorSelectedBranch != null)
            {
                if (cursorSelectedPoint == null)
                {
                    mousePointWS = GetMousePointOverBranch(currentEvent, brushSize);
                }
                else
                {
                    mousePointWS = cursorSelectedPoint.point;

                    if (cursorSelectedPoint.index != cursorSelectedBranch.branchPoints.Count - 1)
                    {
                        Vector3 newPoint = Vector3.Lerp(cursorSelectedPoint.point, cursorSelectedPoint.GetNextPoint().point, 0.5f);

                        if (cursorSelectedPoint.index != 0 && cursorSelectedPoint.index < cursorSelectedBranch.branchPoints.Count - 3)
                        {
                            var currentSegmentMagnitude =
                                Vector3.Magnitude(cursorSelectedPoint.GetNextPoint().point - cursorSelectedPoint.point);
                            var previousSegment =
                                Vector3.Normalize(cursorSelectedPoint.point - cursorSelectedPoint.GetPreviousPoint().point) *
                                currentSegmentMagnitude;
                            var nextSegment =
                                Vector3.Normalize(cursorSelectedBranch.branchPoints[cursorSelectedPoint.index + 1].point -
                                                  cursorSelectedBranch.branchPoints[cursorSelectedPoint.index + 2].point) *
                                currentSegmentMagnitude;
                            var delta = Vector3.Lerp(previousSegment, nextSegment, 0.5f);
                            newPoint = newPoint + delta * 0.2f;

                            if (toolPaintingAllowed)
                                EditorGUI.DrawRect(
                                    new Rect(HandleUtility.WorldToGUIPoint(newPoint) - Vector2.one * 2f,
                                        Vector2.one * 4f), Color.green);
                        }

                        if (currentEvent.type == EventType.MouseDown && !currentEvent.alt && currentEvent.button == 0)
                        {
                            SaveIvy();
                            var newGrabVector = Vector3.Lerp(cursorSelectedPoint.grabVector,
                                cursorSelectedPoint.GetNextPoint().grabVector, 0.5f);

                            cursorSelectedBranch.InsertBranchPoint(newPoint, newGrabVector, cursorSelectedPoint.index + 1);
                            cursorSelectedBranch.RepositionLeavesAfterAdd02(cursorSelectedBranch.branchPoints[cursorSelectedPoint.index + 1]);
                            RefreshMesh(true, true);
                        }
                    }
                }

                SceneView.RepaintAll();
            }

            Handles.BeginGUI();
        }
    }
}