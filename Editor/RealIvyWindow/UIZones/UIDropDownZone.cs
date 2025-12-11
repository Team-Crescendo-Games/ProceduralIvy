using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public abstract class UIDropDownZone
    {
        protected float areaHeight;
        protected Rect areaRect;
        protected DropDownButton dropDownButton = new();
        public RealIvyWindow realIvyProWindow;

        public virtual void DrawZone(string sectionName, float areaMaxHeight, RealIvyWindow realIvyProWindow,
            IvyParametersGUI ivyParametersGUI,
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
            var res = dropDownButton.GetTimer();
            return res;
        }

        protected void ChangeState()
        {
            dropDownButton.ChangeState();
        }
    }
}