using Dynamite3D.RealIvy;
using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class UIZone_GrowthSettings : UIDropDownZone
	{
		private AnimationCurve animationCurve;

		public override void DrawZone(string sectionName, float areaMaxHeight, RealIvyWindow realIvyProWindow, IvyParametersGUI ivyParametersGUI,
				GUISkin windowSkin, RealIvyProWindowController controller,
				ref float YSpace, ref float presetDropDownYSpace, ref float areaYSpace, Rect generalArea,
				Color bgColor, AnimationCurve animationCurve)
		{
			base.DrawZone(sectionName, areaMaxHeight, realIvyProWindow, ivyParametersGUI, 
				windowSkin, controller, ref YSpace, ref presetDropDownYSpace, 
				ref areaYSpace, generalArea, bgColor, animationCurve);

			GUILayout.BeginArea(areaRect);

			EditorGUI.DrawRect(new Rect(0f, areaYSpace, generalArea.width, 58f), bgColor);
			UIUtils.CustomIntFloatField(ivyParametersGUI.stepSize, 0.01f, "Step Size", 50f, areaYSpace, 20f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.directionFrequency, 0.01f, "Dir. Frequency", 50f, areaYSpace, 120f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.directionAmplitude, 0.01f, "Dir. Amplitude", 50f, areaYSpace, 225f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.directionRandomness, 0.01f, "Dir. Random", 50f, areaYSpace, 320f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			areaYSpace += 58f;

			GUI.Label(new Rect(15f, areaYSpace, 100f, 25f), "Gravity", windowSkin.label);
			UIUtils.CustomIntFloatField(ivyParametersGUI.gravityX, 0.01f, "X", 50f, areaYSpace + 25f, 15f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.gravityY, 0.01f, "Y", 50f, areaYSpace + 25f, 70f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.gravityZ, 0.01f, "Z", 50f, areaYSpace + 25f, 125f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.stiffness, 0.01f, "Stiffness", 50f, areaYSpace + 25f, 225f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.grabProvabilityOnFall, 0.01f, "Grab prob. on falling", 50f, areaYSpace + 25f, 320f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			areaYSpace += 86f;

			EditorGUI.DrawRect(new Rect(0f, areaYSpace - 5f, generalArea.width, 60f), bgColor);
			GUI.Label(new Rect(15f, areaYSpace + 15f, 150f, 25f), "Distance to surface", windowSkin.label);
			UIUtils.CustomIntFloatField(ivyParametersGUI.maxDistanceToSurface, 0.001f, "Max", 50f, areaYSpace, 165f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.minDistanceToSurface, 0.001f, "Min", 50f, areaYSpace, 225f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.DTSFrequency, 0.01f, "Freq.", 50f, areaYSpace, 285f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.DTSRandomness, 0.01f, "Random", 50f, areaYSpace, 345f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			areaYSpace += 60f;

			UIUtils.CustomIntFloatField(ivyParametersGUI.maxBranchs, 0.1f, "Max. Branches", 50f, areaYSpace, 20f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.branchProvability, 0.001f, "Branch probability", 50f, areaYSpace, 120f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			areaYSpace += 55f;

			GUILayout.EndArea();

			YSpace += areaRect.height;
			realIvyProWindow.Repaint();
		}
	}
}