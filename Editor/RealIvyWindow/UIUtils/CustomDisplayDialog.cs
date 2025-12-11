using System;
using UnityEditor;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
    public class CustomDisplayDialog : EditorWindow
    {
        private Action confirmCallback;
        private Action rejectCallback;

        private GUISkin _windowSkin;
        private Texture2D _icon;
        private string _text;
        private bool _hasCancelButton;

        public static void Init(GUISkin windowSkin, string text, string title, Texture2D icon, float windowWidth, float windowHeight, Action confirmCallback, Action rejectCallback, bool hasCancelButton)
        {
            CustomDisplayDialog previousDialog = GetWindow<CustomDisplayDialog>();
            if (previousDialog != null)
            {
                previousDialog.Close();
            }

            CustomDisplayDialog dialog = ScriptableObject.CreateInstance<CustomDisplayDialog>();

            dialog.SetValues(windowSkin, text, icon, windowWidth, windowHeight, confirmCallback, rejectCallback, hasCancelButton);
            dialog.ShowUtility();
            dialog.titleContent = new GUIContent(title);
        }


        public static void Init(GUISkin windowSkin, string text, string title, Texture2D icon, float windowWidth, float windowHeight, bool hasCancelButton)
        {
            Init(windowSkin, text, title, icon, windowWidth, windowHeight, null, null, hasCancelButton);
        }

        public static void Init(GUISkin windowSkin, string text, string title, Texture2D icon, float windowWidth, float windowHeight, Action confirmCallback, bool hasCancelButton = false)
        {
            Init(windowSkin, text, title, icon, windowWidth, windowHeight, confirmCallback, null, hasCancelButton);
        }

        public void SetValues(GUISkin windowSkin, string text, Texture2D icon, float windowWidth, float windowHeight, Action confirmCallback, Action rejectCallback, bool hasCancelButton)
        {
            _windowSkin = windowSkin;
            _icon = icon;
            _text = text;
            _hasCancelButton = hasCancelButton;


            this.confirmCallback = confirmCallback;
            this.rejectCallback = rejectCallback;

            this.minSize = new Vector2(windowWidth, windowHeight);
            this.maxSize = this.minSize;
        }

        private void OnGUI()
        {
            Focus();
            EditorGUI.DrawRect(new Rect(0f, 0f, this.position.width, this.position.height), new Color(0.45f, 0.45f, 0.45f));
            Rect confirmActionRect = new Rect(10f, 10f, this.position.width - 20f, this.position.height - 20f);

            GUILayout.BeginArea(confirmActionRect);
            GUI.Label(new Rect(74f, 0f, confirmActionRect.width - 74f, confirmActionRect.height - 30f),
            _text,
            _windowSkin.GetStyle("text"));
            GUI.DrawTexture(new Rect(15f, 20f, 54f, 54f), _icon);

            Rect confirmButtonRect = _hasCancelButton ?
                new Rect(confirmActionRect.width / 2f - 110f, confirmActionRect.height - 20f, 100f, 20f) :
                new Rect(confirmActionRect.width / 2f - 50f, confirmActionRect.height - 20f, 100f, 20f);

            if (GUI.Button(confirmButtonRect, "Accept", _windowSkin.GetStyle("button")))
            {
                Confirm();
            }
            if (_hasCancelButton)
            {
                if (GUI.Button(new Rect(confirmActionRect.width / 2f + 10f, confirmActionRect.height - 20f, 100f, 20f), "Cancel", _windowSkin.GetStyle("button")))
                {
                    Reject();
                }
            }
            GUILayout.EndArea();
        }

        private void Confirm()
        {
            if (confirmCallback != null)
            {
                confirmCallback();
            }

            Close();
        }

        private void Reject()
        {
            if (rejectCallback != null)
            {
                rejectCallback();
            }

            Close();
        }

        void OnLostFocus()
        {
            Focus();
        }
    }
}