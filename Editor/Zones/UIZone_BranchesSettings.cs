using UnityEditor;
using UnityEngine;

namespace TeamCrescendo.ProceduralIvy
{
    public class UIZone_BranchesSettings : UIDropDownZone
    {
        public override void DrawZone(string sectionName, float areaMaxHeight, ProceduralIvyWindow proceduralIvyProWindow,
            IvyParametersGUI ivyParametersGUI,
            GUISkin windowSkin, ProceduralIvyWindowController controller,
            ref float YSpace, ref float presetDropDownYSpace, ref float areaYSpace, Rect generalArea,
            Color bgColor, AnimationCurve animationCurve)
        {
            base.DrawZone(sectionName, areaMaxHeight, proceduralIvyProWindow, ivyParametersGUI, windowSkin,
                controller, ref YSpace, ref presetDropDownYSpace,
                ref areaYSpace, generalArea, bgColor, animationCurve);

            GUILayout.BeginArea(areaRect);

            EditorGUI.DrawRect(new Rect(0f, areaYSpace, generalArea.width, 55f), bgColor);
            GUI.Label(new Rect(15f, areaYSpace + 15f, 100f, 25f), "Radius", windowSkin.label);
            UIUtils.CustomIntFloatField(ivyParametersGUI.minRadius, 0.001f, "Min", 50f, areaYSpace, 100f, windowSkin,
                proceduralIvyProWindow.OnUpdatingParameter);
            UIUtils.CustomIntFloatField(ivyParametersGUI.maxRadius, 0.001f, "Max", 50f, areaYSpace, 155f, windowSkin,
                proceduralIvyProWindow.OnUpdatingParameter);
            UIUtils.CustomIntFloatField(ivyParametersGUI.radiusVarFreq, 0.01f, "Freq.", 50f, areaYSpace, 215f,
                windowSkin, proceduralIvyProWindow.OnUpdatingParameter);
            UIUtils.CustomIntFloatField(ivyParametersGUI.radiusVarOffset, 0.01f, "Offset", 50f, areaYSpace, 275f,
                windowSkin, proceduralIvyProWindow.OnUpdatingParameter);
            UIUtils.CustomIntFloatField(ivyParametersGUI.tipInfluence, 0.01f, "Tip", 50f, areaYSpace, 335f, windowSkin,
                proceduralIvyProWindow.OnUpdatingParameter);
            areaYSpace += 60f;

            GUI.Label(new Rect(15f, areaYSpace + 15f, 100f, 25f), "UVs", windowSkin.label);
            UIUtils.CustomIntFloatField(ivyParametersGUI.uvScaleX, 0.01f, "X Scale", 50f, areaYSpace, 100f, windowSkin,
                proceduralIvyProWindow.OnUpdatingParameter);
            UIUtils.CustomIntFloatField(ivyParametersGUI.uvScaleY, 0.01f, "Y Scale", 50f, areaYSpace, 155f, windowSkin,
                proceduralIvyProWindow.OnUpdatingParameter);
            UIUtils.CustomIntFloatField(ivyParametersGUI.uvOffsetX, 0.01f, "X Offset", 50f, areaYSpace, 215f,
                windowSkin, proceduralIvyProWindow.OnUpdatingParameter);
            UIUtils.CustomIntFloatField(ivyParametersGUI.uvOffsetY, 0.01f, "Y Offset", 50f, areaYSpace, 270f,
                windowSkin, proceduralIvyProWindow.OnUpdatingParameter);
            areaYSpace += 60f;

            EditorGUI.DrawRect(new Rect(0f, areaYSpace - 5f, generalArea.width, 60f), bgColor);
            GUI.Label(new Rect(15f, areaYSpace + 15f, 100f, 25f), "Others", windowSkin.label);

            UIUtils.CustomIntFloatField(ivyParametersGUI.sides, 0.01f, "Sides", 50f, areaYSpace, 100f, windowSkin,
                proceduralIvyProWindow.OnUpdatingParameter);

            ivyParametersGUI.halfgeom = GUI.Toggle(new Rect(230f, areaYSpace + 16f, 133f, 20f),
                ivyParametersGUI.halfgeom, "Half Section", windowSkin.toggle);

            areaYSpace += 60f;

            GUILayout.EndArea();

            YSpace += areaRect.height;
            proceduralIvyProWindow.Repaint();
        }
    }
}