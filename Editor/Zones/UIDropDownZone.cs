using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public abstract class UIDropDownZone
    {
        protected float areaHeight;
        protected Rect areaRect;
        protected DropDownButton dropDownButton = new();

        public virtual void DrawZone(string sectionName, float areaMaxHeight, IvyParametersGUI ivyParametersGUI, GUISkin windowSkin, 
            ref float YSpace, ref float presetDropDownYSpace, ref float areaYSpace, Rect generalArea,
            Color bgColor, AnimationCurve animationCurve)
        {
            dropDownButton.Draw(sectionName, windowSkin, ProceduralIvyWindow.downArrowTex,
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