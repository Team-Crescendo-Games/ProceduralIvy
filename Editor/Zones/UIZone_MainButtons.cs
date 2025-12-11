using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TeamCrescendo.ProceduralIvy
{
    public class UIZone_MainButtons
    {
        private Rect currentArea;
        private GUISkin windowSkin;

        public void DrawZone(ProceduralIvyWindow proceduralIvyProWindow, IvyParametersGUI ivyParametersGUI,
            GUISkin windowSkin,
            ref float YSpace, Rect generalArea, Color bgColor)
        {
            this.windowSkin = windowSkin;

            currentArea = new Rect(10f, 10f, generalArea.width, 520f);
            var gameObjectName = "None";
            if (ProceduralIvyWindow.Instance.currentIvyInfo != null) 
                gameObjectName = ProceduralIvyWindow.Instance.ivyGO.name;

            var boxText = "Editing object: " + gameObjectName;
            GUI.Box(new Rect(0f, YSpace, generalArea.width + 20f, 40f), boxText, windowSkin.GetStyle("title"));
            YSpace += 45f;
            GUILayout.BeginArea(currentArea);
            var placeButtonStyle = windowSkin.button;

            GUI.Label(new Rect(170f, YSpace - 15f, 100f, 40f), "Main Controls", windowSkin.label);
            YSpace += 25f;

            var placeButtonText = "Place Seed";
            if (proceduralIvyProWindow.placingSeed)
            {
                placeButtonStyle = windowSkin.GetStyle("buttonorange");
                placeButtonText = "Stop Placing";
            }

            var startStopButtonStyle = windowSkin.button;
            var startStopButtonText = "Start Growth";
            if (ProceduralIvyWindow.Instance.infoPool.growth.growing)
            {
                startStopButtonStyle = windowSkin.GetStyle("buttonorange");
                startStopButtonText = "Stop Growth";
            }

            if (GUI.Button(new Rect(20f, YSpace, 100f, 25f), placeButtonText, placeButtonStyle))
                proceduralIvyProWindow.placingSeed = !proceduralIvyProWindow.placingSeed;


            if (GUI.Button(new Rect(140f, YSpace, 100f, 25f), "Randomize", windowSkin.button))
                CheckRestrictions(Randomize);
            if (GUI.Button(new Rect(20f, YSpace + 40f, 100f, 25f), startStopButtonText, startStopButtonStyle))
                CheckIvySelectedBeforeAction(StartStopGrowth);
            if (GUI.Button(new Rect(140f, YSpace + 40f, 100f, 25f), "Reset", windowSkin.button))
                CheckIvySelectedBeforeAction(Reset);

            if (GUI.Button(new Rect(275f, YSpace + 5f, 100f, 25f), "Optimize", windowSkin.button))
                CheckRestrictions(Optimize);

            var optimizeAngleLabel = new Rect(330f, YSpace + 35f, 50f, 20f);
            GUI.Label(optimizeAngleLabel, "Angle", windowSkin.label);
            if (EditorGUI.DropdownButton(optimizeAngleLabel, GUIContent.none, FocusType.Keyboard,
                    windowSkin.GetStyle("transparent")))
            {
                proceduralIvyProWindow.updatingParameter = ivyParametersGUI.optAngleBias;
                proceduralIvyProWindow.updatingValue = true;
                proceduralIvyProWindow.updatingValueMultiplier = 0.1f;
                proceduralIvyProWindow.originalUpdatingValue = ivyParametersGUI.optAngleBias;
                proceduralIvyProWindow.mouseStartPoint = GUIUtility.GUIToScreenPoint(Event.current.mousePosition).x;
            }

            if (optimizeAngleLabel.Contains(Event.current.mousePosition))
                EditorGUIUtility.AddCursorRect(new Rect(Event.current.mousePosition, Vector2.one * 20f),
                    MouseCursor.SlideArrow);
            ivyParametersGUI.optAngleBias.value = EditorGUI.FloatField(new Rect(275, YSpace + 35f, 50f, 20f), "",
                ivyParametersGUI.optAngleBias, windowSkin.GetStyle("textfield"));

            YSpace += 65f;

            YSpace += 15f;
            EditorGUI.DrawRect(new Rect(0f, YSpace, generalArea.width, 2f), bgColor);
            YSpace += 20f;

            GUI.Label(new Rect(80f, YSpace - 20f, 200f, 40f), "Save ivy", windowSkin.label);
            if (GUI.Button(new Rect(10f, YSpace + 20f, 90f, 40f), "Save into Scene", windowSkin.button))
                CheckRestrictions(SaveCurrentIvyIntoScene);
            if (GUI.Button(new Rect(110f, YSpace + 20f, 90f, 40f), "Save as prefab", windowSkin.button))
                CheckRestrictions(SaveAsPrefab);

            EditorGUI.DrawRect(new Rect(209f, YSpace - 5f, 2f, 75f), bgColor);

            GUI.Label(new Rect(245f, YSpace - 20f, 200f, 40f), "Convert to runtime Ivy", windowSkin.label);
            if (GUI.Button(new Rect(220f, YSpace + 20f, 90f, 40f), "Runtime Procedural", windowSkin.button))
                CheckRestrictions(PrepareRuntimeProcedural);

            if (GUI.Button(new Rect(320f, YSpace + 20f, 90f, 40f), "Runtime Baked", windowSkin.button))
                CheckRestrictions(PrepareRuntimeBaked);
            YSpace += 90f;

            GUILayout.EndArea();
        }

        private void CheckRestrictions(Action action)
        {
            if (ProceduralIvyWindow.Instance.currentIvyInfo == null)
                UIUtils.NoIvySelectedLogMessage();
            else if (ProceduralIvyWindow.Instance.infoPool.growth.growing)
                UIUtils.CannotEditGrowingIvy();
            else
                action();
        }

        private void CheckIvySelectedBeforeAction(Action action)
        {
            if (ProceduralIvyWindow.Instance.currentIvyInfo == null)
                UIUtils.NoIvySelectedLogMessage();
            else
                action();
        }

        private void Randomize()
        {
            ProceduralIvyWindow.Instance.infoPool.ivyParameters.randomSeed = Environment.TickCount;
            Random.InitState(ProceduralIvyWindow.Instance.infoPool.ivyParameters.randomSeed);
        }

        private void Reset()
        {
            ProceduralIvyWindow.Instance.infoPool.growth.growing = false;
            ProceduralIvyWindow.Instance.ResetIvy();
        }

        private void Optimize()
        {
            ProceduralIvyWindow.Instance.OptimizeCurrentIvy();
        }

        private void SaveCurrentIvyIntoScene()
        {
            if (ProceduralIvyWindow.Instance.ivyGO.GetComponent<RuntimeIvy>())
            {
                UIUtils.CannotConvertToRuntimeIvy();
                return;
            }

            CustomDisplayDialog.Init(windowSkin, EditorConstants.CONFIRM_SAVE_IVY, "Save ivy into scene",
                ProceduralIvyWindow.infoTex, 370f, 155f, 
                ProceduralIvyWindow.Instance.SaveCurrentIvyIntoScene, true);
        }

        private void SaveAsPrefab()
        {
            if (ProceduralIvyWindow.Instance.ivyGO.GetComponent<RuntimeIvy>())
            {
                UIUtils.CannotConvertToRuntimeIvy();
                return;
            }
            
            Action confirmCallback = () =>
            {
                var fileName = ProceduralIvyWindow.Instance.ivyGO.name;
                ProceduralIvyWindow.Instance.SaveCurrentIvyAsPrefab(fileName);
            };

            CustomDisplayDialog.Init(windowSkin, EditorConstants.CONFIRM_SAVE_IVY, "Save ivy into scene",
                ProceduralIvyWindow.infoTex, 370f, 155f, confirmCallback, true);
        }

        private void PrepareRuntimeProcedural()
        {
            Reset();
            ProceduralIvyWindow.Instance.PrepareRuntimeProcedural();
        }

        private void PrepareRuntimeBaked()
        {
            ProceduralIvyWindow.Instance.PrepareRuntimeBaked();
        }

        private void StartStopGrowth()
        {
            if (ProceduralIvyWindow.Instance.ivyGO)
                ProceduralIvyWindow.Instance.StartIvy(ProceduralIvyWindow.Instance.infoPool.ivyContainer.ivyGO.transform.position,
                    -ProceduralIvyWindow.Instance.infoPool.ivyContainer.ivyGO.transform.up);
            if (!ProceduralIvyWindow.Instance.infoPool.growth.growing) ProceduralIvyWindow.Instance.RecordIvyToUndo();
            ProceduralIvyWindow.Instance.infoPool.growth.growing = !ProceduralIvyWindow.Instance.infoPool.growth.growing;
        }
    }
}