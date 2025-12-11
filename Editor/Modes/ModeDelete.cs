using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class ModeDelete : AbstractMode
	{
		private List<BranchContainer> branchesToRemove;

		public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize)
		{
			//Empezamos la gui para pintar los puntos en screen space
			Handles.BeginGUI();
			//Con este método guardamos en un array predeclarado todos los puntos de la enredadera en screen space
			GetBranchesPointsSS();
			//Y con este seleccionamos la rama y el punto mas cercanos al ratón en screen space

			if (toolPaintingAllowed)
			{
				SelectBranchPointSS(currentEvent.mousePosition, brushSize);

				if (overBranch != null && overBranch.branchNumber > 0)
				{
					branchesToRemove = new List<BranchContainer>();
					branchesToRemove.Add(overBranch);
					CheckOrphanBranches(overBranch.branchPoints);
					DrawPoints(overBranch.branchPoints, Color.red);


					DrawOriginBranch();

					if (currentEvent.type == EventType.MouseDown && !currentEvent.alt && currentEvent.button == 0)
					{
						SaveIvy();

						for (int i = 0; i < branchesToRemove.Count; i++)
						{
							infoPool.ivyContainer.RemoveBranch(branchesToRemove[i]);
						}

						RefreshMesh(false, false);
					}
				}

				SceneView.RepaintAll();
			}

			Handles.EndGUI();
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

		private void DrawOriginBranch()
		{
			if (overBranch.originPointOfThisBranch != null)
			{
				EditorGUI.DrawRect(new Rect(overBranch.originPointOfThisBranch.pointSS - Vector2.one * 2f, Vector2.one * 4f), Color.blue);
			}
		}
	}
}