using System.Collections.Generic;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class RuntimeIvyGrowth
	{
		private RTIvyContainer rtIvyContainer;
		private IvyParameters ivyParameters;
		private GameObject ivyGO;
		private RTMeshData[] leavesMeshesByChosenLeaf;

		private RTBranchContainer[] branchesPool;
		private int branchesPoolIndex;

		private RTBranchPoint[] branchPointsPool;
		private int branchPointPoolIndex;
		
		private RTLeafPoint[] leavesPool;
		private int leavesPoolIndex;

		private int numPoints;
		private int numLeaves;
		private int maxNumVerticesPerLeaf;


		public void Init(RTIvyContainer ivyContainer, IvyParameters ivyParameters,
			GameObject ivyGO, RTMeshData[] leavesMeshesByChosenLeaf, 
			int numPoints, int numLeaves, int maxNumVerticesPerLeaf)
		{
			this.rtIvyContainer = ivyContainer;
			this.ivyParameters = ivyParameters;
			this.ivyGO = ivyGO;
			this.leavesMeshesByChosenLeaf = leavesMeshesByChosenLeaf;
			this.numPoints = numPoints;
			this.numLeaves = numLeaves;
			this.maxNumVerticesPerLeaf = maxNumVerticesPerLeaf;

			this.branchPointsPool = new RTBranchPoint[numPoints];
			this.branchPointPoolIndex = 0;

			for(int i = 0; i < numPoints; i++)
			{
				RTBranchPoint branchPoint = new RTBranchPoint();
				branchPoint.PreInit(ivyParameters);
				branchPointsPool[i] = branchPoint;
			}

			this.leavesPool = new RTLeafPoint[numLeaves];
			this.leavesPoolIndex = 0;
			for(int i = 0; i < numLeaves; i++)
			{
				RTLeafPoint leafPoint = new RTLeafPoint();
				leafPoint.PreInit(maxNumVerticesPerLeaf);
				leavesPool[i] = leafPoint;
			}

			this.branchesPool = new RTBranchContainer[ivyParameters.maxBranchs];
			for(int i = 0; i < ivyParameters.maxBranchs; i++)
			{
				this.branchesPool[i] = new RTBranchContainer(numPoints, numLeaves);
			}


			Random.InitState(System.Environment.TickCount);


			RTBranchContainer firstBranch = GetNextBranchContainer();
			


			ivyContainer.AddBranch(firstBranch);




			RTBranchPoint nextRTBranchPoint = GetNextFreeBranchPoint();
			nextRTBranchPoint.SetValues(ivyGO.transform.position, -ivyGO.transform.up, false, 0);
			firstBranch.AddBranchPoint(nextRTBranchPoint, ivyParameters.stepSize);

			CalculateVerticesLastPoint(firstBranch);
			//Vector3 axis = GetLoopAxis(nextRTBranchPoint, firstBranch, rtIvyContainer, ivyGO);
			//Vector3 firstVector = GetFirstVector(nextRTBranchPoint, firstBranch, rtIvyContainer, ivyParameters, axis);
			//nextRTBranchPoint.CalculateCenterLoop(ivyGO);
			//nextRTBranchPoint.CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO, firstVector, axis);








			ivyContainer.branches[0].growDirection = Quaternion.AngleAxis(Random.value * 360f, ivyGO.transform.up) * 
				ivyGO.transform.forward;

			ivyContainer.firstVertexVector = ivyContainer.branches[0].growDirection;
			ivyContainer.branches[0].randomizeHeight = Random.Range(4f, 8f);
			CalculateNewHeight(ivyContainer.branches[0]);
			ivyContainer.branches[0].branchSense = ChooseBranchSense();
			randomstate = Random.state;
		}


		public Random.State randomstate;

		//Este método es para calcular la altura del próximo punto
		void CalculateNewHeight(RTBranchContainer branch)
		{
			branch.heightVar = (Mathf.Sin(branch.heightParameter * ivyParameters.DTSFrequency - 45f) + 1f) / 2f;

			branch.newHeight = Mathf.Lerp(ivyParameters.minDistanceToSurface, ivyParameters.maxDistanceToSurface, branch.heightVar);
			branch.newHeight += (Mathf.Sin(branch.heightParameter * ivyParameters.DTSFrequency * branch.randomizeHeight) + 1) / 2f * ivyParameters.maxDistanceToSurface / 4f * ivyParameters.DTSRandomness;

			branch.deltaHeight = branch.currentHeight - branch.newHeight;
			branch.currentHeight = branch.newHeight;
		}

		//Un random penco
		int ChooseBranchSense()
		{
			if (Random.value < 0.5f)
				return -1;
			else
				return 1;
		}

		//todo parte del calculatenewpoint, a partir de ahí se entrama todo
		public void Step()
		{
			Random.state = randomstate;

			for (int b = 0; b < rtIvyContainer.branches.Count; b++)
			{
				//aumentamos el parámetro en el que basamos la altura. podría hacerse al final de la iteración, pero como no sabes dónde va a terminar, mejor ponerlo al principio
				rtIvyContainer.branches[b].heightParameter += ivyParameters.stepSize;
				CalculateNewPoint(rtIvyContainer.branches[b]);
			}

			randomstate = Random.state;
		}

		//Si la rama no está cayendo (está agarrada a una superficie) calculamos la nueva altura del próximo punto y buscamos un muro delante. Si está cayendo, buscamos el próximo punto de la caída.
		void CalculateNewPoint(RTBranchContainer branch)
		{
			if (!branch.falling)
			{
				CalculateNewHeight(branch);
				CheckWall(branch);
			}
			else
			{
				CheckFall(branch);
			}
		}

		//Definimos el punto a checkear y la dirección a él. Tiramos un raycast y si está libre buscamos el suelo. Si por el contrario topamos con un muro, añadimos un punto y calculamos una nueva growdirection
		void CheckWall(RTBranchContainer branch)
		{

			RTBranchPoint checkPoint = GetNextFreeBranchPoint();
			checkPoint.point = branch.GetLastBranchPoint().point + branch.growDirection * ivyParameters.stepSize + branch.GetLastBranchPoint().grabVector * branch.deltaHeight;
			checkPoint.index = branch.branchPoints.Count;
			//checkPoint.length = 0f;


			Vector3 direction = checkPoint.point - branch.GetLastBranchPoint().point;

			Ray ray = new Ray(branch.branchPoints[branch.branchPoints.Count - 1].point, direction);
			RaycastHit RC;
			if (!Physics.Raycast(ray, out RC, ivyParameters.stepSize * 1.15f, ivyParameters.layerMask.value))
			{
				CheckFloor(branch, checkPoint, -branch.GetLastBranchPoint().grabVector);
			}
			else
			{
				NewGrowDirectionAfterWall(branch, -branch.GetLastBranchPoint().grabVector, RC.normal);
				AddPoint(branch, RC.point, RC.normal);
			}
		}

		//Si no encontramos muro en el paso anterior, entonces buscamos si tenemos suelo. tiramos el rayo y si da positivo, añadimos punto, calculamos growdirection y decimos al sistema que no estamos cayendo. Si por el contrario no 
		//hemos encontrado suelo, intenamos agarrarnos al otro lado de la posible esquina.
		void CheckFloor(RTBranchContainer branch, RTBranchPoint potentialPoint, Vector3 oldSurfaceNormal)
		{
			Ray ray = new Ray(potentialPoint.point, -oldSurfaceNormal);
			RaycastHit RC;
			if (Physics.Raycast(ray, out RC, branch.currentHeight * 2f, ivyParameters.layerMask.value))
			{
				AddPoint(branch, RC.point, RC.normal);
				NewGrowDirection(branch);
				branch.fallIteration = 0f;
				branch.falling = false;
			}
			else
			{
				if (Random.value < ivyParameters.grabProvabilityOnFall)
				{
					CheckCorner(branch, potentialPoint, oldSurfaceNormal);
				}
				else
				{
					AddFallingPoint(branch);
					branch.fallIteration += 1f - ivyParameters.stiffness;
					branch.falling = true;
					branch.currentHeight = 0f;
					branch.heightParameter = -45f;
				}
			}
		}

		//Si hábíamos perdido pie, comprobamos si estamos en una esquina e intentamos seguir por el otro lado de lamisma
		void CheckCorner(RTBranchContainer branch, RTBranchPoint potentialPoint, Vector3 oldSurfaceNormal)
		{
			Ray ray = new Ray(potentialPoint.point + branch.branchPoints[branch.branchPoints.Count - 1].grabVector * 2f * branch.currentHeight, -branch.growDirection);
			RaycastHit RC;
			if (Physics.Raycast(ray, out RC, ivyParameters.stepSize * 1.15f, ivyParameters.layerMask.value))
			{
				AddPoint(branch, potentialPoint.point, oldSurfaceNormal);
				AddPoint(branch, RC.point, RC.normal);

				NewGrowDirectionAfterCorner(branch, oldSurfaceNormal, RC.normal);
			}
			else
			{
				AddFallingPoint(branch);
				branch.fallIteration += 1f - ivyParameters.stiffness;
				branch.falling = true;
				branch.currentHeight = 0f;
				branch.heightParameter = -45f;
			}
		}

		//Este se usa si estamos en una caída. Está la probabilidad de buscar una superficie donde agarrarnos (checkgrabpoint). Si topamos con una superficie se añade punto y se dice al sistema que no estamos cayendo
		void CheckFall(RTBranchContainer branch)
		{
			Ray ray = new Ray(branch.branchPoints[branch.branchPoints.Count - 1].point, branch.growDirection);
			RaycastHit RC;
			if (!Physics.Raycast(ray, out RC, ivyParameters.stepSize * 1.15f, ivyParameters.layerMask.value))
			{
				if (Random.value < ivyParameters.grabProvabilityOnFall)
				{
					CheckGrabPoint(branch);
				}
				else
				{
					NewGrowDirectionFalling(branch);
					AddFallingPoint(branch);
					branch.fallIteration += 1f - ivyParameters.stiffness;
					branch.falling = true;
				}
			}
			else
			{
				NewGrowDirectionAfterFall(branch, RC.normal);
				AddPoint(branch, RC.point, RC.normal);
				branch.fallIteration = 0f;
				branch.falling = false;
			}
		}

		//Con esto tiramos rayos alrededor del último punto buscando una superficie donde agarrarnos.
		void CheckGrabPoint(RTBranchContainer branch)
		{
			for (int i = 0; i < 6; i++)
			{
				float angle = (Mathf.Rad2Deg * 2 * Mathf.PI / 6) * i;
				Ray ray = new Ray(branch.branchPoints[branch.branchPoints.Count - 1].point + branch.growDirection * ivyParameters.stepSize, Quaternion.AngleAxis(angle, branch.growDirection) * branch.GetLastBranchPoint().grabVector);
				RaycastHit RC;
				if (Physics.Raycast(ray, out RC, ivyParameters.stepSize * 2f, ivyParameters.layerMask.value))
				{
					AddPoint(branch, RC.point, RC.normal);
					NewGrowDirectionAfterGrab(branch, RC.normal);
					branch.fallIteration = 0f;
					branch.falling = false;
					break;
				}
				else if (i == 5)
				{
					AddFallingPoint(branch);
					NewGrowDirectionFalling(branch);
					branch.fallIteration += 1f - ivyParameters.stiffness;
					branch.falling = true;
				}
			}
		}

		//Añadimos punto y todo lo que ello conlleva. Está la posibilidad de spawnear una rama
		public void AddPoint(RTBranchContainer branch, Vector3 point, Vector3 normal)
		{
			branch.totalLength += ivyParameters.stepSize;

			RTBranchPoint branchPoint = GetNextFreeBranchPoint();
			branchPoint.SetValues(point + normal * branch.currentHeight, -normal);
			branch.AddBranchPoint(branchPoint, ivyParameters.stepSize);


			CalculateVerticesLastPoint(branch);
			//Vector3 axis = GetLoopAxis(branchPoint, branch, rtIvyContainer, ivyGO);
			//Vector3 firstVector = GetFirstVector(branchPoint, branch, rtIvyContainer, ivyParameters, axis);
			//branchPoint.CalculateCenterLoop(ivyGO);
			//branchPoint.CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO, firstVector, axis);

			

			/*if(branch.branchPoints.Count >= 1)
			{
				branch.branchPoints[branch.branchPoints.Count - 2].CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO);
			}*/


			if (Random.value < ivyParameters.branchProvability && rtIvyContainer.branches.Count < ivyParameters.maxBranchs)
			{
				AddBranch(branch, branch.GetLastBranchPoint(), branch.branchPoints[branch.branchPoints.Count - 1].point, normal);
			}


			if (ivyParameters.generateLeaves)
			{
				AddLeave(branch);
			}
		}

		private float CalculateRadius(float lenght)
		{
			float value = (Mathf.Sin(lenght * ivyParameters.radiusVarFreq + ivyParameters.radiusVarOffset) + 1f) / 2f;
			float radius = Mathf.Lerp(ivyParameters.minRadius, ivyParameters.maxRadius, value);

			return radius;
		}

		private float CalculateLeafScale(BranchContainer branch, LeafPoint leafPoint)
		{
			//Aquí la escala, que es facilita, incluyendo el tip influence
			float scale = Random.Range(ivyParameters.minScale, ivyParameters.maxScale);

			if (leafPoint.lpLength - 0.1f >= branch.totalLenght - ivyParameters.tipInfluence)
			{
				scale *= Mathf.InverseLerp(branch.totalLenght, branch.totalLenght - ivyParameters.tipInfluence, leafPoint.lpLength);
			}
			return scale;
		}

		private Quaternion CalculateLeafRotation(LeafPoint leafPoint)
		{
			Vector3 left, forward;
			Quaternion res;
			//Aquí cálculos de orientación en función de las opciones de rotación
			if (!ivyParameters.globalOrientation)
			{
				forward = leafPoint.lpForward;
				left = leafPoint.left;
			}
			else
			{
				forward = ivyParameters.globalRotation;
				left = Vector3.Normalize(Vector3.Cross(ivyParameters.globalRotation, leafPoint.lpUpward));
			}
			//Y aplicamos la rotación
			res = Quaternion.LookRotation(leafPoint.lpUpward, forward);
			res = Quaternion.AngleAxis(ivyParameters.rotation.x, left) * Quaternion.AngleAxis(ivyParameters.rotation.y, leafPoint.lpUpward) * Quaternion.AngleAxis(ivyParameters.rotation.z, forward) * res;
			res = Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.x, ivyParameters.randomRotation.x), left) * 
				Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.y, ivyParameters.randomRotation.y), leafPoint.lpUpward) * 
				Quaternion.AngleAxis(Random.Range(-ivyParameters.randomRotation.z, ivyParameters.randomRotation.z), forward) * 
				res;

			return res;
		}

		//Añadimos punto y todo lo que ello conlleva. Es ligeramente diferente a AddPoint. Está la posibilidad de spawnear una rama
		void AddFallingPoint(RTBranchContainer branch)
		{
			Vector3 grabVector = branch.rotationOnFallIteration * branch.GetLastBranchPoint().grabVector;


			RTBranchPoint branchPoint = GetNextFreeBranchPoint();
			branchPoint.point = branch.branchPoints[branch.branchPoints.Count - 1].point + branch.growDirection * ivyParameters.stepSize;
			branchPoint.grabVector = grabVector;
			branch.AddBranchPoint(branchPoint, ivyParameters.stepSize);



			CalculateVerticesLastPoint(branch);
			//Vector3 axis = GetLoopAxis(branchPoint, branch, rtIvyContainer, ivyGO);
			//Vector3 firstVector = GetFirstVector(branchPoint, branch, rtIvyContainer, ivyParameters, axis);
			//branchPoint.CalculateCenterLoop(ivyGO);
			//branchPoint.CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO, firstVector, axis);


			/*if (branch.branchPoints.Count >= 1)
			{
				branch.branchPoints[branch.branchPoints.Count - 2].CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO);
			}*/


			if (Random.value < ivyParameters.branchProvability && rtIvyContainer.branches.Count < ivyParameters.maxBranchs)
			{
				AddBranch(branch, branch.GetLastBranchPoint(), branch.branchPoints[branch.branchPoints.Count - 1].point, -branch.GetLastBranchPoint().grabVector);
			}

			if (ivyParameters.generateLeaves)
			{
				AddLeave(branch);
			}
		}

		private void CalculateVerticesLastPoint(RTBranchContainer rtBranchContainer)
		{
			if(rtBranchContainer.branchPoints.Count > 1)
			{
				RTBranchPoint branchPoint = rtBranchContainer.branchPoints[rtBranchContainer.branchPoints.Count - 2];

				float radius = CalculateRadius(branchPoint.length);
				Vector3 axis = GetLoopAxis(branchPoint, rtBranchContainer, rtIvyContainer, ivyGO);
				Vector3 firstVector = GetFirstVector(branchPoint, rtBranchContainer, rtIvyContainer, ivyParameters, axis);
				branchPoint.CalculateCenterLoop(ivyGO);
				branchPoint.CalculateVerticesLoop(ivyParameters, rtIvyContainer, ivyGO, firstVector, axis, radius);
			}
		}

		//Todo lo necesario para añadir una nueva hoja
		void AddLeave(RTBranchContainer branch)
		{
			if (branch.branchPoints.Count % (ivyParameters.leaveEvery + Random.Range(0, ivyParameters.randomLeaveEvery)) == 0)
			{
				int chosenLeaf = Random.Range(0, ivyParameters.leavesPrefabs.Length);

				RTBranchPoint initSegment = branch.branchPoints[branch.branchPoints.Count - 2];
				RTBranchPoint endSegment = branch.branchPoints[branch.branchPoints.Count - 1];
				Vector3 leafPoint = Vector3.Lerp(initSegment.point, endSegment.point, 0.5f);


				float leafScale = Random.Range(ivyParameters.minScale, ivyParameters.maxScale);
				RTLeafPoint leafAdded = GetNextLeafPoint();
				leafAdded.SetValues(leafPoint, branch.totalLength, branch.growDirection,
					-branch.GetLastBranchPoint().grabVector, chosenLeaf, initSegment, endSegment, leafScale, ivyParameters);

				RTMeshData leafMeshData = leavesMeshesByChosenLeaf[leafAdded.chosenLeave];
				leafAdded.CreateVertices(ivyParameters, leafMeshData, ivyGO);

				branch.AddLeaf(leafAdded);
			}
		}

		public void DeleteLastBranch()
		{
			rtIvyContainer.branches.RemoveAt(rtIvyContainer.branches.Count - 1);
		}

		//Todo lo necesario para añadir una rama
		public void AddBranch(RTBranchContainer branch, RTBranchPoint originBranchPoint, Vector3 point, Vector3 normal)
		{
			RTBranchContainer newBranchContainer = GetNextBranchContainer();


			RTBranchPoint nextPoint = GetNextFreeBranchPoint();
			nextPoint.SetValues(point, -normal);
			newBranchContainer.AddBranchPoint(nextPoint, ivyParameters.stepSize);


			newBranchContainer.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(branch.growDirection, normal));
			newBranchContainer.randomizeHeight = Random.Range(4f, 8f);
			newBranchContainer.currentHeight = branch.currentHeight;
			newBranchContainer.heightParameter = branch.heightParameter;
			newBranchContainer.branchSense = ChooseBranchSense();
			
			rtIvyContainer.AddBranch(newBranchContainer);

			originBranchPoint.InitBranchInThisPoint(newBranchContainer.branchNumber);
		}

		//Cálculos de nuevas growdirection en diferentes casuísticas
		void NewGrowDirection(RTBranchContainer branch)
		{
			branch.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(Quaternion.AngleAxis(Mathf.Sin(branch.branchSense * branch.totalLength * ivyParameters.directionFrequency * (1 + Random.Range(-ivyParameters.directionRandomness, ivyParameters.directionRandomness))) *
				ivyParameters.directionAmplitude * ivyParameters.stepSize * 10f * Mathf.Max(ivyParameters.directionRandomness, 1f),
				branch.GetLastBranchPoint().grabVector) * branch.growDirection,
				branch.GetLastBranchPoint().grabVector));
		}

		void NewGrowDirectionAfterWall(RTBranchContainer branch, Vector3 oldSurfaceNormal, Vector3 newSurfaceNormal)
		{
			branch.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(oldSurfaceNormal, newSurfaceNormal));
		}

		void NewGrowDirectionFalling(RTBranchContainer branch)
		{
			Vector3 newGrowDirection = Vector3.Lerp(branch.growDirection, ivyParameters.gravity, branch.fallIteration / 10f);
			newGrowDirection = Quaternion.AngleAxis(Mathf.Sin(branch.branchSense * branch.totalLength * ivyParameters.directionFrequency * (1 + Random.Range(-ivyParameters.directionRandomness / 8f, ivyParameters.directionRandomness / 8f))) *
				ivyParameters.directionAmplitude * ivyParameters.stepSize * 5f * Mathf.Max(ivyParameters.directionRandomness / 8f, 1f),
				branch.GetLastBranchPoint().grabVector) * newGrowDirection;

			newGrowDirection = Quaternion.AngleAxis(Mathf.Sin(branch.branchSense * branch.totalLength * ivyParameters.directionFrequency / 2f * (1 + Random.Range(-ivyParameters.directionRandomness / 8f, ivyParameters.directionRandomness / 8f))) *
				ivyParameters.directionAmplitude * ivyParameters.stepSize * 5f * Mathf.Max(ivyParameters.directionRandomness / 8f, 1f),
				Vector3.Cross(branch.GetLastBranchPoint().grabVector, branch.growDirection)) * newGrowDirection;

			branch.rotationOnFallIteration = Quaternion.FromToRotation(branch.growDirection, newGrowDirection);
			branch.growDirection = newGrowDirection;
		}

		void NewGrowDirectionAfterFall(RTBranchContainer branch, Vector3 newSurfaceNormal)
		{
			branch.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(-branch.GetLastBranchPoint().grabVector, newSurfaceNormal));
		}

		void NewGrowDirectionAfterGrab(RTBranchContainer branch, Vector3 newSurfaceNormal)
		{
			branch.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(branch.growDirection, newSurfaceNormal));
		}

		void NewGrowDirectionAfterCorner(RTBranchContainer branch, Vector3 oldSurfaceNormal, Vector3 newSurfaceNormal)
		{
			branch.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(-oldSurfaceNormal, newSurfaceNormal));
		}


	public Vector3 GetFirstVector(RTBranchPoint rtBranchPoint, RTBranchContainer rtBranchContainer, 
		RTIvyContainer rtIvyContainer, IvyParameters ivyParameters, Vector3 axis)
	{
		Vector3 firstVector = Vector3.zero;

		if (rtBranchContainer.branchNumber == 0 && rtBranchPoint.index == 0)
		{
			if (!ivyParameters.halfgeom)
			{
				firstVector = rtIvyContainer.firstVertexVector;
			}
			else
			{
				firstVector = Quaternion.AngleAxis(90f, axis) * rtIvyContainer.firstVertexVector;
			}
		}
		else
		{
			if (!ivyParameters.halfgeom)
			{
				firstVector = Vector3.Normalize(Vector3.ProjectOnPlane(rtBranchPoint.grabVector, axis));
			}
			else
			{
				firstVector = Quaternion.AngleAxis(90f, axis) * Vector3.Normalize(Vector3.ProjectOnPlane(rtBranchPoint.grabVector, axis));
			}
		}

		return firstVector;
	}

	public Vector3 GetLoopAxis(RTBranchPoint rtBranchPoint, RTBranchContainer rtBranchContainer, RTIvyContainer rtIvyContainer, GameObject ivyGo)
	{
		Vector3 axis = Vector3.zero;

		if (rtBranchPoint.index == 0 && rtBranchContainer.branchNumber == 0)
		{
			axis = ivyGo.transform.up;
		}
		else
		{
			if(rtBranchPoint.index == 0)
			{
				axis = rtBranchPoint.GetNextPoint().point - rtBranchPoint.point;
			}
			else
			{
				axis = Vector3.Normalize(Vector3.Lerp(rtBranchPoint.point - rtBranchPoint.GetPreviousPoint().point,
					rtBranchPoint.GetNextPoint().point - rtBranchPoint.point, 0.5f));
			}
		}

		return axis;
	}

		//Algoritmo de refinamiento Se va ejecutando sobre la marcha, buscando angulos demasiado poco pronunciados y refinándolos o puntos demasiado juntos y eliminando puntos para evitar nudos
		/* void Refine(BranchContainer branch)
		{
			if (branch.branchPoints.Count > 3)
			{
				if (Vector3.Distance(branch.branchPoints[branch.branchPoints.Count - 2].point, branch.branchPoints[branch.branchPoints.Count - 3].point) < ivyParameters.stepSize * 0.7f ||
				   Vector3.Distance(branch.branchPoints[branch.branchPoints.Count - 2].point, branch.branchPoints[branch.branchPoints.Count - 1].point) < ivyParameters.stepSize * 0.7f)
				{
					branch.RemoveBranchPoint(branch.branchPoints.Count - 2);

					//branch.grabVectors.RemoveAt (branch.branchPoints.Count - 2);
					
				}

				if (Vector3.Angle(branch.branchPoints[branch.branchPoints.Count - 1].point - branch.branchPoints[branch.branchPoints.Count - 2].point, branch.branchPoints[branch.branchPoints.Count - 2].point - branch.branchPoints[branch.branchPoints.Count - 3].point) > 25f)
				{
					Vector3 last = branch.branchPoints[branch.branchPoints.Count - 1].point - branch.branchPoints[branch.branchPoints.Count - 2].point;
					Vector3 preLast = branch.branchPoints[branch.branchPoints.Count - 3].point - branch.branchPoints[branch.branchPoints.Count - 2].point;

					//BranchPoint branchPoint01 = branch.branchPoints[branch.branchPoints.Count - 2];
					branch.InsertBranchPoint(branch.branchPoints[branch.branchPoints.Count - 2].point + preLast / 2f,
						branch.branchPoints[branch.branchPoints.Count - 2].grabVector,
						branch.branchPoints.Count - 2);

					//branch.grabVectors.Insert(branch.grabVectors.Count - 2, ,branch.grabVectors[branch.grabVectors.Count-2]);
					

					branch.InsertBranchPoint(branch.branchPoints[branch.branchPoints.Count - 2].point + last / 2f,
						branch.branchPoints[branch.branchPoints.Count - 2].grabVector,
						branch.branchPoints.Count - 1);

					//branch.grabVectors.Insert(branch.grabVectors.Count - 1, branch.grabVectors[branch.grabVectors.Count-2]);
					

					branch.RemoveBranchPoint(branch.branchPoints.Count - 3);
					//branch.grabVectors.RemoveAt (branch.branchPoints.Count - 3);
					
				}
			}
		} */

		private RTBranchPoint GetNextFreeBranchPoint()
		{
			RTBranchPoint res = branchPointsPool[branchPointPoolIndex];
			branchPointPoolIndex++;

			if(branchPointPoolIndex >= branchPointsPool.Length)
			{
				System.Array.Resize(ref branchPointsPool, branchPointsPool.Length * 2);

				for(int i = branchPointPoolIndex; i < branchPointsPool.Length; i++)
				{
					RTBranchPoint branchPoint = new RTBranchPoint();
					branchPoint.PreInit(ivyParameters);
					branchPointsPool[i] = branchPoint;
				}
			}

			return res;
		}

		private RTLeafPoint GetNextLeafPoint()
		{
			RTLeafPoint res = leavesPool[leavesPoolIndex];
			leavesPoolIndex++;

			if(leavesPoolIndex >= leavesPool.Length)
			{
				System.Array.Resize(ref leavesPool, leavesPool.Length * 2);

				for(int i = leavesPoolIndex; i < leavesPool.Length; i++)
				{
					leavesPool[i] = new RTLeafPoint();
					leavesPool[i].PreInit(maxNumVerticesPerLeaf);
				}
			}

			return res;
		}

		private RTBranchContainer GetNextBranchContainer()
		{
			RTBranchContainer res = branchesPool[branchesPoolIndex];
			branchesPoolIndex++;

			return res;
		}
	}
}