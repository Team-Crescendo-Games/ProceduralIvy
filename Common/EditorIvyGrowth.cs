using UnityEngine;

namespace Dynamite3D.RealIvy{
	public class EditorIvyGrowth : GrowthBuilder {
		public Random.State randomstate;
		//Creamos las estructuras de info para las ramas y asignamos las variables iniciales de la primera rama
		public void Initialize(Vector3 firstPoint, Vector3 firstGrabVector){
			Random.InitState (infoPool.ivyParameters.randomSeed);

			BranchContainer newBranchContainer = ScriptableObject.CreateInstance<BranchContainer>();
			newBranchContainer.Init();
			newBranchContainer.currentHeight = infoPool.ivyParameters.minDistanceToSurface;


			infoPool.ivyContainer.AddBranch(newBranchContainer);
			infoPool.ivyContainer.branches[0].AddBranchPoint(firstPoint, firstGrabVector, true, newBranchContainer.branchNumber);
			infoPool.ivyContainer.branches [0].growDirection = Quaternion.AngleAxis (Random.value * 360f, infoPool.ivyContainer.ivyGO.transform.up) * infoPool.ivyContainer.ivyGO.transform.forward;
			infoPool.ivyContainer.firstVertexVector = infoPool.ivyContainer.branches [0].growDirection;
			infoPool.ivyContainer.branches [0].randomizeHeight = Random.Range (4f, 8f);
			CalculateNewHeight (infoPool.ivyContainer.branches [0]);
			infoPool.ivyContainer.branches [0].branchSense = ChooseBranchSense ();
			randomstate = Random.state;
		}

		//Este método es para calcular la altura del próximo punto
		void CalculateNewHeight (BranchContainer branch){

			branch.heightVar = (Mathf.Sin (branch.heightParameter * infoPool.ivyParameters.DTSFrequency - 45f) + 1f) / 2f;

			branch.newHeight = Mathf.Lerp(infoPool.ivyParameters.minDistanceToSurface, infoPool.ivyParameters.maxDistanceToSurface, branch.heightVar);
			branch.newHeight += (Mathf.Sin (branch.heightParameter * infoPool.ivyParameters.DTSFrequency * branch.randomizeHeight) + 1) / 2f * infoPool.ivyParameters.maxDistanceToSurface / 4f * infoPool.ivyParameters.DTSRandomness;

			branch.newHeight = Mathf.Clamp(branch.newHeight, infoPool.ivyParameters.minDistanceToSurface, infoPool.ivyParameters.maxDistanceToSurface);

			branch.deltaHeight = branch.currentHeight - branch.newHeight;
			branch.currentHeight = branch.newHeight;
		}

		//Un random penco
		int ChooseBranchSense(){
			if (Random.value < 0.5f)
				return -1;
			else
				return 1;
		}

		//todo parte del calculatenewpoint, a partir de ahí se entrama todo
		public void Step(){
			Random.state = randomstate;

			for (int b = 0; b < infoPool.ivyContainer.branches.Count; b++) {
				//aumentamos el parámetro en el que basamos la altura. podría hacerse al final de la iteración, pero como no sabes dónde va a terminar, mejor ponerlo al principio
				infoPool.ivyContainer.branches[b].heightParameter += infoPool.ivyParameters.stepSize;
				CalculateNewPoint (infoPool.ivyContainer.branches [b]);
//				Refine (infoPool.ivyContainer.branches [b]);
			}
			randomstate = Random.state;
		}

		//Si la rama no está cayendo (está agarrada a una superficie) calculamos la nueva altura del próximo punto y buscamos un muro delante. Si está cayendo, buscamos el próximo punto de la caída.
		void CalculateNewPoint(BranchContainer branch){
			if (!branch.falling) {
				CalculateNewHeight (branch);
				CheckWall (branch);
			}
			else {
				CheckFall (branch);
			}
		}

		//Definimos el punto a checkear y la dirección a él. Tiramos un raycast y si está libre buscamos el suelo. Si por el contrario topamos con un muro, añadimos un punto y calculamos una nueva growdirection
		void CheckWall(BranchContainer branch){
			
			BranchPoint checkPoint = new BranchPoint(
				branch.GetLastBranchPoint().point + branch.growDirection * infoPool.ivyParameters.stepSize + branch.GetLastBranchPoint().grabVector * branch.deltaHeight, 
				branch.branchPoints.Count,
				0f,
				branch);

			Vector3 direction = checkPoint.point - branch.GetLastBranchPoint().point;

			Ray ray = new Ray(branch.branchPoints[branch.branchPoints.Count - 1].point, direction);
			RaycastHit RC;
			if (!Physics.Raycast (ray, out RC, infoPool.ivyParameters.stepSize * 1.15f, infoPool.ivyParameters.layerMask.value)) {
				CheckFloor (branch, checkPoint, -branch.GetLastBranchPoint().grabVector);
			}
			else {
                NewGrowDirectionAfterWall (branch, -branch.GetLastBranchPoint().grabVector, RC.normal);
                AddPoint(branch, RC.point, RC.normal);
            }
		}

		//Si no encontramos muro en el paso anterior, entonces buscamos si tenemos suelo. tiramos el rayo y si da positivo, añadimos punto, calculamos growdirection y decimos al sistema que no estamos cayendo. Si por el contrario no 
		//hemos encontrado suelo, intenamos agarrarnos al otro lado de la posible esquina.
		void CheckFloor(BranchContainer branch, BranchPoint potentialPoint, Vector3 oldSurfaceNormal){
			Ray ray = new Ray(potentialPoint.point, -oldSurfaceNormal);
            RaycastHit RC;
			if (Physics.Raycast (ray, out RC, branch.currentHeight * 2f, infoPool.ivyParameters.layerMask.value)) {
				AddPoint (branch, RC.point, RC.normal);
				NewGrowDirection (branch);
				branch.fallIteration = 0f;
				branch.falling = false;
			}
			else {
                if (Random.value < infoPool.ivyParameters.grabProvabilityOnFall)
                {
                    CheckCorner(branch, potentialPoint, oldSurfaceNormal);
                }
                else
                {
                    AddFallingPoint(branch);
                    branch.fallIteration += 1f - infoPool.ivyParameters.stiffness;
                    branch.falling = true;
                    branch.currentHeight = 0f;
                    branch.heightParameter = -45f;
                }
            }
		}

		//Si hábíamos perdido pie, comprobamos si estamos en una esquina e intentamos seguir por el otro lado de lamisma
		void CheckCorner(BranchContainer branch, BranchPoint potentialPoint, Vector3 oldSurfaceNormal){
			Ray ray = new Ray(potentialPoint.point + branch.branchPoints[branch.branchPoints.Count-1].grabVector * 2f * branch.currentHeight, -branch.growDirection);
			RaycastHit RC;
			if (Physics.Raycast(ray, out RC, infoPool.ivyParameters.stepSize * 1.15f, infoPool.ivyParameters.layerMask.value)){
				AddPoint (branch, potentialPoint.point, oldSurfaceNormal);
				AddPoint (branch, RC.point, RC.normal);

				NewGrowDirectionAfterCorner (branch, oldSurfaceNormal, RC.normal);
			}
			else{
				AddFallingPoint(branch);
				branch.fallIteration += 1f - infoPool.ivyParameters.stiffness;
				branch.falling = true;
				branch.currentHeight = 0f;
				branch.heightParameter = - 45f;
			}
		}

		//Este se usa si estamos en una caída. Está la probabilidad de buscar una superficie donde agarrarnos (checkgrabpoint). Si topamos con una superficie se añade punto y se dice al sistema que no estamos cayendo
		void CheckFall(BranchContainer branch){
			Ray ray = new Ray(branch.branchPoints[branch.branchPoints.Count - 1].point, branch.growDirection);
			RaycastHit RC;
			if (!Physics.Raycast (ray, out RC, infoPool.ivyParameters.stepSize * 1.15f, infoPool.ivyParameters.layerMask.value)) {
				if (Random.value < infoPool.ivyParameters.grabProvabilityOnFall) {
					CheckGrabPoint (branch);
				} 
				else {
					NewGrowDirectionFalling (branch);
					AddFallingPoint (branch);
					branch.fallIteration += 1f - infoPool.ivyParameters.stiffness;
					branch.falling = true;
				}
			}
			else {
				NewGrowDirectionAfterFall (branch, RC.normal);
				AddPoint (branch, RC.point, RC.normal);
				branch.fallIteration = 0f;
				branch.falling = false;
			}
		}

		//Con esto tiramos rayos alrededor del último punto buscando una superficie donde agarrarnos.
		void CheckGrabPoint(BranchContainer branch){
			for (int i = 0; i < 6; i++) {
				float angle = (Mathf.Rad2Deg * 2 * Mathf.PI / 6) * i;
				Ray ray = new Ray (branch.branchPoints [branch.branchPoints.Count - 1].point + branch.growDirection * infoPool.ivyParameters.stepSize, Quaternion.AngleAxis(angle, branch.growDirection) * branch.GetLastBranchPoint().grabVector);
				RaycastHit RC;
				if (Physics.Raycast (ray, out RC, infoPool.ivyParameters.stepSize * 2f, infoPool.ivyParameters.layerMask.value)) {
					AddPoint (branch, RC.point, RC.normal);
					NewGrowDirectionAfterGrab (branch, RC.normal);
					branch.fallIteration = 0f;
					branch.falling = false;
					break;
				}
				else if (i == 5) {
					AddFallingPoint (branch);
					NewGrowDirectionFalling (branch);
					branch.fallIteration += 1f - infoPool.ivyParameters.stiffness;
					branch.falling = true;
				}
			}
		}


		/*public void AddDrawingPoint(BranchContainer branch, Vector3 point, Vector3 normal, Vector3 paintDir)
		{
			//Vector3 dir = branch.branchPoints[branch.branchPoints.Count - 1].point - branch.branchPoints[branch.branchPoints.Count - 2].point;
			//dir = dir.normalized;

			branch.AddBranchPoint(point + normal * branch.currentHeight, -normal);
			

			branch.totalLenght += infoPool.ivyParameters.stepSize;

			branch.GetLastBranchPoint().length = branch.totalLenght;
			branch.lenghts.Add(branch.totalLenght);

			AddLeave(branch);
		}*/

		//Añadimos punto y todo lo que ello conlleva. Está la posibilidad de spawnear una rama
		public void AddPoint(BranchContainer branch, Vector3 point, Vector3 normal)
		{
			branch.totalLenght += infoPool.ivyParameters.stepSize;
			branch.heightParameter += infoPool.ivyParameters.stepSize;


			branch.AddBranchPoint(point + normal * branch.currentHeight, -normal);

			//Con este if lo que comprobamos realmente es si estamos en modo procedural o en modo pintado
			if (growing) {
				if (Random.value < infoPool.ivyParameters.branchProvability && infoPool.ivyContainer.branches.Count < infoPool.ivyParameters.maxBranchs) {

					AddBranch (branch, branch.GetLastBranchPoint(), branch.branchPoints[branch.branchPoints.Count - 1].point, normal);
				}
			}
			AddLeave (branch);
		}

		//Añadimos punto y todo lo que ello conlleva. Es ligeramente diferente a AddPoint. Está la posibilidad de spawnear una rama
		void AddFallingPoint(BranchContainer branch){

			Vector3 grabVector = branch.rotationOnFallIteration * branch.GetLastBranchPoint().grabVector;

			branch.totalLenght += infoPool.ivyParameters.stepSize;
			branch.AddBranchPoint(branch.branchPoints[branch.branchPoints.Count - 1].point + branch.growDirection * infoPool.ivyParameters.stepSize, 
				grabVector);

			
			if (Random.value < infoPool.ivyParameters.branchProvability && infoPool.ivyContainer.branches.Count < infoPool.ivyParameters.maxBranchs) {
				AddBranch (branch, branch.GetLastBranchPoint(), branch.branchPoints[branch.branchPoints.Count - 1].point, -branch.GetLastBranchPoint().grabVector);
			}
			AddLeave (branch);
		}

		//Todo lo necesario para añadir una nueva hoja
		void AddLeave(BranchContainer branch){
			if (branch.branchPoints.Count % (infoPool.ivyParameters.leaveEvery + Random.Range(0, infoPool.ivyParameters.randomLeaveEvery)) == 0) {
				
				/*BORRAR*/
				//branch.branchPoints[branch.branchPoints.Count - 1].AddCurrentPointAsLeafPoint();
				//branch.branchPoints[branch.branchPoints.Count - 1].AddLPLength(branch.totalLenght);
				//branch.branchPoints[branch.branchPoints.Count - 1].AddLPForward(branch.growDirection);
				//branch.branchPoints[branch.branchPoints.Count - 1].AddLPUpward(-branch.grabVectors[branch.grabVectors.Count - 1]);


				float[] probabilities = new float[infoPool.ivyParameters.leavesPrefabs.Length];
				int chosenLeave = 0;
				float max = 0f;
				for (int i = 0; i < probabilities.Length; i++) {
					probabilities [i] = Random.Range (0f, infoPool.ivyParameters.leavesProb [i]);
				}
				for (int i = 0; i < probabilities.Length; i++) {
					if (probabilities [i] >= max) {
						max = probabilities [i];
						chosenLeave = i;
					}
				}

				/*BORRAR*/
				//branch.branchPoints[branch.branchPoints.Count - 1].AddLPType(chosenLeave);


				BranchPoint initSegment = branch.branchPoints[branch.branchPoints.Count - 2];
				BranchPoint endSegment = branch.branchPoints[branch.branchPoints.Count - 1];
				Vector3 leafPoint = Vector3.Lerp(initSegment.point, endSegment.point, 0.5f);

				branch.AddLeaf(leafPoint, branch.totalLenght, branch.growDirection,
					-branch.GetLastBranchPoint().grabVector, chosenLeave, initSegment, endSegment);
			}
		}

		public void DeleteLastBranch()
		{
			infoPool.ivyContainer.branches.RemoveAt(infoPool.ivyContainer.branches.Count - 1);
		}

		//Todo lo necesario para añadir una rama
		public void AddBranch(BranchContainer branch, BranchPoint originBranchPoint, Vector3 point, Vector3 normal){

			
			BranchContainer newBranchContainer = ScriptableObject.CreateInstance<BranchContainer>();
			newBranchContainer.Init();

			newBranchContainer.AddBranchPoint(point, -normal);


			//newBranchContainer.grabVectors.Add (-normal);
			newBranchContainer.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane (branch.growDirection, normal));
			newBranchContainer.randomizeHeight = Random.Range (4f, 8f);
			newBranchContainer.currentHeight = branch.currentHeight;
			newBranchContainer.heightParameter = branch.heightParameter;
			newBranchContainer.branchSense = ChooseBranchSense ();
			newBranchContainer.originPointOfThisBranch = originBranchPoint;
			//newBranchContainer.branchNumber = infoPool.ivyContainer.branches.Count;

			//Undo.RegisterCompleteObjectUndo(infoPool.ivyContainer, "Create new branch");
			//EditorUtility.SetDirty(infoPool.ivyContainer);

			//infoPool.ivyContainer.branches.Add(newBranchContainer);
			infoPool.ivyContainer.AddBranch(newBranchContainer);

			originBranchPoint.InitBranchInThisPoint(newBranchContainer.branchNumber);
		}

		//Cálculos de nuevas growdirection en diferentes casuísticas
		void NewGrowDirection(BranchContainer branch){
			branch.growDirection = Vector3.Normalize (Vector3.ProjectOnPlane (Quaternion.AngleAxis (Mathf.Sin (branch.branchSense * branch.totalLenght * infoPool.ivyParameters.directionFrequency * (1 + Random.Range (-infoPool.ivyParameters.directionRandomness, infoPool.ivyParameters.directionRandomness))) * 
				infoPool.ivyParameters.directionAmplitude * infoPool.ivyParameters.stepSize * 10f * Mathf.Max(infoPool.ivyParameters.directionRandomness, 1f), 
				branch.GetLastBranchPoint().grabVector) * branch.growDirection,
				branch.GetLastBranchPoint().grabVector));
		}

		void NewGrowDirectionAfterWall(BranchContainer branch, Vector3 oldSurfaceNormal, Vector3 newSurfaceNormal){
            branch.growDirection = Vector3.Normalize (Vector3.ProjectOnPlane (oldSurfaceNormal, newSurfaceNormal));
		}

		void NewGrowDirectionFalling(BranchContainer branch){
			Vector3 newGrowDirection = Vector3.Lerp (branch.growDirection, infoPool.ivyParameters.gravity, branch.fallIteration / 10f);
			newGrowDirection = Quaternion.AngleAxis (Mathf.Sin (branch.branchSense * branch.totalLenght * infoPool.ivyParameters.directionFrequency * (1 + Random.Range (-infoPool.ivyParameters.directionRandomness / 8f, infoPool.ivyParameters.directionRandomness / 8f))) * 
				infoPool.ivyParameters.directionAmplitude * infoPool.ivyParameters.stepSize * 5f * Mathf.Max(infoPool.ivyParameters.directionRandomness / 8f, 1f), 
				branch.GetLastBranchPoint().grabVector) * newGrowDirection;

			newGrowDirection = Quaternion.AngleAxis (Mathf.Sin (branch.branchSense * branch.totalLenght * infoPool.ivyParameters.directionFrequency / 2f * (1 + Random.Range (-infoPool.ivyParameters.directionRandomness /8f, infoPool.ivyParameters.directionRandomness / 8f))) * 
				infoPool.ivyParameters.directionAmplitude * infoPool.ivyParameters.stepSize * 5f * Mathf.Max(infoPool.ivyParameters.directionRandomness / 8f, 1f), 
				Vector3.Cross(branch.GetLastBranchPoint().grabVector, branch.growDirection)) * newGrowDirection;
			
			branch.rotationOnFallIteration = Quaternion.FromToRotation (branch.growDirection, newGrowDirection);
			branch.growDirection = newGrowDirection;
		}

		void NewGrowDirectionAfterFall(BranchContainer branch, Vector3 newSurfaceNormal){
			branch.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(-branch.GetLastBranchPoint().grabVector, newSurfaceNormal));
		}

		void NewGrowDirectionAfterGrab(BranchContainer branch, Vector3 newSurfaceNormal){
			branch.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(branch.growDirection, newSurfaceNormal));
		}

		void NewGrowDirectionAfterCorner(BranchContainer branch, Vector3 oldSurfaceNormal, Vector3 newSurfaceNormal){
			branch.growDirection = Vector3.Normalize(Vector3.ProjectOnPlane(-oldSurfaceNormal, newSurfaceNormal));
		}

		//Algoritmo de refinamiento Se va ejecutando sobre la marcha, buscando angulos demasiado poco pronunciados y refinándolos o puntos demasiado juntos y eliminando puntos para evitar nudos
		void Refine(BranchContainer branch){
			if (branch.branchPoints.Count > 3) {
				if (Vector3.Distance (branch.branchPoints [branch.branchPoints.Count - 2].point, branch.branchPoints [branch.branchPoints.Count - 3].point) < infoPool.ivyParameters.stepSize * 0.7f ||
				   Vector3.Distance (branch.branchPoints [branch.branchPoints.Count - 2].point, branch.branchPoints [branch.branchPoints.Count - 1].point) < infoPool.ivyParameters.stepSize * 0.7f) {
					branch.RemoveBranchPoint(branch.branchPoints.Count - 2);

					//branch.grabVectors.RemoveAt (branch.branchPoints.Count - 2);

				}

				if (Vector3.Angle(branch.branchPoints [branch.branchPoints.Count - 1].point - branch.branchPoints [branch.branchPoints.Count - 2].point, branch.branchPoints [branch.branchPoints.Count - 2].point - branch.branchPoints [branch.branchPoints.Count - 3].point) > 25f)
				{
					Vector3 last = branch.branchPoints [branch.branchPoints.Count - 1].point - branch.branchPoints [branch.branchPoints.Count - 2].point;
					Vector3 preLast = branch.branchPoints [branch.branchPoints.Count - 3].point - branch.branchPoints [branch.branchPoints.Count - 2].point;

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
		}

		//Algoritmo de optimización, este se ejectua desde la interfaz. Hace parecido al refine, pero en este caso no añade puntos, solo quita
		public void Optimize (){
			foreach (BranchContainer branch in infoPool.ivyContainer.branches) {
				for (int i = 1; i < branch.branchPoints.Count - 2; i++) {
					if (Vector3.Distance (branch.branchPoints [i - 1].point, branch.branchPoints [i].point) < infoPool.ivyParameters.stepSize * 0.7f) {
						branch.RemoveBranchPoint(i);

						//branch.grabVectors.RemoveAt (i);

					}

					if (Vector3.Angle (branch.branchPoints [i - 1].point - branch.branchPoints [i].point, branch.branchPoints [i].point - branch.branchPoints [i + 1].point) < infoPool.ivyParameters.optAngleBias) {
						branch.RemoveBranchPoint(i);

						//branch.grabVectors.RemoveAt (i);

					}
				}
			}
		}
	}
}