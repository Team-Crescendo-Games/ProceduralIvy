using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public abstract class AMode
    {
        protected float brushDistance = 5f;
        protected Vector3 brushWS;

        protected InfoPool infoPool;
        protected MeshFilter mf;
        protected Vector3 mouseNormal;
        protected Vector3 mousePoint;

        protected float normalizedSegmentOffset;

        protected BranchContainer overBranch;
        protected List<LeafPoint> overLeaves = new();
        protected BranchPoint overPoint;
        protected BranchPoint[] overSegment;
        protected bool pressingMidleButton;
        protected bool pressingMouseButton;
        protected bool pressingRightButton;

        protected bool rayCast;
        protected bool toolPaintingAllowed;

        public void Init(InfoPool infoPool, MeshFilter mf)
        {
            this.infoPool = infoPool;
            this.mf = mf;
        }

        public virtual void Update(Event currentEvent, Rect forbiddenRect)
        {
            ProcessEvent(currentEvent, forbiddenRect);
        }

        public void GetBranchesPointsSS()
        {
            //Iteramos las ramas
            for (var i = 0; i < infoPool.ivyContainer.branches.Count; i++)
            {
                var currentBranch = infoPool.ivyContainer.branches[i];
                //Iteramos los puntos de las ramas, calculamos su screen space y lo almacenamos en el propio punto
                for (var j = 0; j < currentBranch.branchPoints.Count; j++)
                {
                    var currentBranchPoint = currentBranch.branchPoints[j];
                    currentBranchPoint.CalculatePointSS();
                }

                if (currentBranch.originPointOfThisBranch != null)
                    currentBranch.originPointOfThisBranch.CalculatePointSS();
            }
        }

        public void SelectLeavesSS(Vector2 mousePosition, float brushSize)
        {
            var minDistance = brushSize;

            if (overBranch != null)
            {
                overLeaves.Clear();
                for (var i = 0; i < overBranch.leaves.Count; i++)
                {
                    overBranch.leaves[i].CalculatePointSS();

                    if ((overBranch.leaves[i].pointSS - mousePosition).magnitude < brushSize * 0.1f)
                        overLeaves.Add(overBranch.leaves[i]);
                }
            }
        }

        public void SelectBranchPointSS(Vector2 mousePosition, float brushSize)
        {
            var minDistance = brushSize;
            overBranch = null;
            overPoint = null;


            var nearestSegment = infoPool.ivyContainer.GetNearestSegmentSSBelowDistance(mousePosition, minDistance);

            if (nearestSegment != null)
            {
                overBranch = nearestSegment[0].branchContainer;
                overPoint = GetNearestBranchPointBySegment(mousePosition, nearestSegment, brushSize * 0.5f);
                overSegment = nearestSegment;
            }
        }

        private BranchPoint GetNearestBranchPointBySegment(Vector2 pointSS, BranchPoint[] segment, float maxDistance)
        {
            BranchPoint res = null;

            if ((segment[0].pointSS - pointSS).sqrMagnitude < (segment[1].pointSS - pointSS).sqrMagnitude)
                res = segment[0];
            else
                res = segment[1];

            if ((pointSS - res.pointSS).magnitude > maxDistance) res = null;

            return res;
        }

        protected void DrawBrush(Event currentEvent, float brushSize)
        {
            var cam = SceneView.currentDrawingSceneView.camera;
            var mousePositionX = currentEvent.mousePosition.x;
            var mousePositionY = cam.pixelHeight - currentEvent.mousePosition.y;

            Handles.color = Color.white;
            Handles.DrawWireDisc(cam.ScreenToWorldPoint(new Vector3(mousePositionX, mousePositionY, 2.5f)),
                -cam.transform.forward, 0.00065f * brushSize);
        }

        protected void RayCastSceneView(float distance)
        {
            var mouseScreenPos = Event.current.mousePosition;
            var ray = HandleUtility.GUIPointToWorldRay(mouseScreenPos);
            RaycastHit RC;
            if (Physics.Raycast(ray, out RC, distance, infoPool.ivyParameters.layerMask.value))
            {
                SceneView.lastActiveSceneView.Repaint();
                mousePoint = RC.point;
                mouseNormal = RC.normal;
                rayCast = true;
            }
            else
            {
                rayCast = false;
            }
        }

        protected void RefreshBrushWS(Event currentEvent)
        {
            var cam = SceneView.currentDrawingSceneView.camera;
            var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            brushWS = cam.transform.position + ray.direction * brushDistance;
        }

        protected Vector3 GetMousePointOverBranch(Event currentEvent, float brushSize)
        {
            var nearestSegment = infoPool.ivyContainer.GetNearestSegmentSS(currentEvent.mousePosition);

            var segmentDir = nearestSegment[1].pointSS - nearestSegment[0].pointSS;
            var initSegmentToMousePoint = currentEvent.mousePosition - nearestSegment[0].pointSS;
            var initToMouse = currentEvent.mousePosition - nearestSegment[0].pointSS;

            var distanceMouseToFirstPoint = initToMouse.magnitude;

            normalizedSegmentOffset = distanceMouseToFirstPoint / segmentDir.magnitude;
            var leafPositionSS = Vector2.Lerp(nearestSegment[0].pointSS, nearestSegment[1].pointSS,
                distanceMouseToFirstPoint / segmentDir.magnitude);
            var leafPositionWS =
                Vector3.Lerp(nearestSegment[0].point, nearestSegment[1].point, normalizedSegmentOffset);

            return leafPositionWS;
        }

        protected void RefreshBrushDistance()
        {
            var cam = SceneView.currentDrawingSceneView.camera;
            if (overBranch != null && overPoint != null)
                brushDistance = Vector3.Distance(cam.transform.position, overPoint.point);
            else
                brushDistance = 5f;
        }

        protected void RefreshMesh(bool repositionLeaves, bool updatePositionLeaves)
        {
            if (infoPool.ivyContainer.branches.Count > 0)
            {
                if (repositionLeaves && infoPool.ivyParameters.generateLeaves)
                    for (var i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                        infoPool.ivyContainer.branches[i].RepositionLeaves(updatePositionLeaves);

                infoPool.meshBuilder.BuildGeometry();
                mf.mesh = infoPool.meshBuilder.ivyMesh;
            }
        }

        protected void SaveIvy()
        {
            SaveIvy(true);
        }

        protected void SaveIvy(bool incrementGroup)
        {
            if (incrementGroup) Undo.IncrementCurrentGroup();
            infoPool.ivyContainer.RecordUndo();
        }

        protected void ProcessEvent(Event currentEvent, Rect forbiddenRect)
        {
            if (currentEvent.type == EventType.MouseDown)
            {
                pressingMouseButton = true;

                if (currentEvent.button == 2) pressingMidleButton = true;

                if (currentEvent.button == 1) pressingRightButton = true;
            }
            else if (currentEvent.type == EventType.MouseUp)
            {
                pressingMouseButton = false;

                if (currentEvent.button == 2) pressingMidleButton = false;

                if (currentEvent.button == 1) pressingRightButton = false;
            }

            if (currentEvent.type == EventType.MouseLeaveWindow || currentEvent.type == EventType.MouseEnterWindow)
                pressingMouseButton = false;

            var mousePositionInForbiddenRect = forbiddenRect.Contains(currentEvent.mousePosition);
            var stopPainting = pressingRightButton || pressingMidleButton || currentEvent.alt ||
                               mousePositionInForbiddenRect;

            toolPaintingAllowed = !stopPainting;
        }
    }
}