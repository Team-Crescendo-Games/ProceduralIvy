using UnityEngine;

namespace Dynamite3D.RealIvy
{
    public class DropDownButton
    {
        public bool unfolded;
        private bool animating;
        public float timer;
        private float lastTimeMarker;

        public void Draw(string buttonText, GUISkin guiSkin, Texture2D arrowTex,
            Rect generalArea, ref float areaHeight, ref float YSpace, float areaMaxHeight,
            AnimationCurve animationCurve, UIDropDownZone dropDownZone)
        {
            if (GUI.Button(new Rect(0f, YSpace, generalArea.width + 20f, 40f), buttonText, guiSkin.GetStyle("sectionbutton")))
            {
                lastTimeMarker = Time.realtimeSinceStartup;
                unfolded = !unfolded;
                animating = true;
            }
            GUIUtility.RotateAroundPivot(90f * (1 - timer), new Vector2(generalArea.width, YSpace + 20f));
            GUI.DrawTexture(new Rect(generalArea.width - 17f, YSpace + 2f, 35f, 35f), arrowTex);
            GUIUtility.RotateAroundPivot(-90f * (1 - timer), new Vector2(generalArea.width, YSpace + 20f));

            YSpace += 50f;

            UpdateArea(ref areaHeight, areaMaxHeight, animationCurve);
        }

        void UpdateArea(ref float areaHeight, float areaMaxHeight, AnimationCurve animationCurve)
        {
            if (animating)
            {
                if (unfolded)
                {
                    timer += 0.04f;
                }
                else
                {
                    timer -= 0.04f;
                }

                areaHeight = Mathf.Lerp(0, areaMaxHeight, animationCurve.Evaluate(timer));

                if (timer >= 1f)
                {
                    timer = 1f;
                    areaHeight = areaMaxHeight;
                    animating = false;
                }
                else if (timer <= 0f)
                {
                    timer = 0f;
                    areaHeight = 0f;
                    animating = false;
                }
            }
        }

        public void ChangeState()
        {
            unfolded = !unfolded;
            animating = true;
        }

        public float GetTimer()
        {
            return timer;
        }
    }
}