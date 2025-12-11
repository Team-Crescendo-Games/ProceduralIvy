using System.Collections.Generic;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class RTBakedMeshBuilder
	{
		public IvyParameters ivyParameters;
		public RTIvyContainer rtIvyContainer;
		public RTIvyContainer rtBakedIvyContainer;
		public GameObject ivyGO;

		//La malla final de la enredadera en su conjunto
		public MeshRenderer meshRenderer;
		public MeshFilter meshFilter;


		private bool onOptimizedStretch;
		//private float lengthFactor;

		private MeshFilter leavesMeshFilter;
		private MeshRenderer leavesMeshRenderer;
		private MeshRenderer mrProcessedMesh;

		private Mesh processedMesh;

		private Mesh ivyMesh;
		public List<RTBranchContainer> activeBranches;


		public RTMeshData bakedMeshData;
		public RTMeshData buildingMeshData;
		public RTMeshData processedMeshData;


		public List<List<int>> processedVerticesIndicesPerBranch;
		public List<List<int>> processedBranchesVerticesIndicesPerBranch;

		private int[] vertCountsPerBranch;
		private int[] lastTriangleIndexPerBranch;
		private int[] vertCountLeavesPerBranch;


		private int lastPointCopied;

		private int vertCount;
		private int lastVertCount;
		private int triCount;
		private int lastVerticesCountProcessed;
		private int lastLeafVertProcessed;

		private int submeshCount;
		private int[] submeshByChoseLeaf;



		int initIdxLeaves;
		int endIdxLeaves;
		private int backtrackingPoints;

		private int[] fromTo;
		private Vector3[] vectors;

		private RTMeshData[] leavesMeshesByChosenLeaf;
		//public Dictionary<int, RTMeshData> leavesMeshesDict;
		private int lastVertexIndex;


		//ángulo para la generación de cada ring
		float angle;

		//lista de materiales de las hojas
		public List<Material> leavesMaterials;
		//Aquí metemos qué tipos de hojas corresponden a cada material
		public List<List<int>> typesByMat;

		//Esto es solo para debugear, para hacer una representación 
		public Rect[] uv2Rects = new Rect[0];
		//Booleano para saber si están inicializadas las estructuras para las hojas, así no intenta construir la geometría sin tener lo necesario
		public bool leavesDataInitialized = false;





		//CONFIG
		private float growthSpeed;
		private float leafLengthCorrrectionFactor;


		//CACHED VARIABLES
		private Vector3 ivyGoPosition;
		private Quaternion ivyGoRotation;
		private Quaternion ivyGoInverseRotation;
		private Vector3 zeroVector3;
		private Vector2 zeroVector2;
		private Color blackColor;



		public void InitializeMeshBuilder(IvyParameters ivyParameters, RTIvyContainer ivyContainer, 
			RTIvyContainer bakedIvyContainer, GameObject ivyGO, Mesh bakedMesh, MeshRenderer meshRenderer, 
			MeshFilter meshFilter, int numBranches, Mesh processedMesh, float growSpeed, MeshRenderer mrProcessedMesh,
			int backtrackingPoints, int[] submeshByChoseLeaf, RTMeshData[] leavesMeshesByChosenLeaf, Material[] materials)
		{
			this.ivyParameters = ivyParameters;
			this.rtIvyContainer = ivyContainer;
			this.rtBakedIvyContainer = bakedIvyContainer;
			this.ivyGO = ivyGO;

			this.meshRenderer = meshRenderer;
			this.meshFilter = meshFilter;

			this.processedMesh = processedMesh;
			this.processedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;

			this.mrProcessedMesh = mrProcessedMesh;
			this.submeshByChoseLeaf = submeshByChoseLeaf;
			this.leavesMeshesByChosenLeaf = leavesMeshesByChosenLeaf;

			activeBranches = new List<RTBranchContainer>();

			this.fromTo = new int[2];
			this.vectors = new Vector3[2];

			this.growthSpeed = growSpeed;
			this.backtrackingPoints = backtrackingPoints;

			this.submeshCount = meshRenderer.sharedMaterials.Length;

			vertCountsPerBranch = new int[numBranches];
			lastTriangleIndexPerBranch = new int[numBranches];
			vertCountLeavesPerBranch = new int[numBranches];
			processedVerticesIndicesPerBranch = new List<List<int>>(numBranches);
			processedBranchesVerticesIndicesPerBranch = new List<List<int>>(numBranches);

			for (int i = 0; i < numBranches; i++)
			{
				processedVerticesIndicesPerBranch.Add(new List<int>());
				processedBranchesVerticesIndicesPerBranch.Add(new List<int>());
			}


			this.vertCount = 0;

			ivyMesh = new Mesh();
			ivyMesh.subMeshCount = submeshCount;
			ivyMesh.name = Constants.IVY_MESH_NAME;

			meshFilter.mesh = ivyMesh;


			List<Material> filteredMaterials = new List<Material>();
			filteredMaterials.Add(materials[0]); //Bark material

			if (ivyParameters.generateLeaves)
			{
				for(int i = 1; i < materials.Length; i++)
				{
					filteredMaterials.Add(materials[i]);
				}
			}

			Material[] filteredMaterialArray = filteredMaterials.ToArray();


			ivyGO.GetComponent<MeshRenderer>().sharedMaterials = filteredMaterialArray;
			mrProcessedMesh.sharedMaterials = filteredMaterialArray;




			leavesDataInitialized = true;

			


			ivyGoPosition = ivyGO.transform.position;
			ivyGoRotation = ivyGO.transform.rotation;
			ivyGoInverseRotation = Quaternion.Inverse(ivyGO.transform.rotation);
			zeroVector3 = Vector3.zero;
			zeroVector2 = Vector2.zero;
			blackColor = Color.black;
		}

		public void InitializeMeshesDataBaked(Mesh bakedMesh, int numBranches)
		{
			CreateBuildingMeshData(bakedMesh, numBranches);
			CreateBakedMeshData(bakedMesh);
			CreateProcessedMeshData(bakedMesh);

			bakedMesh.Clear();
		}

		public void InitializeMeshesDataProcedural(Mesh bakedMesh, int numBranches, float lifetime, float velocity)
		{
			CreateBuildingMeshData(bakedMesh, numBranches);
			CreateBakedMeshData(bakedMesh);
			CreateProcessedMeshDataProcedural(bakedMesh, lifetime, velocity);

			bakedMesh.Clear();
		}

		public void CreateBuildingMeshData(Mesh bakedMesh, int numBranches)
		{
			int numVerticesPerLoop = ivyParameters.sides + 1;
			int numVertices = (backtrackingPoints * numVerticesPerLoop) + (backtrackingPoints * 2 * 8);
			numVertices *= numBranches;

			int subMeshCount = bakedMesh.subMeshCount;
			List<int> numTrianglesPerSubmesh = new List<int>();

			int branchTrianglesNumber = ((backtrackingPoints - 2) * (ivyParameters.sides * 6)) + (ivyParameters.sides * 3);
			branchTrianglesNumber *= numBranches;

			numTrianglesPerSubmesh.Add(branchTrianglesNumber);

			for (int i = 1; i < subMeshCount; i++)
			{
				int numTriangles = backtrackingPoints * 6 * numBranches;
				numTrianglesPerSubmesh.Add(numTriangles);
			}


			this.buildingMeshData = new RTMeshData(numVertices, subMeshCount, numTrianglesPerSubmesh);
		}

		public void CreateBakedMeshData(Mesh bakedMesh)
		{
			this.bakedMeshData = new RTMeshData(bakedMesh);
		}

		public void CreateProcessedMeshDataProcedural(Mesh bakedMesh, float lifetime, float velocity)
		{
			float ratio = lifetime / velocity;

			int numPoints = Mathf.CeilToInt(ratio * 200);


			int numVertices = numPoints * (ivyParameters.sides + 1);
			int submeshCount = bakedMesh.subMeshCount;

			List<int> numTrianglesPerSubmesh = new List<int>();
			for (int i = 0; i < submeshCount; i++)
			{
				int numTriangles = ivyParameters.sides * numPoints * 9;
				numTrianglesPerSubmesh.Add(numTriangles);
			}

			this.processedMeshData = new RTMeshData(numVertices, submeshCount, numTrianglesPerSubmesh);
		}

		public void CreateProcessedMeshData(Mesh bakedMesh)
		{
			int numVertices = bakedMesh.vertexCount;
			int submeshCount = bakedMesh.subMeshCount;

			List<int> numTrianglesPerSubmesh = new List<int>();
			for (int i = 0; i < submeshCount; i++)
			{
				int numTriangles = bakedMesh.GetTriangles(i).Length;
				numTrianglesPerSubmesh.Add(numTriangles);
			}

			this.processedMeshData = new RTMeshData(numVertices, submeshCount, numTrianglesPerSubmesh);
		}

		public void SetLeafLengthCorrectionFactor(float leafLengthCorrrectionFactor)
		{
			this.leafLengthCorrrectionFactor = leafLengthCorrrectionFactor;
		}

		public void ClearMesh()
		{
			ivyMesh.Clear();
		}

		public RTBakedMeshBuilder()
		{}

		public RTBakedMeshBuilder(RTIvyContainer ivyContainer, GameObject ivyGo)
		{
			this.rtIvyContainer = ivyContainer;
			this.ivyGO = ivyGo;
		}

		private void ClearTipMesh()
		{
			buildingMeshData.Clear();
			for (int i = 0; i < vertCountsPerBranch.Length; i++)
			{
				vertCountsPerBranch[i] = 0;
				lastTriangleIndexPerBranch[i] = 0;
				vertCountLeavesPerBranch[i] = 0;
			}
			vertCount = 0;
			triCount = 0;
		}


		public void CheckCopyMesh(int branchIndex, List<RTBranchContainer> bakedBranches)
		{
			RTBranchContainer buildingBranch = rtIvyContainer.branches[branchIndex];
			RTBranchContainer bakedBranch = bakedBranches[branchIndex];

			int initSegment = buildingBranch.branchPoints.Count - backtrackingPoints - 1;
			initSegment = Mathf.Clamp(initSegment, 0, int.MaxValue);

			int initSegmentIdx = initSegment;
			int endSegmentIndx = initSegment + 1;

			CopyToFixedMesh(branchIndex, initSegmentIdx, endSegmentIndx, buildingBranch, bakedBranch);
		}

		public void BuildGeometry02(List<RTBranchContainer> activeBakedBranches, 
			List<RTBranchContainer> activeBuildingBranches)
		{
			if (!ivyParameters.halfgeom)
			{
				angle = Mathf.Rad2Deg * 2 * Mathf.PI / ivyParameters.sides;
			}
			else
			{
				angle = Mathf.Rad2Deg * 2 * Mathf.PI / ivyParameters.sides / 2;
			}


			if (leavesDataInitialized)
			{
				ClearTipMesh();

				//Recorremos cada rama y definimos el primer vértice que tenemos que escribir del array, recogido del vertcount actualizado en la iteración anterior
				for (int b = 0; b < rtIvyContainer.branches.Count; b++)
				{
					int firstVertex = vertCount;


					RTBranchContainer currentBranch = activeBuildingBranches[b];

					if (currentBranch.branchPoints.Count > 1)
					{
						lastVertCount = 0;

						//Recorremos cada punto de la rama hasta el penúltimo
						int initIndexPoint = currentBranch.branchPoints.Count - backtrackingPoints;
						initIndexPoint = Mathf.Clamp(initIndexPoint, 0, int.MaxValue);

						int endIndexPoint = currentBranch.branchPoints.Count;



						for (int p = initIndexPoint; p < endIndexPoint; p++)
						{

							RTBranchPoint currentBranchPoint = currentBranch.branchPoints[p];

							Vector3 centerLoop = ivyGO.transform.InverseTransformPoint(currentBranchPoint.point);

							Vector3 vertex = zeroVector3;
							Vector3 normal = zeroVector3;
							Vector2 uv = zeroVector2;
							Vector2 uv2 = zeroVector2;
							Color vertexColor = blackColor;


							float tipInfluenceFactor = Mathf.InverseLerp(currentBranch.totalLength,
							currentBranch.totalLength - ivyParameters.tipInfluence,
							currentBranchPoint.length);




							if (p < currentBranch.branchPoints.Count - 1)
							{
								for (int i = 0; i < currentBranchPoint.verticesLoop.Length; i++)
								{
									if (ivyParameters.generateBranches)
									{
										vertex = Vector3.LerpUnclamped(currentBranchPoint.centerLoop, currentBranchPoint.verticesLoop[i].vertex, tipInfluenceFactor);
										buildingMeshData.AddVertex(vertex, currentBranchPoint.verticesLoop[i].normal, currentBranchPoint.verticesLoop[i].uv,
											currentBranchPoint.verticesLoop[i].color);



										vertCountsPerBranch[b]++;
										vertCount++;
										lastVertCount++;
									}
								}
							}
							else
							{
								if (ivyParameters.generateBranches)
								{
									vertex = centerLoop;


									normal = Vector3.Normalize(currentBranchPoint.point - currentBranchPoint.GetPreviousPoint().point);
									normal = ivyGO.transform.InverseTransformVector(normal);

									uv = currentBranch.GetLastUV(ivyParameters);

									buildingMeshData.AddVertex(vertex, normal, uv, Color.black);

									vertCountsPerBranch[b]++;
									vertCount++;
									lastVertCount++;
								}
							}
						}

						if (ivyParameters.generateBranches)
						{
							SetTriangles(currentBranch, vertCount, initIndexPoint, b);
						}
					}

					fromTo[0] = firstVertex;
					fromTo[1] = vertCount - 1;



					if (ivyParameters.generateLeaves)
					{
						BuildLeaves(b, activeBuildingBranches[b], activeBakedBranches[b]);
					}

					
				}

				RefreshMesh();
			}
		}

		

		float CalculateRadius(BranchPoint branchPoint, BranchContainer buildingBranchContainer)
		{
			float tipInfluenceFactor = Mathf.InverseLerp(branchPoint.branchContainer.totalLenght,
			branchPoint.branchContainer.totalLenght - ivyParameters.tipInfluence, branchPoint.length - 0.1f);	

			branchPoint.currentRadius = branchPoint.radius * tipInfluenceFactor;
			float radius = branchPoint.currentRadius;

			return radius;
		}

		private void SetTriangles(RTBranchContainer branch, int vertCount, int initIndex, int branchIndex)
		{
			int initRound = 0;
			int endRound = Mathf.Min(branch.branchPoints.Count - 2, ((branch.branchPoints.Count - initIndex) - 2));


			for (int round = initRound; round < endRound; round++)
			{
				for (int i = 0; i < ivyParameters.sides; i++)
				{
					int offset = vertCount - lastVertCount;

					int v0 = (i + (round * (ivyParameters.sides + 1))) + offset;
					int v1 = (i + (round * (ivyParameters.sides + 1)) + 1) + offset;
					int v2 = (i + (round * (ivyParameters.sides + 1)) + ivyParameters.sides + 1) + offset;
					int v3 = (i + (round * (ivyParameters.sides + 1)) + 1) + offset;
					int v4 = (i + (round * (ivyParameters.sides + 1)) + ivyParameters.sides + 2) + offset;
					int v5 = (i + (round * (ivyParameters.sides + 1)) + ivyParameters.sides + 1) + offset;


					//int firstIndexTriangle = lastTriangleIndexPerBranch[branchIndex];

					

					buildingMeshData.AddTriangle(0, v0);
					buildingMeshData.AddTriangle(0, v1);
					buildingMeshData.AddTriangle(0, v2);


					buildingMeshData.AddTriangle(0, v3);
					buildingMeshData.AddTriangle(0, v4);
					buildingMeshData.AddTriangle(0, v5);

					triCount += 6;
				}
			}
			

			for (int t = 0, c = 0; t < ivyParameters.sides * 3; t += 3, c++)
			{
				buildingMeshData.AddTriangle(0, vertCount - 1);
				buildingMeshData.AddTriangle(0, vertCount - 3 - c);
				buildingMeshData.AddTriangle(0, vertCount - 2 - c);

				triCount += 3;
			}

			lastTriangleIndexPerBranch[branchIndex] = vertCount - 1;
		}


		private void BuildLeaves(int branchIndex, RTBranchContainer buildingBranchContainer, 
			RTBranchContainer bakedBranchContainer)
		{
			RTMeshData chosenLeaveMeshData;




			int firstPointIdx = buildingBranchContainer.branchPoints.Count - backtrackingPoints;
			firstPointIdx = Mathf.Clamp(firstPointIdx, 0, int.MaxValue);

			
			for(int i = firstPointIdx; i < buildingBranchContainer.branchPoints.Count; i++)
			{
				RTLeafPoint[] leaves = bakedBranchContainer.leavesOrderedByInitSegment[i];
				RTBranchPoint rtBranchPoint = buildingBranchContainer.branchPoints[i];

				for (int j = 0; j < leaves.Length; j++)
				{
					RTLeafPoint currentLeaf = leaves[j];


					if(currentLeaf == null) { continue; }


					float tipInfluenceFactor = Mathf.InverseLerp(buildingBranchContainer.totalLength,
						(buildingBranchContainer.totalLength - ivyParameters.tipInfluence),
						rtBranchPoint.length);

					chosenLeaveMeshData = leavesMeshesByChosenLeaf[currentLeaf.chosenLeave];



					//Metemos los triángulos correspondientes en el array correspondiente al material que estamos iterando
					for (int t = 0; t < chosenLeaveMeshData.triangles[0].Length; t++)
					{
						int triangleValue = chosenLeaveMeshData.triangles[0][t] + vertCount;

						int submesh = submeshByChoseLeaf[currentLeaf.chosenLeave];
						buildingMeshData.AddTriangle(submesh, triangleValue);
					}

					for (int v = 0; v < currentLeaf.vertices.Length; v++)
					{
						Vector3 vertex = Vector3.LerpUnclamped(currentLeaf.leafCenter, currentLeaf.vertices[v].vertex, tipInfluenceFactor);
						
						buildingMeshData.AddVertex(vertex, currentLeaf.vertices[v].normal, currentLeaf.vertices[v].uv, currentLeaf.vertices[v].color);

						vertCountLeavesPerBranch[branchIndex]++;
						vertCountsPerBranch[branchIndex]++;
						vertCount++;
					}
				}
			}
		}


		public void CopyToFixedMesh(int branchIndex, int initSegmentIdx,
			int endSegmentIdx, RTBranchContainer branchContainer, RTBranchContainer bakedBranchContainer)
		{

			int numVerticesPerLoop = ivyParameters.sides + 1;
			int numTrianglesPerLoop = ivyParameters.sides * 6;
			int numLoopsToProcess = 1;
			int onlyBranchVertices = (vertCountsPerBranch[branchIndex] - vertCountLeavesPerBranch[branchIndex]);


			int vertexOffset = 0;
			for (int i = 1; i <= branchIndex; i++)
			{
				vertexOffset += vertCountsPerBranch[branchIndex];
			}

			if (processedBranchesVerticesIndicesPerBranch[branchIndex].Count <= 0)
			{
				numLoopsToProcess = 2;
			}
			else
			{
				numLoopsToProcess = 1;
				vertexOffset += numVerticesPerLoop;
			}


			for(int i = numLoopsToProcess - 1; i >= 0; i--)
			{
				int index = branchContainer.branchPoints.Count - backtrackingPoints - i;
				
				RTBranchPoint rtBranchPoint = branchContainer.branchPoints[index];
				
				for(int j = 0; j < rtBranchPoint.verticesLoop.Length; j++)
				{
					RTVertexData vertexData = rtBranchPoint.verticesLoop[j];
					processedMeshData.AddVertex(vertexData.vertex, vertexData.normal, vertexData.uv, vertexData.color);

					processedBranchesVerticesIndicesPerBranch[branchIndex].Add(processedMeshData.VertexCount() - 1);
				}

				
			}


			int triangleIndexOffset = 0;
			if (branchIndex > 0)
			{
				triangleIndexOffset = lastTriangleIndexPerBranch[branchIndex];
			}

			if (processedBranchesVerticesIndicesPerBranch[branchIndex].Count >= numVerticesPerLoop * 2)
			{

				int initIdx = processedBranchesVerticesIndicesPerBranch[branchIndex].Count - (numVerticesPerLoop * 2);


				for (int i = 0; i < ivyParameters.sides; i++)
				{
					int v0 = processedBranchesVerticesIndicesPerBranch[branchIndex][i + initIdx];

					int v1 = processedBranchesVerticesIndicesPerBranch[branchIndex][i + 1 + initIdx];

					int v2 = processedBranchesVerticesIndicesPerBranch[branchIndex][i + ivyParameters.sides + 1 + initIdx];

					int v3 = processedBranchesVerticesIndicesPerBranch[branchIndex][i + 1 + initIdx];

					int v4 = processedBranchesVerticesIndicesPerBranch[branchIndex][i + ivyParameters.sides + 2 + initIdx];

					int v5 = processedBranchesVerticesIndicesPerBranch[branchIndex][i + ivyParameters.sides + 1 + initIdx];


					processedMeshData.AddTriangle(0, v0);
					processedMeshData.AddTriangle(0, v1);
					processedMeshData.AddTriangle(0, v2);

					processedMeshData.AddTriangle(0, v3);
					processedMeshData.AddTriangle(0, v4);
					processedMeshData.AddTriangle(0, v5);
				}
			}




			if (ivyParameters.generateLeaves)
			{
				int lastVertexLeafProcessed = processedMeshData.VertexCount();
				int numLeavesProcessed = 0;

				for (int i = initSegmentIdx; i < endSegmentIdx; i++)
				{
					RTLeafPoint[] leaves = bakedBranchContainer.leavesOrderedByInitSegment[i];
					for (int j = 0; j < leaves.Length; j++)
					{
						RTLeafPoint currentLeaf = leaves[j];

						if(currentLeaf == null)
						{
							continue;
						}

						RTMeshData chosenLeaveMeshData = leavesMeshesByChosenLeaf[currentLeaf.chosenLeave];

						int submesh = submeshByChoseLeaf[currentLeaf.chosenLeave];
						for (int t = 0; t < chosenLeaveMeshData.triangles[0].Length; t++)
						{
							int triangleValue = chosenLeaveMeshData.triangles[0][t] + lastVertexLeafProcessed;
							processedMeshData.AddTriangle(submesh, triangleValue);
						}

						for (int v = 0; v < currentLeaf.vertices.Length; v++)
						{
							RTVertexData vertexData = currentLeaf.vertices[v];
							processedMeshData.AddVertex(vertexData.vertex,
								vertexData.normal, vertexData.uv,
								vertexData.color);

							processedVerticesIndicesPerBranch[branchIndex].Add(processedMeshData.VertexCount() - 1);

							lastVertexLeafProcessed++;
						}
						numLeavesProcessed++;

					}
				}
			}
		}

		public void RefreshProcessedMesh()
		{
			processedMesh.MarkDynamic();

			processedMesh.subMeshCount = this.submeshCount;

			processedMesh.vertices = processedMeshData.vertices;
			processedMesh.normals = processedMeshData.normals;
			processedMesh.colors = processedMeshData.colors;
			processedMesh.uv = processedMeshData.uv;


			processedMesh.SetTriangles(processedMeshData.triangles[0], 0);

			if (ivyParameters.generateLeaves)
			{
				for (int i = 1; i < this.submeshCount; i++)
				{
					processedMesh.SetTriangles(processedMeshData.triangles[i], i);
				}
			}


			processedMesh.RecalculateBounds();
		}

		private void RefreshMesh()
		{
			ivyMesh.Clear();

			ivyMesh.subMeshCount = this.submeshCount;

			ivyMesh.MarkDynamic();


			ivyMesh.vertices = buildingMeshData.vertices;
			ivyMesh.normals = buildingMeshData.normals;
			ivyMesh.colors = buildingMeshData.colors;
			ivyMesh.uv = buildingMeshData.uv;


			ivyMesh.SetTriangles(buildingMeshData.triangles[0], 0);
			if (ivyParameters.generateLeaves)
			{
				for (int i = 1; i < submeshCount; i++)
				{
					ivyMesh.SetTriangles(buildingMeshData.triangles[i], i);
				}
			}

			ivyMesh.RecalculateBounds();
		}
	}
}