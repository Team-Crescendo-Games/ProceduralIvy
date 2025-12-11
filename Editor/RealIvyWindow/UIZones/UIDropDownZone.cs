using Dynamite3D.RealIvy;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public abstract class UIDropDownZone
	{
		public RealIvyWindow realIvyProWindow;
		protected float areaHeight;
		protected DropDownButton dropDownButton = new DropDownButton();
		protected Rect areaRect;

		public virtual void DrawZone(string sectionName, float areaMaxHeight, RealIvyWindow realIvyProWindow, IvyParametersGUI ivyParametersGUI,
				GUISkin windowSkin, RealIvyProWindowController controller,
				ref float YSpace, ref float presetDropDownYSpace, ref float areaYSpace, Rect generalArea,
				Color bgColor, AnimationCurve animationCurve)
		{
			this.realIvyProWindow = realIvyProWindow;

			dropDownButton.Draw(sectionName, windowSkin, RealIvyWindow.downArrowTex, 
				generalArea, ref areaHeight, ref YSpace, areaMaxHeight, animationCurve, this);

			areaRect = new Rect(10f, YSpace, generalArea.width, areaHeight);
		}

		protected float GetTimer()
		{
			float res = dropDownButton.GetTimer();
			return res;
		}

		protected void ChangeState()
		{
			dropDownButton.ChangeState();
		}
	}
}