
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dynamite3D.RealIvy{
	[System.Serializable]
	//Este será el contenedor de la enredadera, tendrá a su vez otros contenedores

	public class IvyContainer : ScriptableObject
	{
		public int lastNumberAssigned;
		public GameObject ivyGO;
		public List<BranchContainer>  branches;
		public Vector3 firstVertexVector;

		IvyContainer() {
			branches = new List<BranchContainer> ();
			lastNumberAssigned = 0;
		}

#if UNITY_EDITOR
        public void RecordCreated()
		{
			Undo.RegisterCreatedObjectUndo(ivyGO, "Ivy created");
		}

		public void RecordUndo()
		{
			string undoName = "Ivy modification";
			Undo.RegisterCompleteObjectUndo(this, undoName);
			for(int i = 0; i < branches.Count; i++)
			{
				//branches[i].RecordUndo(undoName);
				Undo.RegisterCompleteObjectUndo(branches[i], undoName);
			}
		}
#endif

        public void Clear()
		{
			lastNumberAssigned = 0;
			branches.Clear();
		}

		public void RemoveBranch(BranchContainer branchToDelete)
		{
			if (branchToDelete.originPointOfThisBranch != null)
			{
				branchToDelete.originPointOfThisBranch.branchContainer.ReleasePoint(branchToDelete.originPointOfThisBranch.index);
				//branchToDelete.originPointOfThisBranch.ReleasePoint();
			}
			branches.Remove(branchToDelete);


		}

		public BranchContainer GetBranchContainerByBranchNumber(int branchNumber)
		{
			BranchContainer res = null;

			for (int i = 0; i < branches.Count; i++)
			{
				if (branches[i].branchNumber == branchNumber)
				{
					res = branches[i];
					break;
				}
			}

			return res;
		}

		public BranchPoint[] GetNearestSegmentSSBelowDistance(Vector2 pointSS, float distanceThreshold)
		{
			BranchPoint[] res = null;
			BranchPoint initSegment = null;
			BranchPoint endSegment = null;


			float minDistance = distanceThreshold;

			for (int i = 0; i < branches.Count; i++)
			{
				for (int j = 1; j < branches[i].branchPoints.Count; j++)
				{
					BranchPoint a = branches[i].branchPoints[j - 1];
					BranchPoint b = branches[i].branchPoints[j];

					float d = RealIvyMathUtils.DistanceBetweenPointAndSegmentSS(pointSS, a.pointSS, b.pointSS);

					if (d <= minDistance)
					{
						minDistance = d;
						initSegment = a;
						endSegment = b;
					}
				}
			}

			if(initSegment != null && endSegment != null)
			{
				res = new BranchPoint[2];
				res[0] = initSegment;
				res[1] = endSegment;
			}
			return res;
		}

		public BranchPoint[] GetNearestSegmentSS(Vector2 pointSS)
		{
			return GetNearestSegmentSSBelowDistance(pointSS, float.MaxValue);
		}

		public void AddBranch(BranchContainer newBranchContainer)
		{
			newBranchContainer.branchNumber = lastNumberAssigned;

			lastNumberAssigned++;
			branches.Add(newBranchContainer);
		}

		/*private int GetBranchNumber()
		{
			int res = 0;

			for(int i = 0; i < branches.Count; i++)
			{
				res = Mathf.Max(res, branches[i].branchNumber);
			}

			res++;
			return res;
		}*/

		//public BranchPoint[] GetNearestSegmentSS(Vector2 pointSS)
		//{
		//	BranchPoint[] res = new BranchPoint[2];

		//	BranchPoint initSegment = null;
		//	BranchPoint endSegment = null;

		//	BranchPoint nearestPoint = GetNearestPointAllBranchesSSFrom(pointSS);
		//	BranchPoint nextPoint = nearestPoint.GetNextPoint();
		//	BranchPoint previousPoint = nearestPoint.GetPreviousPoint();


		//	if (nextPoint != null && previousPoint != null)
		//	{
		//		float distanceToNextPoint = (pointSS - nextPoint.pointSS).magnitude;
		//		float distanceToPreviousPoint = (pointSS - previousPoint.pointSS).magnitude;

		//		if (distanceToNextPoint <= distanceToPreviousPoint)
		//		{
		//			initSegment = nearestPoint;
		//			endSegment = nextPoint;
		//		}
		//		else
		//		{
		//			initSegment = previousPoint;
		//			endSegment = nearestPoint;
		//		}
		//	}

		//	res[0] = initSegment;
		//	res[1] = endSegment;

		//	return res;
		//}



		public BranchPoint GetNearestPointAllBranchesSSFrom(Vector2 pointSS)
		{
			BranchPoint res = null;
			float minDistance = float.MaxValue;

			for (int i = 0; i < branches.Count; i++)
			{
				for (int j = 0; j < branches[i].branchPoints.Count; j++)
				{
					float newSqrDst = (branches[i].branchPoints[j].pointSS - pointSS).sqrMagnitude;
					if (newSqrDst <= minDistance)
					{
						res = branches[i].branchPoints[j];
						minDistance = newSqrDst;
					}
				}
			}

			return res;
		}
	}
}