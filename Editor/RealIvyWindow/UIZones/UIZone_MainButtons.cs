using System;
using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
    public class UIZone_MainButtons
    {
        private Rect currentArea;
        private RealIvyProWindowController controller;
        private RealIvyWindow realIvyProWindow;
        private IvyParametersGUI ivyParametersGUI;
        private GUISkin windowSkin;

        public void DrawZone(RealIvyWindow realIvyProWindow, IvyParametersGUI ivyParametersGUI,
            GUISkin windowSkin, RealIvyProWindowController controller,
            ref float YSpace, Rect generalArea, Color bgColor)
        {
            this.controller = controller;
            this.realIvyProWindow = realIvyProWindow;
            this.ivyParametersGUI = ivyParametersGUI;
            this.windowSkin = windowSkin;


            currentArea = new Rect(10f, 10f, generalArea.width, 520f);
            string gameObjectName = "None";
            if (controller.currentIvyInfo != null)
            {
                gameObjectName = controller.ivyGO.name;
            }

            string boxText = "Editing object: " + gameObjectName;
            GUI.Box(new Rect(0f, YSpace, generalArea.width + 20f, 40f), boxText, windowSkin.GetStyle("title"));
            YSpace += 45f;
            GUILayout.BeginArea(currentArea);
            GUIStyle placeButtonStyle = windowSkin.button;

            GUI.Label(new Rect(170f, YSpace - 15f, 100f, 40f), "Main Controls", windowSkin.label);
            YSpace += 25f;

            string placeButtonText = "Place Seed";
            if (realIvyProWindow.placingSeed)
            {
                placeButtonStyle = windowSkin.GetStyle("buttonorange");
                placeButtonText = "Stop Placing";
            }

            GUIStyle startStopButtonStyle = windowSkin.button;
            string startStopButtonText = "Start Growth";
            if (controller.infoPool.growth.growing)
            {
                startStopButtonStyle = windowSkin.GetStyle("buttonorange");
                startStopButtonText = "Stop Growth";
            }
            if (GUI.Button(new Rect(20f, YSpace, 100f, 25f), placeButtonText, placeButtonStyle))
            {
                realIvyProWindow.placingSeed = !realIvyProWindow.placingSeed;
            }


            if (GUI.Button(new Rect(140f, YSpace, 100f, 25f), "Randomize", windowSkin.button))
            {
                CheckRestrictions(Randomize);
            }
            if (GUI.Button(new Rect(20f, YSpace + 40f, 100f, 25f), startStopButtonText, startStopButtonStyle))
            {
                CheckIvySelectedBeforeAction(StartStopGrowth);
            }
            if (GUI.Button(new Rect(140f, YSpace + 40f, 100f, 25f), "Reset", windowSkin.button))
            {
                CheckIvySelectedBeforeAction(Reset);
            }

            if (GUI.Button(new Rect(275f, YSpace + 5f, 100f, 25f), "Optimize", windowSkin.button))
            {
                CheckRestrictions(Optimize);
            }

            Rect optimizeAngleLabel = new Rect(330f, YSpace + 35f, 50f, 20f);
            GUI.Label(optimizeAngleLabel, "Angle", windowSkin.label);
            if (EditorGUI.DropdownButton(optimizeAngleLabel, GUIContent.none, FocusType.Keyboard, windowSkin.GetStyle("transparent")))
            {
                realIvyProWindow.updatingParameter = ivyParametersGUI.optAngleBias;
                realIvyProWindow.updatingValue = true;
                realIvyProWindow.updatingValueMultiplier = 0.1f;
                realIvyProWindow.originalUpdatingValue = ivyParametersGUI.optAngleBias;
                realIvyProWindow.mouseStartPoint = GUIUtility.GUIToScreenPoint(Event.current.mousePosition).x;
            }
            if (optimizeAngleLabel.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(new Rect(Event.current.mousePosition, Vector2.one * 20f), MouseCursor.SlideArrow);
            }
            ivyParametersGUI.optAngleBias.value = EditorGUI.FloatField(new Rect(275, YSpace + 35f, 50f, 20f), "",
                ivyParametersGUI.optAngleBias, windowSkin.GetStyle("textfield"));

            YSpace += 65f;

            YSpace += 15f;
            EditorGUI.DrawRect(new Rect(0f, YSpace, generalArea.width, 2f), bgColor);
            YSpace += 20f;

            GUI.Label(new Rect(80f, YSpace - 20f, 200f, 40f), "Save ivy", windowSkin.label);
            if (GUI.Button(new Rect(10f, YSpace + 20f, 90f, 40f), "Save into Scene", windowSkin.button))
            {
                CheckRestrictions(SaveCurrentIvyIntoScene);
            }
            if (GUI.Button(new Rect(110f, YSpace + 20f, 90f, 40f), "Save as prefab", windowSkin.button))
            {
                CheckRestrictions(SaveAsPrefab);
            }

            EditorGUI.DrawRect(new Rect(209f, YSpace - 5f, 2f, 75f), bgColor);

            GUI.Label(new Rect(245f, YSpace - 20f, 200f, 40f), "Convert to runtime Ivy", windowSkin.label);
            if (GUI.Button(new Rect(220f, YSpace + 20f, 90f, 40f), "Runtime Procedural", windowSkin.button))
            {
                CheckRestrictions(PrepareRuntimeProcedural);
            }

            if (GUI.Button(new Rect(320f, YSpace + 20f, 90f, 40f), "Runtime Baked", windowSkin.button))
            {
                CheckRestrictions(PrepareRuntimeBaked);
            }
            YSpace += 90f;

            GUILayout.EndArea();
        }

        private void CheckRestrictions(Action action)
        {
            if (controller.currentIvyInfo == null)
            {
                UIUtils.NoIvySelectedLogMessage();
            }
            else if (controller.infoPool.growth.growing)
            {
                UIUtils.CannotEditGrowingIvy();
            }
            else
            {
                action();
            }
        }

        private void CheckIvySelectedBeforeAction(Action action)
        {
            if (controller.currentIvyInfo == null)
            {
                UIUtils.NoIvySelectedLogMessage();
            }
            else
            {
                action();
            }
        }

        private void Randomize()
        {
            controller.infoPool.ivyParameters.randomSeed = System.Environment.TickCount;
            UnityEngine.Random.InitState(controller.infoPool.ivyParameters.randomSeed);
        }

        private void Reset()
        {
            controller.infoPool.growth.growing = false;
            controller.ResetIvy();
        }

        private void Optimize()
        {
            controller.Optimize();
        }

        private void SaveCurrentIvyIntoScene()
        {
            if (!controller.ivyGO.GetComponent<RTIvy>())
            {
                Action confirmCallback = () =>
                {
                    controller.SaveCurrentIvyIntoScene();
                };

                CustomDisplayDialog.Init(windowSkin, Constants.CONFIRM_SAVE_IVY, "Save ivy into scene", RealIvyWindow.infoTex, 370f, 155f, confirmCallback, true);
            }
        }

        private void SaveAsPrefab()
        {
            if (!controller.ivyGO.GetComponent<RTIvy>())
            {
                Action confirmCallback = () =>
                {

                    string fileName = controller.ivyGO.name;
                    controller.SaveCurrentIvyAsPrefab(fileName);
                };

                CustomDisplayDialog.Init(windowSkin, Constants.CONFIRM_SAVE_IVY, "Save ivy into scene", RealIvyWindow.infoTex, 370f, 155f, confirmCallback, true);
            }
        }

        private void PrepareRuntimeProcedural()
        {
            Reset();
            controller.PrepareRuntimeProcedural();
        }

        private void PrepareRuntimeBaked()
        {
            controller.PrepareRuntimeBaked();
        }

        private void StartStopGrowth()
        {
            if (controller.ivyGO)
            {
                controller.StartIvy(controller.infoPool.ivyContainer.ivyGO.transform.position, -controller.infoPool.ivyContainer.ivyGO.transform.up);
            }
            if (!controller.infoPool.growth.growing)
            {
                controller.SaveIvy();
            }
            controller.infoPool.growth.growing = !controller.infoPool.growth.growing;
        }
    }
}