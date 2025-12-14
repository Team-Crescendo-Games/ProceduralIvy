using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public abstract class AMode
    {
        protected InfoPool infoPool;
        protected Vector3 mouseNormal;
        protected Vector3 mousePoint;

        protected BranchContainer cursorSelectedBranch;
        protected BranchPoint cursorSelectedPoint;
        protected BranchPoint[] overSegment;
        private bool pressingMiddleButton;
        private bool pressingRightButton;

        protected bool toolPaintingAllowed;

        public void Init(InfoPool infoPool)
        {
            this.infoPool = infoPool;
        }

        public void Update(Event currentEvent, Rect forbiddenRect)
        {
            ProcessEvent(currentEvent, forbiddenRect);
        }
        
        private void ProcessEvent(Event currentEvent, Rect forbiddenRect)
        {
            if (currentEvent.type == EventType.MouseDown)
            {
                if (currentEvent.button == 2) pressingMiddleButton = true;

                if (currentEvent.button == 1) pressingRightButton = true;
            }
            else if (currentEvent.type == EventType.MouseUp)
            {
                if (currentEvent.button == 2) pressingMiddleButton = false;

                if (currentEvent.button == 1) pressingRightButton = false;
            }

            var mousePositionInForbiddenRect = forbiddenRect.Contains(currentEvent.mousePosition);
            var stopPainting = pressingRightButton || pressingMiddleButton || currentEvent.alt ||
                               mousePositionInForbiddenRect;

            toolPaintingAllowed = !stopPainting;
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

            if ((segment[0].GetScreenspacePosition() - pointSS).sqrMagnitude < (segment[1].GetScreenspacePosition() - pointSS).sqrMagnitude)
                res = segment[0];
            else
                res = segment[1];

            if ((pointSS - res.GetScreenspacePosition()).magnitude > maxDistance) res = null;

            return res;
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

        protected void RefreshMesh(bool repositionLeaves, bool updatePositionLeaves)
        {
            if (repositionLeaves && infoPool.ivyParameters.generateLeaves)
                foreach (var branch in infoPool.ivyContainer.branches)
                    branch.RepositionLeaves(updatePositionLeaves);
            
            ProceduralIvyEditorWindow.Instance.RebuildMesh(true);
        }
    }
}