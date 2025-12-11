using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TeamCrescendo.ProceduralIvy
{
    public static class UIUtils
    {
        public static void DragAndDropArea(Rect area, Action<Object> onObjectDragged, Action onDragPerformed)
        {
            var evt = Event.current;
            var objects = new Object[0];
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!area.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        objects = DragAndDrop.objectReferences;
                        for (var i = 0; i < objects.Length; i++)
                            if (PrefabUtility.GetPrefabAssetType(objects[i]) != PrefabAssetType.NotAPrefab)
                                onObjectDragged(objects[i]);

                        onDragPerformed();
                        DragAndDrop.AcceptDrag();
                    }

                    break;
            }
        }

        private static bool IsSelectionCorrect<T>(Object[] objectsSelected)
        {
            var res = true;

            for (var i = 0; i < objectsSelected.Length; i++)
                if (!(objectsSelected[i] is T))
                {
                    res = false;
                    break;
                }

            return res;
        }

        public static T[] DragAndDropObjects<T>(Rect area, Action<T> onDragPerformed) where T : Object
        {
            var evt = Event.current;
            T[] res = default;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!area.Contains(evt.mousePosition))
                        return res;


                    var objectsDragged = DragAndDrop.objectReferences;

                    if (IsSelectionCorrect<T>(objectsDragged))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;


                        if (evt.type == EventType.DragPerform)
                        {
                            res = new T[objectsDragged.Length];
                            for (var i = 0; i < objectsDragged.Length; i++) res[i] = objectsDragged[i] as T;

                            if (onDragPerformed != null) onDragPerformed(res[0]);
                            DragAndDrop.AcceptDrag();
                        }
                    }

                    break;
            }

            return res;
        }

        public static void CustomIntFloatField(IvyParameter ivyParameter, float multiplier, string labelText,
            float width, float YSpace, float XSpace, GUISkin windowSkin, Action<IvyParameter, float> onDropDown)
        {
            var rect = new Rect(XSpace, YSpace, width, 20f);
            GUI.Label(new Rect(rect.x - 30f, rect.y, rect.width + 60f, rect.height), labelText,
                windowSkin.GetStyle("intfloatfieldlabel"));
            if (EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Keyboard, windowSkin.GetStyle("transparent")))
                if (onDropDown != null)
                    onDropDown(ivyParameter, multiplier);

            if (rect.Contains(Event.current.mousePosition))
                EditorGUIUtility.AddCursorRect(new Rect(Event.current.mousePosition, Vector2.one * 20f),
                    MouseCursor.SlideArrow);
            var value = EditorGUI.FloatField(new Rect(rect.x, rect.y + 20f, rect.width, 25f), "", ivyParameter.value,
                windowSkin.GetStyle("textfield"));
            multiplier = Mathf.Ceil(1f / multiplier);
            value = Mathf.Floor(value * multiplier) / multiplier;
            ivyParameter.value = value;
        }

        public static T CustomObjectField<T>(Rect area, T item,
            GUISkin skin, GUISkin oldSkin, Texture icon, int pickerID, string searchFilter,
            Action<T> onItemChanged, bool unsavedChanges) where T : Object
        {
            var res = CustomObjectField(area, item, skin, oldSkin, icon, pickerID, searchFilter, null, null,
                onItemChanged, unsavedChanges);
            return res;
        }

        public static T CustomObjectField<T>(Rect area, T item,
            GUISkin skin, GUISkin oldSkin, Texture icon, int pickerID, string searchFilter,
            Action<T> onDragPerformed, Action<T> onObjectPicked, Action<T> onItemChanged,
            bool unsavedChanges) where T : Object
        {
            T res;

            string itemName;
            var itemChanged = false;
            
            if (item)
                itemName = item.name;
            else
                itemName = "None";
            if (unsavedChanges) itemName += " *";

            var style = unsavedChanges ? skin.GetStyle("bold") : skin.GetStyle("transparent");

            GUI.Box(area, "", skin.GetStyle("objectfield"));
            if (GUI.Button(new Rect(area.x, area.y, area.width - 17f, area.height), "       " + itemName, style))
                EditorGUIUtility.PingObject(item);

            GUI.DrawTexture(new Rect(area.x, area.y, area.height, area.height), icon);


            var draggedObjects = DragAndDropObjects(area, onDragPerformed);
            if (draggedObjects != null && draggedObjects.Length > 0)
            {
                itemChanged = true;
                res = draggedObjects[0];
            }
            else
            {
                res = item;
            }


            GUI.skin = oldSkin;
            if (GUI.Button(new Rect(area.width - area.height, area.y, area.height, area.height), "",
                    skin.GetStyle("objectpicker")))
                EditorGUIUtility.ShowObjectPicker<T>(item, false, searchFilter, pickerID);
            if (Event.current.commandName == "ObjectSelectorUpdated")
                if (EditorGUIUtility.GetObjectPickerControlID() == pickerID)
                {
                    res = EditorGUIUtility.GetObjectPickerObject() as T;
                    itemChanged = true;
                    if (onObjectPicked != null) onObjectPicked(res);
                }

            GUI.skin = skin;


            if (itemChanged && onItemChanged != null) onItemChanged(res);

            return res;
        }

        public static string GetGUIDByAsset(Object asset)
        {
            var res = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            return res;
        }

        public static void ButtonChangesAlert(Rect area, string noChangesText, string changesText,
            GUIStyle noChangesStyle, GUIStyle changesStyle, bool unsavedChanges, Action clickAction)
        {
            var buttonText = unsavedChanges ? changesText : noChangesText;
            var buttonStyle = unsavedChanges ? changesStyle : noChangesStyle;

            if (GUI.Button(area, buttonText, buttonStyle)) clickAction();
        }

        public static void NoIvySelectedLogMessage()
        {
            Debug.Log(EditorConstants.NO_IVY_SELECTED_MESSAGE);
        }

        public static void CannotEditGrowingIvy()
        {
            Debug.Log(EditorConstants.CANNOT_EDIT_GROWING_IVY);
        }
        
        public static void CannotConvertToRuntimeIvy()
        {
            Debug.Log(EditorConstants.CANNOT_CONVERT_TO_RUNTIME_IVY);
        }
    }
}