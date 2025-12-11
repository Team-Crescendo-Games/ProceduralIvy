using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{

	public class ModeSmooth : AbstractMode
	{
		private bool smoothing;

		List<Vector3> overPoints = new List<Vector3>();
		List<int> overPointsIndex = new List<int>();
		List<float> overPointsInfluences = new List<float>();
		List<float> overPointsLeavesInfluences = new List<float>();

		public void UpdateMode(Event currentEvent, Rect forbiddenRect, float brushSize, AnimationCurve brushCurve, float smoothInsensity)
		{
			if (currentEvent.type == EventType.MouseLeaveWindow || currentEvent.type == EventType.MouseEnterWindow)
			{
				StopSmoothing();
			}



			if (!smoothing && this.toolPaintingAllowed)
			{
				DrawBrush(currentEvent, brushSize);
			}
			//Empezamos la gui para pintar los puntos en screen space

			//Con este método guardamos en un array predeclarado todos los puntos de la enredadera en screen space
			GetBranchesPointsSS();
			//Si no estamos moviendo ningún punto, buscamos el overbranch, overpoint y pintamos la textura del brush en la pantalla
			if (!smoothing)
			{
				SelectBranchPointSS(currentEvent.mousePosition, brushSize);
			}

			//Si tenemos algún overbranch y no estamos moviendo ningún punto limpiamos las listas de los puntos que encontramos en la iteración anterior al alcance del brush, y llenamos 
			//estas mismas listas con los puntos para este nuevo fotograma, además pintamos los puntos al alcance del brush

			Handles.BeginGUI();
			if (overBranch != null)
			{
				overPoints.Clear();
				overPointsIndex.Clear();
				overPointsInfluences.Clear();
				overPointsLeavesInfluences.Clear();

				for (int p = 0; p < overBranch.branchPoints.Count; p++)
				{
					BranchPoint currenBranchPoint = overBranch.branchPoints[p];
					if (Vector2.Distance(currentEvent.mousePosition, currenBranchPoint.pointSS) < brushSize / 2f)
					{
						overPointsIndex.Add(p);
						overPoints.Add(overBranch.branchPoints[p].point);
						overPointsInfluences.Add(brushCurve.Evaluate(1f - Vector2.Distance(currenBranchPoint.pointSS, currentEvent.mousePosition) / brushSize * 2f));

						if (this.toolPaintingAllowed)
						{
							EditorGUI.DrawRect(new Rect(currenBranchPoint.pointSS - Vector2.one * 2f, Vector2.one * 4f), new Color(1f, 1f, 1f, overPointsInfluences[overPointsInfluences.Count - 1]));
						}
					}
				}

				//Al levantar click, si estábamos moviendo y no orbitando la cámara guardamos el estado de las enredaderas y ponemos la flag moving en falso
				if (!currentEvent.alt && currentEvent.type == EventType.MouseUp && smoothing)
				{
					StopSmoothing();
				}

				//si no estamos en el forbiddenrect, y no estamos pulsando alt y es el ratón pcipal del ratón
				if (!forbiddenRect.Contains(currentEvent.mousePosition) && !currentEvent.alt && currentEvent.button == 0)
				{
					//después, si hacemos clic con el ratón....
					if (currentEvent.type == EventType.MouseDown && overBranch != null)
					{
						SaveIvy();
						smoothing = true;
					}

					//al arrastrar calculamos el delta actualizando el worldspace del target y aplicamos el delta transformado en relación a la distancia al overpoint a los vértices guardados como afectados
					if (currentEvent.type == EventType.MouseDrag)
					{
						for (int i = 0; i < overPointsIndex.Count; i++)
						{
							//Si no estamos ni ante el primer punto de la rama ni el último
							if (overPointsIndex[i] != 0 && overPointsIndex[i] != overBranch.branchPoints.Count - 1)
							{
								//A base de lerps calculamos el punto intermedio y le aplicamos la intensidad correspondiente según la curva
								Vector3 newPoint1, newPoint2, newPoint;
								newPoint1 = Vector3.Lerp(overBranch.branchPoints[overPointsIndex[i]].point, overBranch.branchPoints[overPointsIndex[i] - 1].point, 0.1f);
								newPoint2 = Vector3.Lerp(overBranch.branchPoints[overPointsIndex[i]].point, overBranch.branchPoints[overPointsIndex[i] + 1].point, 0.1f);
								newPoint = Vector3.Lerp(overPoints[i], Vector3.Lerp(newPoint1, newPoint2, 0.5f), smoothInsensity * overPointsInfluences[i]);

								overBranch.branchPoints[overPointsIndex[i]].point = newPoint;
							}
						}


						overBranch.RepositionLeaves02();

						RefreshMesh(true, true);
					}
				}
			}
			Handles.EndGUI();

			SceneView.RepaintAll();
		}

		public void StopSmoothing()
		{
			smoothing = false;
		}
	}
}