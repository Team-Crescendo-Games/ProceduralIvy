using Dynamite3D.RealIvy;
using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class UIZone_LeavesSettings : UIDropDownZone
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

			this.animationCurve = animationCurve;

			GUILayout.BeginArea(areaRect);

			EditorGUI.DrawRect(new Rect(0f, areaYSpace, generalArea.width, 50f), bgColor);
			GUI.Label(new Rect(15f, areaYSpace + 15f, 100f, 25f), "Orientation", windowSkin.label);
			realIvyProWindow.OrientationToggle(100f, areaYSpace);
			UIUtils.CustomIntFloatField(ivyParametersGUI.leaveEvery, 0.01f, "Leaves Interval", 50f, areaYSpace, 250f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.randomLeaveEvery, 0.01f, "Random", 50f, areaYSpace, 330f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			areaYSpace += 45f;

			if (controller.infoPool.ivyParameters.globalOrientation)
			{
				GUI.Label(new Rect(15f, areaYSpace + 15f, 100f, 25f), "Global Rotation", windowSkin.label);
				UIUtils.CustomIntFloatField(ivyParametersGUI.globalRotationX, 0.01f, "X", 50f, areaYSpace + 40f, 15f, windowSkin, realIvyProWindow.OnUpdatingParameter);
				UIUtils.CustomIntFloatField(ivyParametersGUI.globalRotationY, 0.01f, "Y", 50f, areaYSpace + 40f, 70f, windowSkin, realIvyProWindow.OnUpdatingParameter);
				UIUtils.CustomIntFloatField(ivyParametersGUI.globalRotationZ, 0.01f, "Z", 50f, areaYSpace + 40f, 125f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			}
			else
			{
				GUI.Label(new Rect(15f, areaYSpace + 15f, 100f, 25f), "Local Rotation", windowSkin.label);
				UIUtils.CustomIntFloatField(ivyParametersGUI.rotationX, 0.01f, "X", 50f, areaYSpace + 40f, 15f, windowSkin, realIvyProWindow.OnUpdatingParameter);
				UIUtils.CustomIntFloatField(ivyParametersGUI.rotationY, 0.01f, "Y", 50f, areaYSpace + 40f, 70f, windowSkin, realIvyProWindow.OnUpdatingParameter);
				UIUtils.CustomIntFloatField(ivyParametersGUI.rotationZ, 0.01f, "Z", 50f, areaYSpace + 40f, 125f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			}

			//EditorGUI.DrawRect(new Rect(0f, leavesAreaYSpace - 5f, generalAreaWidth, 80f), bckgColor2);
			GUI.Label(new Rect(15f + 200f, areaYSpace + 15f, 100f, 25f), "Randomness", windowSkin.label);
			UIUtils.CustomIntFloatField(ivyParametersGUI.randomRotationX, 0.01f, "X", 50f, areaYSpace + 40f, 215f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.randomRotationY, 0.01f, "Y", 50f, areaYSpace + 40f, 270f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.randomRotationZ, 0.01f, "Z", 50f, areaYSpace + 40f, 325f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			areaYSpace += 80f;

			EditorGUI.DrawRect(new Rect(0f, areaYSpace + 15f, generalArea.width, 80f), bgColor);
			GUI.Label(new Rect(15f, areaYSpace + 15f, 100f, 25f), "Position", windowSkin.label);
			UIUtils.CustomIntFloatField(ivyParametersGUI.offsetX, 0.001f, "X", 50f, areaYSpace + 40f, 15f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.offsetY, 0.001f, "Y", 50f, areaYSpace + 40f, 70f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.offsetZ, 0.001f, "Z", 50f, areaYSpace + 40f, 125f, windowSkin, realIvyProWindow.OnUpdatingParameter);

			GUI.Label(new Rect(215f, areaYSpace + 15f, 100f, 25f), "Scale", windowSkin.label);
			UIUtils.CustomIntFloatField(ivyParametersGUI.minScale, 0.01f, "Min", 50f, areaYSpace + 40f, 215f, windowSkin, realIvyProWindow.OnUpdatingParameter);
			UIUtils.CustomIntFloatField(ivyParametersGUI.maxScale, 0.01f, "Max", 50f, areaYSpace + 40f, 270f, windowSkin, realIvyProWindow.OnUpdatingParameter);


			areaYSpace += 100f;

			GUILayout.EndArea();

			YSpace += areaRect.height;
			realIvyProWindow.Repaint();
		}
	}
}