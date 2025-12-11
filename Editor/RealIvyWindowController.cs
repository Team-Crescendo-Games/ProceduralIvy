using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class RealIvyProWindowController : Editor
	{
		public static event Action OnIvyGoCreated;

		public IvyInfo currentIvyInfo;
		public InfoPool infoPool;
		public GameObject ivyGO;
		public MeshFilter mf;
		public MeshRenderer mr;

		public IvyPreset selectedPreset;
		public RealIvyWindow realIvyProWindow;
		//public IvyParameters currentParameters;
		//public Material currentVinesMaterial;

		private IvyParametersGUI ivyParametersGUI;

		public void Init(RealIvyWindow realIvyProWindow, IvyParametersGUI ivyParametersGUI)
		{
			Selection.selectionChanged += OnSelectionChanged;

			this.ivyParametersGUI = ivyParametersGUI;
			this.realIvyProWindow = realIvyProWindow;
		}

		public void Destroy()
		{
			Selection.selectionChanged -= OnSelectionChanged;
		}

		/*public InfoPool CreateNewIvy()
		{
			return CreateNewIvy(this.selectedPreset);
		}*/

		public InfoPool CreateNewIvy()
		{
			IvyParameters parameters = new IvyParameters();
			parameters.CopyFrom(ivyParametersGUI);
			return CreateNewIvy(parameters);
		}

		public InfoPool CreateNewIvy(IvyParameters ivyParameters)
		{
			infoPool = ScriptableObject.CreateInstance<InfoPool>();
			infoPool.ivyContainer = ScriptableObject.CreateInstance<IvyContainer>();
			infoPool.ivyContainer.branches = new List<BranchContainer>();

			infoPool.ivyParameters = ivyParameters;
			infoPool.ivyParameters.UV2IslesSizes = new Dictionary<int, Vector2>();

			infoPool.growth = ScriptableObject.CreateInstance<EditorIvyGrowth>();
			infoPool.growth.infoPool = infoPool;

			infoPool.meshBuilder = ScriptableObject.CreateInstance<EditorMeshBuilder>();
			infoPool.meshBuilder.infoPool = infoPool;
			infoPool.meshBuilder.ivyMesh = new Mesh();

			RealIvyWindow.realIvyProToolsWindow.infoPool = infoPool;
			
			return infoPool;
		}

		public InfoPool CreateNewIvy(IvyPreset selectedPreset)
		{


			this.selectedPreset = selectedPreset;

			IvyParameters parameters = new IvyParameters();
			parameters.CopyFrom(selectedPreset);

			return CreateNewIvy(parameters);
		}

		public void ModifyIvy(IvyInfo ivyInfo)
		{
			this.currentIvyInfo = ivyInfo;
			this.selectedPreset = ivyInfo.originalPreset;

			this.infoPool = ivyInfo.infoPool;
			this.infoPool.ivyContainer.ivyGO = ivyInfo.gameObject;
			
			this.mf = ivyInfo.meshFilter;
			this.mr = ivyInfo.meshRenderer;
			this.ivyGO = ivyInfo.gameObject;

			this.infoPool.growth.growing = false;

			this.infoPool.ivyParameters.branchesMaterial = this.mr.sharedMaterials[0];

			ivyParametersGUI.CopyFrom(infoPool.ivyParameters); 

			infoPool.meshBuilder.InitLeavesData();
		}

		public bool StartIvy(Vector3 firstPoint, Vector3 firstGrabVector)
		{
			if (infoPool.ivyContainer.branches.Count == 0)
			{
				infoPool.growth.Initialize(firstPoint, firstGrabVector);
				infoPool.meshBuilder.InitLeavesData();
				infoPool.meshBuilder.Initialize();
				return true;
			}
			else
			{
				return false;
			}
		}

		public void ResetIvy()
		{
			infoPool.ivyContainer.Clear();
		}

		public void GenerateLMUVs()
		{
			infoPool.meshBuilder.GenerateLMUVs();
		}

		public void RefreshMesh()
		{
			if (ivyGO)
			{
                infoPool.meshBuilder.InitLeavesData();
				infoPool.meshBuilder.BuildGeometry();

				Material[] newMaterials = mr.sharedMaterials;
				newMaterials[0] = infoPool.ivyParameters.branchesMaterial;
				mr.sharedMaterials = newMaterials;

				mf.mesh = infoPool.meshBuilder.ivyMesh;
			}
		}

		public void Update()
		{
			if (infoPool.growth.growing)
			{
				if (!IsVertexLimitReached())
				{
					infoPool.growth.Step();
					RefreshMesh();
				}
				else
				{
                    if (infoPool.ivyParameters.buffer32Bits)
                    {
                        Debug.LogWarning("Vertices limit reached at " + Constants.VERTEX_LIMIT_32 + ".", infoPool.ivyContainer.ivyGO);
                    }
                    else
                    {
                        Debug.LogWarning("Vertices limit reached at " + Constants.VERTEX_LIMIT_16 + ".", infoPool.ivyContainer.ivyGO);
                    }
					infoPool.growth.growing = false;
				}
			}
		}

		private bool IsVertexLimitReached()
		{
			int numVertices = infoPool.meshBuilder.verts.Length + infoPool.ivyParameters.sides + 1;
            int limit;
            if (infoPool.ivyParameters.buffer32Bits)
            {
                limit = Constants.VERTEX_LIMIT_32;
            }
            else
            {
                limit = Constants.VERTEX_LIMIT_16;
            }
			bool res = numVertices >= limit;
			return res;
		}

		public void SaveIvy()
		{
			Undo.IncrementCurrentGroup();
			infoPool.ivyContainer.RecordUndo();
		}

		public void RegisterUndo()
		{
            if (infoPool)
            {
                Undo.RegisterCompleteObjectUndo(infoPool, "Ivy Parameter Change");
            }
		}

		public void CreateIvyGO(Vector3 position, Vector3 normal)
		{
            ivyGO = new GameObject();
			ivyGO.transform.position = position + normal * infoPool.ivyParameters.minDistanceToSurface;
			ivyGO.transform.rotation = Quaternion.LookRotation(normal);
			ivyGO.transform.RotateAround(ivyGO.transform.position, ivyGO.transform.right, 90f);
			ivyGO.name = "New Ivy";
			//Selection.activeGameObject = ivyGO;

			infoPool.ivyContainer.ivyGO = ivyGO;
			infoPool.growth.origin = ivyGO.transform.position;

			mr = ivyGO.AddComponent<MeshRenderer>();
			Material[] materials = new Material[1];
			materials[0] = infoPool.ivyParameters.branchesMaterial;
			mr.sharedMaterials = materials;

			mf = ivyGO.AddComponent<MeshFilter>();

			


			IvyInfo ivyInfo = ivyGO.AddComponent<IvyInfo>();
			ivyInfo.Setup(infoPool, mf, mr, selectedPreset);

			

			ModifyIvy(ivyInfo);

			infoPool.ivyContainer.RecordCreated();

			if(OnIvyGoCreated != null)
			{
				OnIvyGoCreated();
			}
		}

		/*public void ApplyPresset(IvyParameters preset)
		{
			infoPool.ivyParameters = preset;
		}*/

		public void SaveCurrentIvyIntoScene()
		{
			RemoveAllScripts();
			OnSelectionChanged();
		}

		private GameObject RemoveAllScripts()
		{
			if (infoPool.ivyParameters.generateLightmapUVs)
			{
				infoPool.meshBuilder.GenerateLMUVs();
			}
			List<MonoBehaviour> componentsToDelete = new List<MonoBehaviour>();
			componentsToDelete.AddRange(ivyGO.GetComponentsInChildren<MonoBehaviour>());

			for (int i = 0; i < componentsToDelete.Count; i++)
			{
                DestroyImmediate(componentsToDelete[i]);
            }
            var go = GameObject.Instantiate(infoPool.ivyContainer.ivyGO);
            go.name = go.name.Remove(go.name.Length - 7, 7);
            DestroyImmediate(infoPool.ivyContainer.ivyGO);

            return go;
		}

        public void SaveCurrentIvyAsPrefab(string fileName)
        {
            string filePath = EditorUtility.SaveFilePanelInProject("Save Ivy as prefab", fileName, Constants.EXTENSION_PREFAB, "");
            fileName = Path.GetFileName(filePath);
            string[] separator = new string[] { "." };
            fileName = fileName.Split(separator, StringSplitOptions.None)[0];
            if (filePath.Length > 0)
            {
                int initIndexExtension = filePath.LastIndexOf(".");
                int initIndexFileName = filePath.LastIndexOf("/");
                string localFolderPath = filePath.Substring(0, initIndexFileName);

                string infoPoolFilePath = Path.Combine(localFolderPath, fileName + ".asset");
                AssetDatabase.CreateAsset(infoPool, infoPoolFilePath);
                AssetDatabase.AddObjectToAsset(infoPool.ivyContainer, infoPool);
                AssetDatabase.AddObjectToAsset(infoPool.meshBuilder, infoPool);
                //AssetDatabase.AddObjectToAsset(infoPool.ivyParameters, infoPool);
                AssetDatabase.AddObjectToAsset(infoPool.growth, infoPool);
                AssetDatabase.AddObjectToAsset(infoPool.meshBuilder.ivyMesh, infoPool);

                for (int i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                {
                    AssetDatabase.AddObjectToAsset(infoPool.ivyContainer.branches[i], infoPool);
                }

				var go = RemoveAllScripts();

				GameObject prefabSaved = PrefabUtility.SaveAsPrefabAssetAndConnect(go, filePath, InteractionMode.AutomatedAction);

                EditorGUIUtility.PingObject(prefabSaved);
            }
		}

		private void OnSelectionChanged()
		{
			GameObject activeGameObject = Selection.activeGameObject;

			if (activeGameObject != null)
			{
				IvyInfo ivyInfo = activeGameObject.GetComponent<IvyInfo>();

				if (ivyInfo != null)
				{
					ModifyIvy(ivyInfo);
				}
				else
				{
					currentIvyInfo = null;
				}
			}
			else
			{
				if(ivyGO == null)
				{
					infoPool.ivyContainer.Clear();
					CreateNewIvy();
				}

				currentIvyInfo = null;
			}
		}

		public void OnVinesMaterialChanged(Material newMaterial)
		{
			realIvyProWindow.valueUpdated = true;
		}

		public void OnPresetChanged(IvyPreset newPreset)
		{
            if (newPreset)
            {
                this.selectedPreset = newPreset;
                if (currentIvyInfo != null)
                {
                    currentIvyInfo.originalPreset = selectedPreset;
                }

                string presetGUID = UIUtils.GetGUIDByAsset(selectedPreset);
                EditorPrefs.SetString("RealIvyDefaultGUID", presetGUID);

                ivyParametersGUI.CopyFrom(selectedPreset);
                realIvyProWindow.SaveParameters();
                infoPool.meshBuilder.InitLeavesData();
                realIvyProWindow.valueUpdated = true;
            }
		}

		public void SaveCurrentParametersAsNewPreset(string filePath)
		{
			IvyPreset newPreset = ScriptableObject.CreateInstance<IvyPreset>();
			newPreset.ivyParameters = new IvyParameters(infoPool.ivyParameters);


			AssetDatabase.CreateAsset(newPreset, filePath);
			//IvyPreset newPreset = AssetDatabase.LoadAssetAtPath(filePath, typeof(IvyPreset)) as IvyPreset;
			AssetDatabase.SaveAssets();

			OnPresetChanged(newPreset);
		}

		public void OverridePreset()
		{
			selectedPreset.CopyFrom(ivyParametersGUI);
			
			EditorUtility.SetDirty(selectedPreset);
			AssetDatabase.SaveAssets();

			OnPresetChanged(selectedPreset);
		}

		public void OnScriptReloaded(IvyPreset selectedPreset)
		{
			if(Selection.activeGameObject == null)
			{
				CreateNewIvy(selectedPreset);
			}
			else
			{
				IvyInfo selectedIvyInfo = Selection.activeGameObject.GetComponent<IvyInfo>();
				if(selectedIvyInfo != null)
				{
					ModifyIvy(selectedIvyInfo);
				}
			}
		}

		public bool AreThereUnsavedChanges()
		{
			bool res = !infoPool.ivyParameters.IsEqualTo(selectedPreset.ivyParameters);
			return res;
		}

        public bool GenerateLightmapUVsActivated()
        {
            bool res = infoPool.ivyParameters.generateLightmapUVs != ivyParametersGUI.generateLightmapUVs && ivyParametersGUI.generateLightmapUVs;
            return res;
        }

		public void PrepareRuntimeBaked()
		{
			RTIvy rtIvy = ivyGO.GetComponent<RTIvy>();
			RuntimeBakedIvy rtBakedIvy = (RuntimeBakedIvy)rtIvy;
			RuntimeGrowthParameters defaultGrowthParameters = new RuntimeGrowthParameters();
			IvyController ivyController = ivyGO.GetComponent<IvyController>();

			if (rtIvy == null)
			{
				rtBakedIvy = ivyGO.GetComponent<RuntimeBakedIvy>();
				if (rtBakedIvy == null)
				{
					rtBakedIvy = ivyGO.AddComponent<RuntimeBakedIvy>();
				}

				if(ivyController == null)
				{
					ivyController = ivyGO.AddComponent<IvyController>();
				}

				ivyController.rtIvy = rtBakedIvy;
				ivyController.ivyContainer = currentIvyInfo.infoPool.ivyContainer;
				ivyController.ivyParameters = currentIvyInfo.infoPool.ivyParameters;
				ivyController.growthParameters = defaultGrowthParameters;


				rtBakedIvy.meshFilter = rtBakedIvy.GetComponent<MeshFilter>();
				rtBakedIvy.meshRenderer = rtBakedIvy.GetComponent<MeshRenderer>();


				if (rtBakedIvy.mrProcessedMesh == null)
				{
					GameObject processedMesh = new GameObject();
					processedMesh.name = "processedMesh";
					processedMesh.transform.parent = rtBakedIvy.transform;
					processedMesh.transform.localPosition = Vector3.zero;
					processedMesh.transform.localRotation = Quaternion.identity;
					MeshRenderer mrProcessedMesh = processedMesh.AddComponent<MeshRenderer>();
					MeshFilter mfProcessedMesh = processedMesh.AddComponent<MeshFilter>();

					rtBakedIvy.mrProcessedMesh = mrProcessedMesh;
					rtBakedIvy.mfProcessedMesh = mfProcessedMesh;
				}
			}

			ivyController.ivyParameters = currentIvyInfo.infoPool.ivyParameters;
		}

		public void PrepareRuntimeProcedural()
		{
			RTIvy rtIvy = ivyGO.GetComponent<RTIvy>();

			if (rtIvy == null)
			{
				RuntimeProceduralIvy rtProceduralIvy = ivyGO.GetComponent<RuntimeProceduralIvy>();
				if (rtProceduralIvy == null)
				{
					rtProceduralIvy = ivyGO.AddComponent<RuntimeProceduralIvy>();
				}

				IvyController ivyController = ivyGO.GetComponent<IvyController>();
				if (ivyController == null)
				{
					ivyController = ivyGO.AddComponent<IvyController>();
				}

				ivyController.rtIvy = rtProceduralIvy;
				ivyController.ivyContainer = currentIvyInfo.infoPool.ivyContainer;
				ivyController.ivyParameters = currentIvyInfo.infoPool.ivyParameters;
				ivyController.growthParameters = new RuntimeGrowthParameters();


				//rtProceduralIvy.ivyInfo = currentIvyInfo;
				rtProceduralIvy.meshFilter = rtProceduralIvy.GetComponent<MeshFilter>();
				rtProceduralIvy.meshRenderer = rtProceduralIvy.GetComponent<MeshRenderer>();


				if (rtProceduralIvy.mfProcessedMesh == null)
				{
					GameObject processedMesh = new GameObject();
					processedMesh.name = "processedMesh";
					processedMesh.transform.parent = rtProceduralIvy.transform;
					processedMesh.transform.localPosition = Vector3.zero;
					processedMesh.transform.localRotation = Quaternion.identity;
					MeshRenderer mrProcessedMesh = processedMesh.AddComponent<MeshRenderer>();
					MeshFilter mfProcessedMesh = processedMesh.AddComponent<MeshFilter>();

					rtProceduralIvy.mfProcessedMesh = mfProcessedMesh;
					rtProceduralIvy.mrProcessedMesh = mrProcessedMesh;
				}
			}
		}

		public void Optimize()
        {
            for (int b = 0; b < infoPool.ivyContainer.branches.Count; b++)
            {
                BranchContainer branch = infoPool.ivyContainer.branches[b];
                for (int p = 1; p < branch.branchPoints.Count - 1; p++)
                {
                    Vector3 segment1 = branch.branchPoints[p].point - branch.branchPoints[p - 1].point;
                    Vector3 segment2 = branch.branchPoints[p + 1].point - branch.branchPoints[p].point;
                    if (Vector3.Angle(segment1, segment2) < infoPool.ivyParameters.optAngleBias)
                    {
                        SaveIvy();
                        branch.RemoveBranchPoint(p);
                        RefreshMesh();
                    }
                }
            }
        }
	}
}
