using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
    public class ModeRefine : AbstractMode
    {
        private Vector3 mousePointWS = Vector3.zero;

        public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
        {
            //Empezamos la gui para pintar los puntos en screen space
            Handles.BeginGUI();
            //Con este método guardamos en un array predeclarado todos los puntos de la enredadera en screen space
            GetBranchesPointsSS();
            //Y con este seleccionamos la rama y el punto mas cercanos al ratón en screen space
            SelectBranchPointSS(currentEvent.mousePosition, brushSize);

            if (overBranch != null)
            {
                if (overPoint == null)
                {
                    mousePointWS = GetMousePointOverBranch(currentEvent, brushSize);
                }
                else
                {
                    mousePointWS = overPoint.point;

                    if (overPoint.index != overBranch.branchPoints.Count - 1)
                    {
                        Vector3 newPoint;
                        newPoint = Vector3.Lerp(overPoint.point, overPoint.GetNextPoint().point, 0.5f);

                        if (overPoint.index != 0 && overPoint.index < overBranch.branchPoints.Count - 3)
                        {
                            float currentSegmentMagnitude = Vector3.Magnitude(overPoint.GetNextPoint().point - overPoint.point);
                            Vector3 previousSegment = Vector3.Normalize(overPoint.point - overPoint.GetPreviousPoint().point) * currentSegmentMagnitude;
                            Vector3 nextSegment = Vector3.Normalize(overBranch.branchPoints[overPoint.index + 1].point - overBranch.branchPoints[overPoint.index + 2].point) * currentSegmentMagnitude;
                            Vector3 delta = Vector3.Lerp(previousSegment, nextSegment, 0.5f);
                            newPoint = newPoint + delta * 0.2f;

                            if (toolPaintingAllowed)
                            {
                                EditorGUI.DrawRect(new Rect(HandleUtility.WorldToGUIPoint(newPoint) - Vector2.one * 2f, Vector2.one * 4f), Color.green);
                            }
                        }

                        if (currentEvent.type == EventType.MouseDown && !currentEvent.alt && currentEvent.button == 0)
                        {
                            SaveIvy();
                            Vector3 newGrabVector = Vector3.Lerp(overPoint.grabVector,
                                overPoint.GetNextPoint().grabVector, 0.5f);

                            //BranchPoint nextPoint = overBranch.branchPoints[overPoint.index + 1];
                            //float newLenght = Mathf.Lerp(overPoint.length, nextPoint.length, 0.5f);


                            overBranch.InsertBranchPoint(newPoint, newGrabVector, overPoint.index + 1);
                            //insertedPoint.length = newLenght;
                            //infoPool.ivyContainer.branches[overBranch].grabVectors.Insert(overPoint + 1, newGrabVector);

                            overBranch.RepositionLeavesAfterAdd02(overBranch.branchPoints[overPoint.index + 1]);
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