using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class ModeCut : AbstractMode
	{
		private List<BranchContainer> branchesToRemove;
		private List<BranchPoint> pointsToRemove;

		public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
		{
			//Empezamos la gui para pintar los puntos en screen space
			Handles.BeginGUI();
			//Con este método guardamos en un array predeclarado todos los puntos de la enredadera en screen space
			GetBranchesPointsSS();
			//Y con este seleccionamos la rama y el punto mas cercanos al ratón en screen space
			SelectBranchPointSS(currentEvent.mousePosition, brushSize);


			if (overBranch != null && overPoint != null)
			{

				if (this.toolPaintingAllowed)
				{

					pointsToRemove = new List<BranchPoint>();
					branchesToRemove = new List<BranchContainer>();

					int initIndex = overPoint.index;
					initIndex = Mathf.Clamp(initIndex, 2, int.MaxValue);

					int endIndex = (overBranch.branchPoints.Count - initIndex);

					pointsToRemove = overBranch.branchPoints.GetRange(initIndex, endIndex);
					DrawPoints(pointsToRemove, Color.red);
					CheckOrphanBranches(pointsToRemove);
				}

				//después, si hacemos clic con el ratón removemos el punto seleccionado de la rama
				if (currentEvent.type == EventType.MouseDown && !currentEvent.alt && currentEvent.button == 0)
				{
					SaveIvy();

					ProceedToRemove();
					RefreshMesh(true, true);
				}
			}

			SceneView.RepaintAll();
			Handles.EndGUI();
		}

		private void ProceedToRemove()
		{
			overBranch.RemoveRange(pointsToRemove[0].index, pointsToRemove.Count);

			for (int i = 0; i < branchesToRemove.Count; i++)
			{
				infoPool.ivyContainer.RemoveBranch(branchesToRemove[i]);
			}
		}

		private void CheckOrphanBranches(List<BranchPoint> pointsToCheck)
		{
			for (int i = 0; i < pointsToCheck.Count; i++)
			{
				if (pointsToCheck[i].newBranch && pointsToCheck[i].newBranchNumber != overBranch.branchNumber)
				{
					BranchContainer orphanBranch = infoPool.ivyContainer.GetBranchContainerByBranchNumber(pointsToCheck[i].newBranchNumber);
					if (orphanBranch != null)
					{
						branchesToRemove.Add(orphanBranch);
						DrawPoints(orphanBranch.branchPoints, Color.blue);
						CheckOrphanBranches(orphanBranch.branchPoints);
					}
				}
			}
		}

		private void DrawPoints(List<BranchPoint> pointsToDraw, Color color)
		{
			for (int i = 0; i < pointsToDraw.Count; i++)
			{
				EditorGUI.DrawRect(new Rect(pointsToDraw[i].pointSS - Vector2.one * 2f, Vector2.one * 4f), color);
			}
		}
	}
}