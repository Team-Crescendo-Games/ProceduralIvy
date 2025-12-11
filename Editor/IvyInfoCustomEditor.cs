using Dynamite3D.RealIvy;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	[CustomEditor(typeof(IvyInfo))]
	public class IvyInfoCustomEditor : Editor
	{
		private IvyInfo ivyInfo;

		public override void OnInspectorGUI()
		{
			if (GUILayout.Button("Edit in Real Ivy Editor"))
			{
				IvyInfo ivyInfo = (IvyInfo)target;
				RealIvyWindow.Init();
				RealIvyWindow.controller.ModifyIvy(ivyInfo);
			}
		}
	}
}