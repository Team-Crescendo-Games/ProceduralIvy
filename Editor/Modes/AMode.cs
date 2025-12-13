using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

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

        protected BranchContainer cursorSelectedBranch;
        protected List<LeafPoint> overLeaves = new();
        protected BranchPoint cursorSelectedPoint;
        protected BranchPoint[] overSegment;
        protected bool pressingMidleButton;
        protected bool pressingMouseButton;
        protected bool pressingRightButton;

        protected bool toolPaintingAllowed;

        public void Init(InfoPool infoPool, MeshFilter mf)
        {
            this.infoPool = infoPool;
            this.mf = mf;
        }

        public void Update(Event currentEvent, Rect forbiddenRect)
        {
            ProcessEvent(currentEvent, forbiddenRect);
        }

        protected void GetBranchesPointsSS()
        {
            if (infoPool == null) return;
            foreach (var currentBranch in infoPool.ivyContainer.branches)
            {
                foreach (var point in currentBranch.branchPoints)
                    point.CalculatePointSS();

                currentBranch.originPointOfThisBranch?.CalculatePointSS();
            }
        }

        protected void SelectLeavesSS(Vector2 mousePosition, float brushSize)
        {
            if (cursorSelectedBranch != null)
            {
                overLeaves.Clear();
                for (var i = 0; i < cursorSelectedBranch.leaves.Count; i++)
                {
                    cursorSelectedBranch.leaves[i].CalculatePointSS();

                    if ((cursorSelectedBranch.leaves[i].pointSS - mousePosition).magnitude < brushSize * 0.1f)
                        overLeaves.Add(cursorSelectedBranch.leaves[i]);
                }
            }
        }

        protected void SelectBranchPointSS(Vector2 mousePosition, float brushSize)
        {
            if (infoPool == null) return;
            
            var minDistance = brushSize;
            cursorSelectedBranch = null;
            cursorSelectedPoint = null;

            var nearestSegment = infoPool.ivyContainer.GetNearestSegmentSSBelowDistance(mousePosition, minDistance);

            if (nearestSegment != null)
            {
                cursorSelectedBranch = nearestSegment[0].branchContainer;
                cursorSelectedPoint = GetNearestBranchPointBySegment(mousePosition, nearestSegment, brushSize * 0.5f);
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

        protected bool RayCastSceneView(float distance)
        {
            var mouseScreenPos = Event.current.mousePosition;
            var ray = HandleUtility.GUIPointToWorldRay(mouseScreenPos);
            LayerMask mask = infoPool ? infoPool.ivyParameters.layerMask : ~0;
            if (Physics.Raycast(ray, out RaycastHit hit, distance, mask))
            {
                SceneView.lastActiveSceneView.Repaint();
                mousePoint = hit.point;
                mouseNormal = hit.normal;
                return true;
            }
            return false;
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
            if (cursorSelectedBranch != null && cursorSelectedPoint != null)
                brushDistance = Vector3.Distance(cam.transform.position, cursorSelectedPoint.point);
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

                ProceduralIvyEditorWindow.Instance.RebuildMesh();
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
        
        // gets the current global growth controller
        // and asserts that it's growing the same ivy as the current infoPool
        protected EditorIvyGrowth GetGrowthController()
        {
            EditorIvyGrowth growthController = ProceduralIvyEditorWindow.Instance.GrowthController;
            Assert.IsNotNull(growthController);
            Assert.IsTrue(growthController.infoPool == infoPool, "Should be growing the ivy on the same object!");
            return growthController;
        }

        protected EditorMeshBuilder GetMeshBuilder()
        {
            Assert.IsNotNull(ProceduralIvyEditorWindow.Instance.MeshBuilder);
            Assert.IsTrue(ProceduralIvyEditorWindow.Instance.MeshBuilder.infoPool == infoPool, "Should be building the mesh for the same ivy!");
            return ProceduralIvyEditorWindow.Instance.MeshBuilder;
        }
    }
}