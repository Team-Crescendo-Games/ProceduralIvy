using Dynamite3D.RealIvy;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UIZone_Presets : UIDropDownZone
{
	/*private Vector2 presetsScrollView;
	private List<int> presetsChanged = new List<int>();
	private int presetSelected;


	public override void DrawZone(string sectionName, float areaMaxHeight, RealIvyProWindow realIvyProWindow, IvyParametersGUI ivyParametersGUI,
			GUISkin windowSkin, RealIvyProWindowController controller,
			ref float YSpace, ref float presetDropDownYSpace, ref float areaYSpace, Rect generalArea,
			Rect currentArea, Color bgColor, AnimationCurve animationCurve)
	{
		base.DrawZone(sectionName, areaMaxHeight, realIvyProWindow, ivyParametersGUI,
			windowSkin, controller, ref YSpace, ref presetDropDownYSpace,
			ref areaYSpace, generalArea, currentArea, bgColor, animationCurve);



		float dropdownButtonWidth = currentArea.width;
		if (controller.presets.Count > 10)
		{
			dropdownButtonWidth -= 17f;
		}


		
		GUILayout.BeginArea(areaRect);
		EditorGUI.DrawRect(new Rect(0f, 0f, generalArea.width, areaHeight), new Color(0.1f, 0.1f, 0.1f));

		presetsScrollView = GUI.BeginScrollView(new Rect(0f, 0f, areaRect.width, areaHeight), presetsScrollView,
			new Rect(0f, 0f, generalArea.width - 17f, (controller.presets.Count + 1) * 25f * GetTimer()),
			false, false);

		for (int i = 0; i < controller.presets.Count + 1; i++)
		{
			if (i < controller.presets.Count)
			{
				string presetName = controller.presets[i].presetName;
				if (presetsChanged.Contains(i))
				{
					presetName = presetName + "*";

				}
				GUIStyle presetButtonStyle = windowSkin.GetStyle("dropdownbutton");
				if (presetsChanged.Contains(i))
				{
					presetButtonStyle = windowSkin.GetStyle("dropdownbuttonchanged");
				}
				if (GUI.Button(new Rect(0f, 25f * i, dropdownButtonWidth, 25f), presetName, presetButtonStyle))
				{
					controller.presets[presetSelected] = ScriptableObject.Instantiate<Dynamite3D.RealIvyPro.IvyParameters>(controller.infoPool.ivyParameters);

					controller.infoPool.ivyParameters = ScriptableObject.Instantiate<Dynamite3D.RealIvyPro.IvyParameters>(controller.presets[i]);
					ivyParametersGUI.CopyFrom(controller.infoPool.ivyParameters);
					ChangeState();
					//presetsAreaState = !presetsAreaState;
					//presetsAreaAnim = true;
					presetSelected = i;

					if (controller.ivyGO)
					{
						controller.infoPool.meshBuilder.InitLeavesData();
						controller.infoPool.meshBuilder.Initialize();
					}

					if (presetsChanged.Contains(i))
					{
						realIvyProWindow.parametersChanged = true;
					}
					else
					{
						realIvyProWindow.parametersChanged = false;
					}
				}
			}
			else
			{
				if (GUI.Button(new Rect(0f, 25f * i, dropdownButtonWidth, 125f), "Load presets from folder..."))
				{
					if (realIvyProWindow.parametersChanged)
					{
						realIvyProWindow.confirmAction = true;
						realIvyProWindow.confirmActionLoadPresets = true;
					}
					else
					{
						realIvyProWindow.SelectPresetsFolder();
					}

				}
			}
		}

		GUI.EndScrollView();
		GUILayout.EndArea();
	}*/
}