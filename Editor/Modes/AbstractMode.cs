using Dynamite3D.RealIvy;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Dynamite3D.RealIvy
{
    public abstract class AbstractMode
    {
        /* EVENT FIELDS*/
        protected bool toolPaintingAllowed;
        protected bool pressingMouseButton;
        protected bool pressingMidleButton;
        protected bool pressingRightButton;


        protected InfoPool infoPool;
        protected MeshFilter mf;

        protected BranchContainer overBranch = null;
        protected BranchPoint overPoint = null;
        protected BranchPoint[] overSegment = null;
        protected List<LeafPoint> overLeaves = new List<LeafPoint>();


        protected float normalizedSegmentOffset;
        protected Vector3 mousePoint;
        protected Vector3 mouseNormal;
        protected bool rayCast;

        protected float brushDistance = 5f;
        protected Vector3 brushWS;

        protected RealIvyTools proToolsWindow;


        public void Init(InfoPool infoPool, MeshFilter mf, RealIvyTools proToolsWindow)
        {
            this.infoPool = infoPool;
            this.mf = mf;
            this.proToolsWindow = proToolsWindow;
        }

        public virtual void Update(Event currentEvent, Rect forbiddenRect)
        {
            ProcessEvent(currentEvent, forbiddenRect);
        }

        public void GetBranchesPointsSS()
        {
            //Iteramos las ramas
            for (int i = 0; i < infoPool.ivyContainer.branches.Count; i++)
            {
                BranchContainer currentBranch = infoPool.ivyContainer.branches[i];
                //Iteramos los puntos de las ramas, calculamos su screen space y lo almacenamos en el propio punto
                for (int j = 0; j < currentBranch.branchPoints.Count; j++)
                {
                    BranchPoint currentBranchPoint = currentBranch.branchPoints[j];
                    currentBranchPoint.CalculatePointSS();
                }

                if (currentBranch.originPointOfThisBranch != null)
                {
                    currentBranch.originPointOfThisBranch.CalculatePointSS();
                }
            }
        }

        public void SelectLeavesSS(Vector2 mousePosition, float brushSize)
        {
            float minDistance = brushSize;

            if (overBranch != null)
            {
                overLeaves.Clear();
                for (int i = 0; i < overBranch.leaves.Count; i++)
                {
                    overBranch.leaves[i].CalculatePointSS();

                    if ((overBranch.leaves[i].pointSS - mousePosition).magnitude < (brushSize * 0.1f))
                    {
                        overLeaves.Add(overBranch.leaves[i]);
                    }
                }
            }
        }

        public void SelectBranchPointSS(Vector2 mousePosition, float brushSize)
        {
            float minDistance = brushSize;
            overBranch = null;
            overPoint = null;


            BranchPoint[] nearestSegment = infoPool.ivyContainer.GetNearestSegmentSSBelowDistance(mousePosition, minDistance);

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
            {
                res = segment[0];
            }
            else
            {
                res = segment[1];
            }

            if ((pointSS - res.pointSS).magnitude > maxDistance)
            {
                res = null;
            }

            return res;
        }

        //public void SelectBranchPointSS(Vector2 mousePosition, float brushSize)
        //{
        //	//Definimos una serie de listas. En estas guardaremos información sobre las ramas y los puntos al alcance de la brush
        //	List<int> branchesInRange = new List<int>();
        //	List<int> nearestPointPerBranch = new List<int>();
        //	List<float> distancesPerBranch = new List<float>();

        //	//variables locales para trabajar mas cómodo un poco mas abajo después del bucle
        //	float closestDistance;
        //	int indexOfClosestDistance;

        //	//Aquí iteramos todas las ramas y todos los puntos
        //	for (int b = 0; b < infoPool.ivyContainer.branches.Count; b++)
        //	{
        //		//y guardamos en estas listas locales los puntos y las distancias de todos los que están en rango de brush
        //		List<int> pointIndexInRange = new List<int>();
        //		List<float> pointDistanceInRange = new List<float>();
        //		for (int p = 0; p < infoPool.ivyContainer.branches[b].branchPoints.Count; p++)
        //		{
        //			BranchPoint currentBranchPoint = infoPool.ivyContainer.branches[b].branchPoints[p];
        //			float distance = Vector2.Distance(currentBranchPoint.pointSS, mousePosition);
        //			if (distance < brushSize / 2f)
        //			{
        //				pointIndexInRange.Add(p);
        //				pointDistanceInRange.Add(distance);
        //			}
        //		}
        //		//para cada rama que hayamos encontrado dentro del alcance, encontramos la menor distancia y la guardamos en las listas declaradas al principio del método
        //		if (pointIndexInRange.Count > 0)
        //		{
        //			float minDistance = pointDistanceInRange.Min();
        //			int index = pointDistanceInRange.IndexOf(minDistance);
        //			nearestPointPerBranch.Add(pointIndexInRange[index]);
        //			distancesPerBranch.Add(pointDistanceInRange[index]);
        //			branchesInRange.Add(b);
        //		}
        //	}

        //	//por último si al menos hay una rama en el rango, elegimos la menor de las distancias como overbranch y overpoint
        //	if (branchesInRange.Count > 0)
        //	{
        //		closestDistance = distancesPerBranch.Min();
        //		indexOfClosestDistance = distancesPerBranch.IndexOf(closestDistance);

        //		overBranch = branchesInRange[indexOfClosestDistance];
        //		overPoint = nearestPointPerBranch[indexOfClosestDistance];
        //	}
        //	else
        //	{
        //		overBranch = -1;
        //		overPoint = -1;
        //	}
        //}

        protected void DrawBrush(Event currentEvent, float brushSize)
        {
            Camera cam = SceneView.currentDrawingSceneView.camera;
            float mousePositionX = currentEvent.mousePosition.x;
            float mousePositionY = cam.pixelHeight - currentEvent.mousePosition.y;

            Handles.color = Color.white;
            Handles.DrawWireDisc(cam.ScreenToWorldPoint(new Vector3(mousePositionX, mousePositionY, 2.5f)), -cam.transform.forward, 0.00065f * brushSize);
        }

        protected void RayCastSceneView(float distance)
        {
            Vector2 mouseScreenPos = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mouseScreenPos);
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
            Camera cam = SceneView.currentDrawingSceneView.camera;
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            brushWS = cam.transform.position + ray.direction * brushDistance;
        }

        protected Vector3 GetMousePointOverBranch(Event currentEvent, float brushSize)
        {
            BranchPoint[] nearestSegment = infoPool.ivyContainer.GetNearestSegmentSS(currentEvent.mousePosition);

            Vector2 segmentDir = (nearestSegment[1].pointSS - nearestSegment[0].pointSS);
            Vector2 initSegmentToMousePoint = (currentEvent.mousePosition - nearestSegment[0].pointSS);
            Vector2 initToMouse = currentEvent.mousePosition - nearestSegment[0].pointSS;

            float distanceMouseToFirstPoint = initToMouse.magnitude;

            this.normalizedSegmentOffset = distanceMouseToFirstPoint / segmentDir.magnitude;
            Vector2 leafPositionSS = Vector2.Lerp(nearestSegment[0].pointSS, nearestSegment[1].pointSS, distanceMouseToFirstPoint / segmentDir.magnitude);
            Vector3 leafPositionWS = Vector3.Lerp(nearestSegment[0].point, nearestSegment[1].point, normalizedSegmentOffset);

            return leafPositionWS;
        }

        protected void RefreshBrushDistance()
        {
            Camera cam = SceneView.currentDrawingSceneView.camera;
            if (overBranch != null && overPoint != null)
            {
                brushDistance = Vector3.Distance(cam.transform.position, overPoint.point);
            }
            else
            {
                brushDistance = 5f;
            }
        }

        protected void RefreshMesh(bool repositionLeaves, bool updatePositionLeaves)
        {
            if (infoPool.ivyContainer.branches.Count > 0)
            {
                if (repositionLeaves && infoPool.ivyParameters.generateLeaves)
                {
                    for (int i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                    {
                        infoPool.ivyContainer.branches[i].RepositionLeaves02(updatePositionLeaves);
                    }
                }

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
            if (incrementGroup)
            {
                Undo.IncrementCurrentGroup();
            }
            infoPool.ivyContainer.RecordUndo();
        }

        protected void ProcessEvent(Event currentEvent, Rect forbiddenRect)
        {
            if (currentEvent.type == EventType.MouseDown)
            {
                pressingMouseButton = true;

                if (currentEvent.button == 2)
                {
                    pressingMidleButton = true;
                }

                if (currentEvent.button == 1)
                {
                    pressingRightButton = true;
                }
            }
            else if (currentEvent.type == EventType.MouseUp)
            {
                pressingMouseButton = false;

                if (currentEvent.button == 2)
                {
                    pressingMidleButton = false;
                }

                if (currentEvent.button == 1)
                {
                    pressingRightButton = false;
                }
            }

            if (currentEvent.type == EventType.MouseLeaveWindow || currentEvent.type == EventType.MouseEnterWindow)
            {
                pressingMouseButton = false;
            }

            bool mousePositionInForbiddenRect = forbiddenRect.Contains(currentEvent.mousePosition);
            bool stopPainting = pressingRightButton || pressingMidleButton || currentEvent.alt || mousePositionInForbiddenRect;

            toolPaintingAllowed = !stopPainting;
        }
    }
}