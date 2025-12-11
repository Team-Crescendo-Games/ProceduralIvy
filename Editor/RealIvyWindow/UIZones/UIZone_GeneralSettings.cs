using Dynamite3D.RealIvy;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class UIZone_GeneralSettings : UIDropDownZone
	{
		//private Rect currentArea;

		//private Rect currentArea;
		//private RealIvyProWindowController controller;
		//private RealIvyProWindow realIvyProWindow;
		//private IvyParametersGUI ivyParametersGUI;
		//private UIZone_Presets presetsZone = new UIZone_Presets();

		//private string presetGUID;

        public UIZone_GeneralSettings()
        {
            base.areaHeight = 265f;
            base.dropDownButton.timer = 1f;
            base.dropDownButton.unfolded = true;
        }

        public override void DrawZone(string sectionName, float areaMaxHeight, RealIvyWindow realIvyProWindow, IvyParametersGUI ivyParametersGUI,
			GUISkin windowSkin, RealIvyProWindowController controller,
			ref float YSpace, ref float presetDropDownYSpace, ref float areaYSpace, Rect generalArea,
			Color bgColor, AnimationCurve animationCurve)
		{
			base.DrawZone(sectionName, areaMaxHeight, realIvyProWindow, ivyParametersGUI,
			windowSkin, controller, ref YSpace, ref presetDropDownYSpace,
			ref areaYSpace, generalArea, bgColor, animationCurve);

			GUILayout.BeginArea(areaRect);
			//EditorGUI.DrawRect(new Rect(0f, areaYSpace, generalArea.width, 58f), bgColor);

			//currentArea = new Rect(10f, 10f, generalArea.width, 520f);

			/*GUI.Box(new Rect(0f, areaYSpace, generalArea.width + 20f, 40f), "General Settings", windowSkin.GetStyle("sectionbutton"));*/
			//areaYSpace += 40f;

			//GUILayout.BeginArea(currentArea);
			GUI.Label(new Rect(5f, areaYSpace, 100f, 20f), "Preset:", windowSkin.label);

			areaYSpace += 20f;


			UIUtils.CustomObjectField<IvyPreset>(new Rect(0f, areaYSpace, areaRect.width / 2f, 25f),
				controller.selectedPreset, windowSkin, realIvyProWindow.oldSkin, RealIvyWindow.presetTex, 489136168, string.Empty,
				OnPresetChanged, controller.AreThereUnsavedChanges());


			//bool unsavedChanges = !controller.infoPool.ivyParameters.IsEqualTo(controller.selectedPreset);
			UIUtils.ButtonChangesAlert(new Rect(areaRect.width / 2f + 10f, areaYSpace, 60f, 25f), "Save", "Save *",
				windowSkin.button, windowSkin.GetStyle("bold"), false, SavePreset);

			/*if (GUI.Button(new Rect(currentArea.width / 2f + 10f, YSpace, 60f, 25f), "Save*", windowSkin.GetStyle("bold")))
			{
				SavePreset();
			}*/
			if (GUI.Button(new Rect(areaRect.width / 2f + 70f + 10f, areaYSpace, 130f, 25f), "Save preset as...", windowSkin.button))
			{
				SavePresetAs();
			}


			areaYSpace += 40f;
			GUI.Label(new Rect(5f, areaYSpace, 100f, 20f), "Generate...", windowSkin.label);
			areaYSpace += 20f;

			ivyParametersGUI.generateBranches = GUI.Toggle(new Rect(0f, areaYSpace, 133f, 20f), ivyParametersGUI.generateBranches, "Vines", windowSkin.toggle);
			ivyParametersGUI.generateLeaves = GUI.Toggle(new Rect(generalArea.width / 3f + 10f, areaYSpace, 133f, 20f), ivyParametersGUI.generateLeaves, "Leaves", windowSkin.toggle);
            ivyParametersGUI.generateLightmapUVs = GUI.Toggle(new Rect(generalArea.width / 3f * 2f + 10f, areaYSpace, 133f, 20f), ivyParametersGUI.generateLightmapUVs, "Lightmap UVs", windowSkin.toggle);
            areaYSpace += 20f;

			areaYSpace += 10f;
			GUI.Label(new Rect(5f, areaYSpace, generalArea.width / 3f - 5f, 20f), "Vines material:", windowSkin.label);
			GUI.Label(new Rect(generalArea.width / 3f + 10f, areaYSpace, generalArea.width / 3f - 5f, 20f), "Leaves prefabs:", windowSkin.label);
			areaYSpace += 20f;

			ivyParametersGUI.branchesMaterial = UIUtils.CustomObjectField<Material>(new Rect(0f, areaYSpace, generalArea.width / 3f - 5f, 25f),
				ivyParametersGUI.branchesMaterial, windowSkin, realIvyProWindow.oldSkin, RealIvyWindow.materialTex, 489136169, string.Empty, OnVinesMaterialChanged, false);

			GUI.Label(new Rect(5f, areaYSpace + 35f, generalArea.width / 3f - 5f, 20f), "Layer Mask:", windowSkin.label);
			ivyParametersGUI.layerMask = EditorGUI.MaskField(new Rect(0f, areaYSpace + 55f, generalArea.width / 3f - 5f, 25f),
				InternalEditorUtility.LayerMaskToConcatenatedLayersMask(ivyParametersGUI.layerMask),
				InternalEditorUtility.layers,
				windowSkin.GetStyle("dropdown"));
			GUI.DrawTexture(new Rect(generalArea.width / 3f - 30f, areaYSpace + 55f, 25f, 25f), RealIvyWindow.downArrowTex);
			ivyParametersGUI.layerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(ivyParametersGUI.layerMask);

            ivyParametersGUI.buffer32Bits = GUI.Toggle(new Rect(0f, areaYSpace + 100f, 133f, 20f), ivyParametersGUI.buffer32Bits, "32 bits mesh buffer", windowSkin.toggle);

            GUI.Box(new Rect(generalArea.width / 3f + 5f, areaYSpace, (generalArea.width / 3f - 5f) * 2f, 120f), "", windowSkin.GetStyle("list"));

			float leaveButtonsMargin;
			if (ivyParametersGUI.leavesPrefabs.Count > 3)
			{
				leaveButtonsMargin = 50f;
			}
			else
			{
				leaveButtonsMargin = 35f;
			}

			realIvyProWindow.leavesPrefabsScrollView = GUI.BeginScrollView(new Rect(generalArea.width / 3f + 5f, areaYSpace, (generalArea.width / 3f - 5f) * 2f, 100f), realIvyProWindow.leavesPrefabsScrollView, new Rect(0f, 0f, (generalArea.width / 3f - 21f) * 2f, (ivyParametersGUI.leavesPrefabs.Count + 1) * 25f));

			for (int i = 0; i < ivyParametersGUI.leavesPrefabs.Count + 1; i++)
			{
				if (i < ivyParametersGUI.leavesPrefabs.Count)
				{
					if (ivyParametersGUI.leavesPrefabs[i] != null)
					{
						if (GUI.Button(new Rect(0f, i * 25f, (generalArea.width / 3f - leaveButtonsMargin) * 2f, 25f), ivyParametersGUI.leavesPrefabs[i].name, windowSkin.GetStyle("listelement")))
						{
							EditorGUIUtility.PingObject(ivyParametersGUI.leavesPrefabs[i]);
						}

						GUI.DrawTexture(new Rect(0f, i * 25f, 25f, 25f), RealIvyWindow.leaveTex);

						ivyParametersGUI.leavesProb[i] = EditorGUI.FloatField(new Rect(generalArea.width / 3f * 2f - leaveButtonsMargin * 2f + 10, i * 25f + 2, 25f, 20f), ivyParametersGUI.leavesProb[i], windowSkin.textArea);
						GUI.Label(new Rect(generalArea.width / 3f * 2f - leaveButtonsMargin * 2f - 8f, i * 25f + 2, 25f, 20f), "%", windowSkin.GetStyle("whitelabel"));

						if (GUI.Button(new Rect(generalArea.width / 3f * 2f - leaveButtonsMargin, i * 25f, 25f, 25f), "", windowSkin.GetStyle("removeelement")))
						{
                            RemoveLeavePrefab(ivyParametersGUI, i);

                        }
					}
                    else
                    {
                        RemoveLeavePrefab(ivyParametersGUI, i);
                    }
				}
				else
				{
					GUI.Label(new Rect(0f, i * 25f, (generalArea.width / 3f - 5f) * 2, 25f), "Drag & Drop here to add", windowSkin.GetStyle("tooltip"));
					UIUtils.DragAndDropArea(new Rect(0f, i * 25f, (generalArea.width / 3f - 5f) * 2f, 25f), OnObjectDragged, realIvyProWindow.SaveParameters);
				}
			}

			GUI.EndScrollView();
			

			GUILayout.EndArea();
			YSpace += areaRect.height;
			realIvyProWindow.Repaint();
        }

        private void RemoveLeavePrefab(IvyParametersGUI ivyParametersGUI, int index)
        {
            ivyParametersGUI.leavesPrefabs.RemoveAt(index);
            ivyParametersGUI.leavesProb.RemoveAt(index);
            realIvyProWindow.SaveParameters();

            realIvyProWindow.parametersChanged = true;
            realIvyProWindow.presetsChanged.Add(realIvyProWindow.presetSelected);

            for (int b = 0; b < RealIvyWindow.controller.infoPool.ivyContainer.branches.Count; b++)
            {
                for (int l = 0; l < RealIvyWindow.controller.infoPool.ivyContainer.branches[b].GetNumLeaves(); l++)
                {
                    if (RealIvyWindow.controller.infoPool.ivyContainer.branches[b].leaves[l].chosenLeave == index)
                    {
                        RealIvyWindow.controller.infoPool.ivyContainer.branches[b].leaves[l].chosenLeave = 0;
                    }
                }
            }
        }

		private void OnObjectDragged(UnityEngine.Object objectDragged)
		{
			GameObject prefab = (GameObject)objectDragged;
			if (prefab.GetComponent<MeshFilter>() && prefab.GetComponent<MeshRenderer>())
			{
				if (prefab.GetComponent<MeshFilter>().sharedMesh && prefab.GetComponent<MeshRenderer>().sharedMaterial)
				{
					RealIvyWindow.ivyParametersGUI.leavesPrefabs.Add((GameObject)objectDragged);
					RealIvyWindow.ivyParametersGUI.leavesProb.Add(1f);


					realIvyProWindow.presetsChanged.Add(realIvyProWindow.presetSelected);

					realIvyProWindow.leavesPrefabsScrollView += new Vector2(0f, 25f);
				}
				else
				{
					Debug.Log("The prefab has no mesh or material assigned", objectDragged);
				}
			}
			else
			{
				Debug.Log("The prefab has no Mesh Filter or Mesh Renderer component attached", objectDragged);
			}
		}

		private void OnPresetChanged(IvyPreset newPreset)
		{
			RealIvyWindow.controller.OnPresetChanged(newPreset);
		}

		private void OnVinesMaterialChanged(Material newMaterial)
		{
			RealIvyWindow.controller.OnVinesMaterialChanged(newMaterial);
		}

		private void SavePreset()
		{
			if (RealIvyWindow.controller.infoPool.ivyParameters != null)
			{
				RealIvyWindow.controller.OverridePreset();
			}
		}

		private void SavePresetAs()
		{
			string path = EditorUtility.SaveFilePanelInProject("Save preset as...", "RealIvy Preset", "asset", "");

			if (path != "")
			{
				RealIvyWindow.controller.SaveCurrentParametersAsNewPreset(path);
			}
		}
	}
}
