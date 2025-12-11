using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dynamite3D.RealIvy{
	public class EditorMeshBuilder : ScriptableObject{
		public InfoPool infoPool;
		//La malla final de la enredadera en su conjunto
		public Mesh ivyMesh;

		//Diccionario usado en la generación de UVs de lightmap
		Dictionary<int, int[]> branchesLeavesIndices = new Dictionary<int, int[]> ();
		//Datos de la malla
		public Vector3[] verts;
		Vector3[] normals;
		Vector2[] uvs;
        Color[] vColor;
        //Triángulos de las ramas
        int[] trisBranches;
		//Triángulos de las hojas, divididos según el material de cada tipo de hoja para hacer las submeshes. Se podría hacer con arrays, pero bueno, me permito la licencia dada la complejidad
		List<List<int>> trisLeaves;

		//ángulo para la generación de cada ring
		float angle;

		//lista de materiales de las hojas
		public List <Material> leavesMaterials;
		//Aquí metemos qué tipos de hojas corresponden a cada material
		public List<List<int>> typesByMat;

		//Esto es solo para debugear, para hacer una representación 
		public Rect[] uv2Rects = new Rect[0];
		//Booleano para saber si están inicializadas las estructuras para las hojas, así no intenta construir la geometría sin tener lo necesario
		public bool leavesDataInitialized = false;

		//Aquí se inicializan las estructuras de las hojas antes de empezar a generarse la enredadera y la geometría
		public void InitLeavesData()
        {
            if (infoPool.ivyContainer.ivyGO)
            {
				infoPool.meshBuilder.typesByMat = new List<List<int>>();
				infoPool.meshBuilder.leavesMaterials = new List<Material>();

				if (infoPool.ivyParameters.generateLeaves)
                {
                    //revisamos los materiales repetidos dentro de los prefabs
                    for (int i = 0; i < infoPool.ivyParameters.leavesPrefabs.Length; i++)
                    {
                        bool materialExists = false;
                        for (int m = 0; m < infoPool.meshBuilder.leavesMaterials.Count; m++)
                        {
                            if (infoPool.meshBuilder.leavesMaterials[m] == infoPool.ivyParameters.leavesPrefabs[i].GetComponent<MeshRenderer>().sharedMaterial)
                            {
                                infoPool.meshBuilder.typesByMat[m].Add(i);
                                materialExists = true;
                            }
                        }
                        if (!materialExists)
                        {
                            infoPool.meshBuilder.leavesMaterials.Add(infoPool.ivyParameters.leavesPrefabs[i].GetComponent<MeshRenderer>().sharedMaterial);
                            infoPool.meshBuilder.typesByMat.Add(new List<int>());
                            infoPool.meshBuilder.typesByMat[infoPool.meshBuilder.typesByMat.Count - 1].Add(i);
                        }
                    }
                    //Asignamos los materiales al mesh renderer una vez recogidos de los prefabs
                    Material[] materials = new Material[leavesMaterials.Count + 1];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (i == 0)
                        {
                            materials[i] = infoPool.ivyContainer.ivyGO.GetComponent<MeshRenderer>().sharedMaterial;
                        }
                        else
                        {
                            materials[i] = infoPool.meshBuilder.leavesMaterials[i - 1];
                        }
                    }
                    infoPool.ivyContainer.ivyGO.GetComponent<MeshRenderer>().sharedMaterials = materials;
                }
                else
                {
                    infoPool.ivyContainer.ivyGO.GetComponent<MeshRenderer>().sharedMaterials = new Material[1] {infoPool.ivyParameters.branchesMaterial};
                }
                leavesDataInitialized = true;
            }
        }

        public void Initialize() {
            //Reiniciamos los triángulos de las hojas en cada iteración
            infoPool.meshBuilder.trisLeaves = new List<List<int>>();
            for (int i = 0; i < infoPool.meshBuilder.leavesMaterials.Count; i++) {
                infoPool.meshBuilder.trisLeaves.Add(new List<int>());
            }

            //Reiniciamos la malla y definimos el número de materiales
            ivyMesh.Clear();
            if (infoPool.ivyParameters.buffer32Bits)
            {
                ivyMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            ivyMesh.name = "Ivy Mesh";
            ivyMesh.subMeshCount = leavesMaterials.Count + 1;
            //Y también el diccionario usado en la creación de las uvs de lightmap
            branchesLeavesIndices.Clear();

            //Estos contadores son para calcular cuantos huecos hacen falta en los arrays de vertices y tris
            int vertCount = 0;
            int triBranchesCount = 0;
            if (infoPool.ivyParameters.generateBranches)
            {
                //Contamos los verts y tris necesarios y hacemos hueco en las arrays Por este lado las ramas
                for (int i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                {
                    if (infoPool.ivyContainer.branches[i].branchPoints.Count > 1)
                    {
                        vertCount += (infoPool.ivyContainer.branches[i].branchPoints.Count - 1) * (infoPool.ivyParameters.sides + 1) + 1;
                        triBranchesCount += (infoPool.ivyContainer.branches[i].branchPoints.Count - 2) * infoPool.ivyParameters.sides * 2 * 3 + infoPool.ivyParameters.sides * 3;
                    }
                }
            }
            if (infoPool.ivyParameters.generateLeaves && infoPool.ivyParameters.leavesPrefabs.Length > 0)
            {
                //Y por este las hojas, dependiendo de la malla de cada prefab
                for (int i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                {
                    if (infoPool.ivyContainer.branches[i].branchPoints.Count > 1)
                    {
                        for (int j = 0; j < infoPool.ivyContainer.branches[i].leaves.Count; j++)
                        {
                            BranchContainer currentBranch = infoPool.ivyContainer.branches[i];
							//BranchPoint currentBranchPoint = infoPool.ivyContainer.branches[i].branchPoints[j];
							MeshFilter leafMeshFilter = infoPool.ivyParameters.leavesPrefabs[currentBranch.leaves[j].chosenLeave].GetComponent<MeshFilter>();
							vertCount += leafMeshFilter.sharedMesh.vertexCount;


                            /*for (int p = 0; p < currentBranchPoint.leaves.Count; p++)
						    {
							    vertCount += infoPool.ivyParameters.leavesPrefabs[currentBranchPoint.leaves[p].chosenLeave].GetComponent<MeshFilter>().sharedMesh.vertexCount;
						    }*/
                        }
                    }
                }
            }
			//creamos las arrays para todos los datos de la malla (salvo los triángulos de las hojas que se van añadiendo al vuelo, pues son una listas)
			verts = new Vector3[vertCount];
			normals = new Vector3[vertCount];
			uvs = new Vector2[vertCount];
            vColor = new Color[vertCount];
            trisBranches = new int[Mathf.Max (triBranchesCount, 0)];
			//Calculamos el ángulo y tal
			if (!infoPool.ivyParameters.halfgeom) {
				angle = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides;
			} else {
				angle = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides / 2;
			}
		}

		//Aquí se construyen las hojas, este método es llamado rama a rama
		void BuildLeaves(int b, ref int vertCount){
			
			Mesh chosenLeaveMesh;
			//Se recorren los materiales
			for (int i = 0; i < leavesMaterials.Count; i++) {
				Random.InitState (b + infoPool.ivyParameters.randomSeed + i);

				for(int j = 0; j < infoPool.ivyContainer.branches[b].leaves.Count; j++)
				{
					LeafPoint currentLeaf = infoPool.ivyContainer.branches[b].leaves[j];
					

					//Ahora vemos si para el material que estamos iterando, le corresponde el tipo de hoja que tenemos en este punto
					if (typesByMat[i].Contains(currentLeaf.chosenLeave))
					{
						currentLeaf.verticesLeaves = new List<RTVertexData>();
						//Y vemos qué tipo de hoja corresponde a cada punto a cogemos esa malla
						chosenLeaveMesh = infoPool.ivyParameters.leavesPrefabs[currentLeaf.chosenLeave].GetComponent<MeshFilter>().sharedMesh;
						//definimos el vértice por el que tenemos que empezar a escribir en el array
						int firstVertex = vertCount;
						Vector3 left, forward;
						Quaternion quat;
						//Aquí cálculos de orientación en función de las opciones de rotación
						if (!infoPool.ivyParameters.globalOrientation)
						{
							forward = currentLeaf.lpForward;
							left = currentLeaf.left;
							//left = Vector3.Cross(currentLeaf.lpForward, currentLeaf.lpUpward);
						}
						else
						{
							forward = infoPool.ivyParameters.globalRotation;
							left = Vector3.Normalize(Vector3.Cross(infoPool.ivyParameters.globalRotation, currentLeaf.lpUpward));
						}
						//Y aplicamos la rotación

						quat = Quaternion.LookRotation(currentLeaf.lpUpward, forward);
						quat = Quaternion.AngleAxis(infoPool.ivyParameters.rotation.x, left) * Quaternion.AngleAxis(infoPool.ivyParameters.rotation.y, currentLeaf.lpUpward) * Quaternion.AngleAxis(infoPool.ivyParameters.rotation.z, forward) * quat;
						quat = Quaternion.AngleAxis(Random.Range(-infoPool.ivyParameters.randomRotation.x, infoPool.ivyParameters.randomRotation.x), left) * Quaternion.AngleAxis(Random.Range(-infoPool.ivyParameters.randomRotation.y, infoPool.ivyParameters.randomRotation.y), currentLeaf.lpUpward) * Quaternion.AngleAxis(Random.Range(-infoPool.ivyParameters.randomRotation.z, infoPool.ivyParameters.randomRotation.z), forward) * quat;
						quat = currentLeaf.forwarRot * quat;



						//Aquí la escala, que es facilita, incluyendo el tip influence
						float scale = Random.Range(infoPool.ivyParameters.minScale, infoPool.ivyParameters.maxScale);
						currentLeaf.leafScale = scale;



						scale *= Mathf.InverseLerp(infoPool.ivyContainer.branches[b].totalLenght, infoPool.ivyContainer.branches[b].totalLenght - infoPool.ivyParameters.tipInfluence, currentLeaf.lpLength);



						/*******************/
						currentLeaf.leafRotation = quat;
						currentLeaf.dstScale = scale;
						/*******************/



						//Metemos los triángulos correspondientes en el array correspondiente al material que estamos iterando
						for (int t = 0; t < chosenLeaveMesh.triangles.Length; t++)
						{
							int triangle = chosenLeaveMesh.triangles[t] + vertCount;
							trisLeaves[i].Add(triangle);
						}
						//ylos vértices, normales y uvs, aplicando las transformaciones pertinentes, actualizando el contador para la siguiente iteración saber por dónde vamos
						for (int v = 0; v < chosenLeaveMesh.vertexCount; v++)
						{
							Vector3 offset = left * infoPool.ivyParameters.offset.x + 
								currentLeaf.lpUpward * infoPool.ivyParameters.offset.y + 
								currentLeaf.lpForward * infoPool.ivyParameters.offset.z;

							verts[vertCount] = quat * chosenLeaveMesh.vertices[v] * scale + currentLeaf.point + offset;
							normals[vertCount] = quat * chosenLeaveMesh.normals[v];
							uvs[vertCount] = chosenLeaveMesh.uv[v];
                            vColor[vertCount] = chosenLeaveMesh.colors[v];

                            normals[vertCount] = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) * normals[vertCount];
							verts[vertCount] -= infoPool.ivyContainer.ivyGO.transform.position;
							verts[vertCount] = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) * verts[vertCount];


							RTVertexData vertexData = new RTVertexData(verts[vertCount], normals[vertCount], uvs[vertCount], Vector2.zero, vColor[vertCount]);
							currentLeaf.verticesLeaves.Add(vertexData);

							currentLeaf.leafCenter = currentLeaf.point - infoPool.ivyContainer.ivyGO.transform.position;
							currentLeaf.leafCenter = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) * currentLeaf.leafCenter;

							vertCount++;
						}
						//escribimos en el diccionario el index por donde nos hemos quedado  para después poder
						//transformar las uvs de cada elemento acorde a su dimensión real
						int[] fromTo = new int[2] { firstVertex, vertCount - 1 };
						branchesLeavesIndices.Add(branchesLeavesIndices.Count, fromTo);
					}



					//for (int p = 0; p < currentBranchPoint.leaves.Count; p++)
					//{
						
					//}
				}


				/*//Después por cada material, recorremos los puntos de la rama
				for (int j = 0; j < infoPool.ivyContainer.branches[b].branchPoints.Count; j++) {





					
				}*/
			}
		}

		public void BuildGeometry(){
			if (leavesDataInitialized) {
				//Lo primero inicializar
				Initialize ();
				//Estos contadores nos servirán para saber por dónde vamos calculándo vértices y triángulos, ya q lo calcularemos todo a buco, sin ir por ramas
				int vertCount = 0;
				int triBranchesCount = 0;

				//Recorremos cada rama y definimos el primer vértice que tenemos que escribir del array, recogido del vertcount actualizado en la iteración anterior
				for (int b = 0; b < infoPool.ivyContainer.branches.Count; b++) {
					int firstVertex = vertCount;
					Random.InitState (b + infoPool.ivyParameters.randomSeed);
					if (infoPool.ivyContainer.branches [b].branchPoints.Count > 1) {
						//En este contador guardaremos cuántos vértices tiene la rama actual, para en la siguiente tenerlo en cuenta y saber qué vértices hay que 
						//escribir
						int lastVertCount = 0;
						//Recorremos cada punto de la rama hasta el penúltimo
						for (int p = 0; p < infoPool.ivyContainer.branches [b].branchPoints.Count; p++) {
							//Si no es el último punto, calculamos el ring de vértices


							BranchPoint branchPoint = infoPool.ivyContainer.branches[b].branchPoints[p];
							branchPoint.verticesLoop = new List<RTVertexData>();




							Vector3 centerVertexPosition = (branchPoint.point - infoPool.ivyContainer.ivyGO.transform.position);
							centerVertexPosition = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) * centerVertexPosition;
							float radius = CalculateRadius(branchPoint.length, infoPool.ivyContainer.branches[b].totalLenght);

							branchPoint.radius = radius;

							if (p != infoPool.ivyContainer.branches [b].branchPoints.Count - 1) {
								//En este array, el método nos mete en el index 0 el firstvector, y en el index 1 el axis de rotación del ring
								Vector3[] vectors = CalculateVectors(infoPool.ivyContainer.branches [b].branchPoints [p].point, p, b);

								branchPoint.firstVector = vectors[0];
								branchPoint.axis = vectors[1];


								for (int v = 0; v < infoPool.ivyParameters.sides + 1; v++) {

									if (infoPool.ivyParameters.generateBranches)
									{
										//BranchPoint branchPoint = infoPool.ivyContainer.branches[b].branchPoints[p];
										
										float tipInfluence = GetTipInfluence(branchPoint.length, infoPool.ivyContainer.branches[b].totalLenght);
										infoPool.ivyContainer.branches[b].branchPoints[p].radius = radius;

										Quaternion quat = Quaternion.AngleAxis(angle * v, vectors[1]);
										Vector3 direction = quat * vectors[0];
										//Excepción para el cálculo de normales si tenemos el caso de media geometría y 1 lado
										if (infoPool.ivyParameters.halfgeom && infoPool.ivyParameters.sides == 1)
										{
											normals[vertCount] = -infoPool.ivyContainer.branches[b].branchPoints[p].grabVector;
										}
										else
										{
											normals[vertCount] = direction;
										}

										Vector3 vertexForRuntime = (direction * radius) + centerVertexPosition;

										verts[vertCount] = (direction * radius * tipInfluence) + infoPool.ivyContainer.branches[b].branchPoints[p].point;
										verts[vertCount] -= infoPool.ivyContainer.ivyGO.transform.position;
										verts[vertCount] = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) * verts[vertCount];


										uvs[vertCount] = 
											new Vector2(branchPoint.length * infoPool.ivyParameters.uvScale.y + infoPool.ivyParameters.uvOffset.y - infoPool.ivyParameters.stepSize, 
											1f / infoPool.ivyParameters.sides * v * infoPool.ivyParameters.uvScale.x + infoPool.ivyParameters.uvOffset.x);

										normals[vertCount] = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) * normals[vertCount];
										
										




										RTVertexData vertexData = new RTVertexData(vertexForRuntime, normals[vertCount], uvs[vertCount], Vector2.zero, vColor[vertCount]);
										branchPoint.verticesLoop.Add(vertexData);



										//Vamos actualizando estos contadores para en la siguiente pasada saber por dónde íbamos escribiendo en el array
										vertCount++;
										lastVertCount++;
									}
								}
							}
							//Si es el último punto, en lugar de calcular el ring, usamos el último punto para escribir el último vértice de esta rama
							else {

								if (infoPool.ivyParameters.generateBranches)
								{
									verts[vertCount] = (infoPool.ivyContainer.branches[b].branchPoints[p].point);
									//Corrección de espacio local
									verts[vertCount] -= infoPool.ivyContainer.ivyGO.transform.position;
									verts[vertCount] = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) * verts[vertCount];
									//verts[vertCount] = centerVertexPosition;


									//Excepción para las normales en el caso de media geometría y 1 solo lado
									if (infoPool.ivyParameters.halfgeom && infoPool.ivyParameters.sides == 1)
									{
										normals[vertCount] = -infoPool.ivyContainer.branches[b].branchPoints[p].grabVector;
									}
									else
									{
										normals[vertCount] = Vector3.Normalize(infoPool.ivyContainer.branches[b].branchPoints[p].point - infoPool.ivyContainer.branches[b].branchPoints[p - 1].point);
									}
									uvs[vertCount] = new Vector2(infoPool.ivyContainer.branches[b].totalLenght * infoPool.ivyParameters.uvScale.y + infoPool.ivyParameters.uvOffset.y, 0.5f * infoPool.ivyParameters.uvScale.x + infoPool.ivyParameters.uvOffset.x);

									normals[vertCount] = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) * normals[vertCount];



									Vector3 vertexForRuntime = centerVertexPosition;


									RTVertexData vertexData = new RTVertexData(vertexForRuntime, normals[vertCount], uvs[vertCount], Vector2.zero, vColor[vertCount]);
									branchPoint.verticesLoop.Add(vertexData);



									//Vamos actualizando estos contadores para en la siguiente pasada saber por dónde íbamos escribiendo en el array
									vertCount++;
									lastVertCount++;










									//Y después de poner el último vértice, triangulamos
									TriangulateBranch(b, ref triBranchesCount, vertCount, lastVertCount);
								}
							}
						}
					}
					//escribimos en el diccionario el index por donde nos hemos quedado  para después poder
					//transformar las uvs de cada elemento acorde a su dimensión real
					int[] fromTo = new int[2] { firstVertex, vertCount - 1 };
					branchesLeavesIndices.Add (branchesLeavesIndices.Count, fromTo);


					if (infoPool.ivyParameters.generateLeaves)
					{
						//infoPool.ivyContainer.branches[b].ClearRuntimeVerticesLeaves();
						BuildLeaves(b, ref vertCount);
					}
				}

				//Y pasamos los vértices y tris a la malla
				ivyMesh.vertices = verts;
				ivyMesh.normals = normals;
				ivyMesh.uv = uvs;
                ivyMesh.colors = vColor;
                ivyMesh.SetTriangles (trisBranches, 0);
				//Por cada material, metemos los triángulos de hojas al submesh correspondiente
				for (int i = 0; i < leavesMaterials.Count; i++) {
					ivyMesh.SetTriangles (trisLeaves [i], i + 1);
				}
				ivyMesh.RecalculateTangents ();
				ivyMesh.RecalculateBounds ();
			}
		}



		//Con esto se calculan vectores para el cálculo de cada ring
		Vector3[] CalculateVectors(Vector3 branchPoint, int p, int b){
				//Declaramos el firstvector del ring, el eje sobre el que vamos a rotar, la rotación de cada vértice
				Vector3 firstVector;
				Vector3 axis;
				//para el primer punto de la primera rama definimos las variables
				if (b == 0 && p == 0) {
					axis = infoPool.ivyContainer.ivyGO.transform.up;
					//Excepción para media geometría, para que el arco salga bien alineado respecto al suelo
					if (!infoPool.ivyParameters.halfgeom) {
						firstVector = infoPool.ivyContainer.firstVertexVector;
					}
					else {
						firstVector = Quaternion.AngleAxis (90f, axis) * infoPool.ivyContainer.firstVertexVector;
					}
				}
				//para todo lo demas, tendremos como eje una interpolación del segmento anterior y el siguiente al punto en cuestión, y como firstvector una proyección del grabvector sobre el plano del eje
				else {
					if (p == 0) {
						axis = infoPool.ivyContainer.branches [b].branchPoints [p + 1].point - infoPool.ivyContainer.branches [b].branchPoints [p].point;
					}
					else {
						axis = Vector3.Normalize (Vector3.Lerp (infoPool.ivyContainer.branches [b].branchPoints [p].point - infoPool.ivyContainer.branches [b].branchPoints [p - 1].point, infoPool.ivyContainer.branches [b].branchPoints [p + 1].point - infoPool.ivyContainer.branches [b].branchPoints [p].point, 0.5f));
					}
					if (!infoPool.ivyParameters.halfgeom) {
						firstVector = Vector3.Normalize (Vector3.ProjectOnPlane (infoPool.ivyContainer.branches [b].branchPoints[p].grabVector, axis));
					}
					else {
						firstVector = Quaternion.AngleAxis(90f, axis) * Vector3.Normalize (Vector3.ProjectOnPlane (infoPool.ivyContainer.branches [b].branchPoints[p].grabVector, axis));
					}
				}

			//retornamos los vectores que hemos calculado

			return new Vector3[2]{firstVector, axis};
		}

		//Cálculo del  radio según la distancia recorrida por la rama en ese punto, no es complejo, paso de explicarlo
		float CalculateRadius (float lenght, float totalLenght){
			float value = (Mathf.Sin (lenght * infoPool.ivyParameters.radiusVarFreq + infoPool.ivyParameters.radiusVarOffset) + 1f) / 2f;
			float radius = Mathf.Lerp (infoPool.ivyParameters.minRadius, infoPool.ivyParameters.maxRadius, value);

			//No recuerdo aquí por qué puse el -0.1f este :S
			/*if (lenght - 0.1f >= totalLenght - infoPool.ivyParameters.tipInfluence) {
				radius *= Mathf.InverseLerp (totalLenght, totalLenght - infoPool.ivyParameters.tipInfluence, lenght - 0.1f);
			}*/
			return radius;
		}

		private float GetTipInfluence(float lenght, float totalLenght)
		{
			float res = 1.0f;

			if (lenght - 0.1f >= totalLenght - infoPool.ivyParameters.tipInfluence)
			{
				res = Mathf.InverseLerp(totalLenght, totalLenght - infoPool.ivyParameters.tipInfluence, lenght - 0.1f);
			}

			return res;
		}

		//Algoritmo de triangulación, usamos el número de puntos que tiene la rama, el contador de triángulos global, contador de vértices global, y el número de vértices que tenía la última rama.
		void TriangulateBranch(int b, ref int triCount, int vertCount, int lastVertCount){
			//Hacemos una ronda por cada punto de la rama hasta el penúltimo
			for (int round = 0; round < infoPool.ivyContainer.branches [b].branchPoints.Count - 2; round++) {
				//Y por cada ronda hacemos una pasada a cada lado de la rama
				for (int i = 0; i < infoPool.ivyParameters.sides; i++) {
					//Y vamos asignando índices a cada hueco del array de tris con el algoritmo. Para escribir en los huecos correctos sumamos el total de vértices que hay y 
					//restamos los de la última ráma, así empezamos en el lugar correcto
					trisBranches [triCount] = (i + (round * (infoPool.ivyParameters.sides + 1))) + vertCount - lastVertCount;
					trisBranches [triCount + 1] = (i + (round * (infoPool.ivyParameters.sides + 1)) + 1) + vertCount - lastVertCount;
					trisBranches [triCount + 2] = (i + (round * (infoPool.ivyParameters.sides + 1)) + infoPool.ivyParameters.sides + 1) + vertCount - lastVertCount;

					trisBranches [triCount + 3] = (i + (round * (infoPool.ivyParameters.sides + 1)) + 1) + vertCount - lastVertCount;
					trisBranches [triCount + 4] = (i + (round * (infoPool.ivyParameters.sides + 1)) + infoPool.ivyParameters.sides + 2) + vertCount - lastVertCount;
					trisBranches [triCount + 5] = (i + (round * (infoPool.ivyParameters.sides + 1)) + infoPool.ivyParameters.sides + 1) + vertCount - lastVertCount;
					triCount += 6;

				}
			}
			//Aquí vienen los triángulos del capuchón
			for (int  t = 0, c = 0; t < infoPool.ivyParameters.sides * 3; t +=3, c ++){
				trisBranches[triCount] = vertCount - 1;
				trisBranches[triCount + 1] = vertCount - 3 - c;
				trisBranches[triCount + 2] = vertCount - 2 - c;
				triCount += 3;
			}
		}

#if UNITY_EDITOR
        //Método para generar uvs de lightmap
        public void GenerateLMUVs(){
            if (ivyMesh)
            {
                Unwrapping.GenerateSecondaryUVSet(ivyMesh);
            }
            //Este es el algoritmo custom de empaquetado de UVs de lightmap. Tiene dos fallos principales. El primero es que las ramas al ser muy largas en seguida ocupan todo el ancho del espacio UV, por lo que tenemos que 
            //cortar las ramas cada X distancia a nivel de UVs para tener islas de tamaños mas sensatos, habría que guardar el punto por el que hacemos split y redundar la info de ese ring en el último de ese trozo
            //y el primero del siguiente. Otra movida es que no se tiene en cuenta el tamaño real de las hojas, se consideran cuadradas y con la misma altura de la 
            //cirfunferencia de la rama. Para este problema habría que guardar en alguna parte la escala aplicada a esa hoja en particular y multiplicarlo por el tamaño de la malla.
            //
            //			//primero se borra lo que hubiera hecho en el diccionario
            //			infoPool.ivyParameters.UV2IslesSizes.Clear ();
            //			//Recorremos las ramas guardando en el diccionario el index y el tamaño, así como las hojas. en este punto están a buco, sin ordenar de ninguna manera.
            //			for (int b = 0; b < infoPool.ivyContainer.branches.Count; b++) {
            //				infoPool.ivyParameters.UV2IslesSizes.Add(infoPool.ivyParameters.UV2IslesSizes.Count, new Vector2 (infoPool.ivyContainer.branches[b].totalLenght, 2f * Mathf.PI *  infoPool.ivyParameters.maxRadius));

            //				for(int j = 0; j < infoPool.ivyContainer.branches[b].leaves.Count; j++)
            //				{
            //					infoPool.ivyParameters.UV2IslesSizes.Add(infoPool.ivyParameters.UV2IslesSizes.Count, new Vector2(2f * Mathf.PI * infoPool.ivyParameters.maxRadius, 2f * Mathf.PI * infoPool.ivyParameters.maxRadius));
            //				}
            //			}
            //			//Creamos diccionario temporal para la ordenacion por tamaño, y guardamos en el key del diccionario la posición en la ordenación original
            //			var dictionary = infoPool.ivyParameters.UV2IslesSizes;
            //			var items = from pair in dictionary
            //				orderby pair.Value.magnitude descending
            //				select pair;
            //			Dictionary<int, Vector2> UV2IslesSizesSorted = new Dictionary<int, Vector2>();
            //			foreach (KeyValuePair<int, Vector2> pair in items)
            //			{
            //				//Aquí tenemos los vectores ordenados de mayor a menor según tamaño, conservando el index original
            //				UV2IslesSizesSorted.Add (pair.Key, pair.Value);
            //			}
            //			//En este diccionario iremos metiendo los rects transformados, derivados del diccionario UV2IslesSizesSorted
            //			Dictionary<int, Rect> rects = new Dictionary<int, Rect> ();

            //			//Variables para el algoritmo de empaquetado
            //			float padding = infoPool.ivyParameters.LMUVPadding;
            //			float maxX = 0;
            //			float maxY = 0;
            //			int[] keys = UV2IslesSizesSorted.Keys.ToArray ();
            //			int lastCheckedBackward = keys.Length - 1;
            //			int lastCheckedForward = 1;
            //			Rect lastRect = new Rect ();

            //			//Este for sería el algoritmo de empaquetado, vamos recorriendo cada una de las keys, y por cada una cogemos las dimensiones ordenadas de mayor a menor y las colocamos en el espacio UV como rects
            //			//El funcionamiento es muy sencillo, coloca el primero y a partir de ahí va intentando colocar el siguiente al lado si es que cabe, si no cabe, intenta meter el último elemento, y así hasta que termina.
            //			//Al final tenemos un diccionario de rects y el index original donde está ese elemento en la ivy (branch y leave point) cabe destacar que las keys son consecutivas en el mismo sentido que son creados los elementos
            //			//por ejemplo, rama0, rama0hoja0, rama0hoja1, rama0hoja2, rama1, rama1hoja0, rama1hoja1 y así. Es la forma de saber qué rect corresponde a qué elemento después.
            //			for (int i = 0; i < keys.Length; i++) {
            //				if (keys [i] == 0) {
            //					lastRect = new Rect (Vector2.zero, new Vector2 (UV2IslesSizesSorted [keys [i]].x, UV2IslesSizesSorted [keys [i]].y));
            //					rects.Add (keys [i], lastRect);
            //					maxX = UV2IslesSizesSorted [keys [i]].x;
            //					maxY = UV2IslesSizesSorted [keys [i]].y;
            //					continue;
            //				} 
            //				else {
            //					if (lastRect.x + lastRect.width + UV2IslesSizesSorted [keys [lastCheckedBackward]].x <= maxX) {
            //						lastRect = new Rect (lastRect.x + lastRect.width, lastRect.y, UV2IslesSizesSorted [keys [lastCheckedBackward]].x, UV2IslesSizesSorted [keys [lastCheckedBackward]].y);
            //						rects.Add (keys [lastCheckedBackward], lastRect);
            //						lastCheckedBackward--;
            //					} 
            //					else {
            //						if (lastRect.x + lastRect.width + UV2IslesSizesSorted [keys [lastCheckedForward]].x <= maxX) {
            //							lastRect = new Rect (lastRect.x + lastRect.width, lastRect.y, UV2IslesSizesSorted [keys [lastCheckedForward]].x, UV2IslesSizesSorted [keys [lastCheckedForward]].y);
            //							rects.Add (keys[lastCheckedForward], lastRect);
            //							lastCheckedForward++;
            //						}
            //						else {
            //							lastRect = new Rect (0f, maxY, UV2IslesSizesSorted [keys [lastCheckedForward]].x, UV2IslesSizesSorted [keys [lastCheckedForward]].y);
            //							rects.Add (keys[lastCheckedForward], lastRect);
            //							maxY += UV2IslesSizesSorted [keys [lastCheckedForward]].y;
            //							lastCheckedForward++;
            //						}
            //					}
            //				}
            //			}

            //            //Padding
            //            for (int i = 0; i < keys.Length; i++)
            //            {
            //                rects[keys[i]] = new Rect(rects[keys[i]].x + padding, rects[keys[i]].y + padding, rects[keys[i]].width - 2 * padding, rects[keys[i]].height - 2 * padding);
            //            }

            //            //Esta es la variable de debug, de donde tira después la window para dibujar los rects
            //            uv2Rects = rects.Values.ToArray();

            //			//Creamos el array para uvs2
            //			uv2 = new Vector2[ivyMesh.vertexCount];
            //			//Y con este for  transformamos las uvs de cada uv para colocarla en su lugar correspondiente según el rect asociado al elemento al que pertenece la uv. Esto es bastante lioso, en realidad no lo entiendo mucho, espero no tener que volver a tocarlo xD
            //			for (int i = 0; i < keys.Length; i ++){
            ////				Debug.Log ("Vertices " + branchesLeavesIndices [keys [i]][0] + "-" + branchesLeavesIndices [keys [i]][1]  + " multiplied by " + rects[keys [i]] + " belonging to " + keys[i]);
            //				float maxU = 0f;
            //				float maxV = 0f;
            //				for (int u = branchesLeavesIndices [keys [i]][0]; u <= branchesLeavesIndices [keys [i]][1]; u++) {
            //					if (uvs [u].x > maxU) {
            //						maxU = uvs [u].x;
            //					}
            //					if (uvs [u].y > maxV) {
            //						maxV = uvs [u].y;
            //					}
            //				}

            //				for(int v = branchesLeavesIndices [keys [i]][0]; v <= branchesLeavesIndices [keys [i]][1]; v++){
            //					uv2 [v] = new Vector2 (uvs [v].x / maxU, uvs [v].y / maxV);
            //					uv2 [v] = new Vector2 (uv2 [v].x * rects [keys [i]].width + rects[keys[i]].x, uv2 [v].y * rects [keys [i]].height + rects[keys[i]].y);
            //				}
            //			}
            //			ivyMesh.uv2 = uv2;
        }
#endif
    }
}