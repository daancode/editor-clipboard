using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Daancode.Utils
{
    public class EditorClipboardView
    {
        private EditorClipboardController _controller;
        private EditorClipboardData _data;

        public void Initialize(EditorClipboardController controller)
        {
            _controller = controller;
        }

        public void OnGUI(Rect position, EditorClipboardData data)
        {
            if(data == null)
            {
                return;
            }

            _data = data;

            var rect = EditorGUILayout.BeginVertical(EditorClipboardStyle.NoMargins);
            var isCategorySelected = _data.Category == _controller.SelectedCategory;

            var headerRect = EditorGUILayout.BeginHorizontal(EditorClipboardStyle.NoMargins, GUILayout.ExpandWidth(false));

            if (isCategorySelected)
            {
                EditorGUI.DrawRect(headerRect, isCategorySelected ? new Color(0f, 0.8f, 1f, 0.1f) : new Color(0f, 0f, 0f, 0.1f));
            }
 
            EditorGUI.BeginDisabledGroup(_data.Count == 0);
            if (GUILayout.Button(EditorClipboardStyle.GetIcon($"Toolbar {(_data.IsFolded ? "Plus" : "Minus")}"), EditorStyles.miniButton, GUILayout.Width(20f)))
            {
                _data.IsFolded = !_data.IsFolded;
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            var buttonRect = new Rect(headerRect.x + 25f, headerRect.y, headerRect.width, headerRect.height);
            if (GUI.Button(buttonRect, _data.Category, new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleLeft }))
            {
                _controller.SelectedCategory = isCategorySelected ? string.Empty : _data.Category;
            }

            EditorGUILayout.EndHorizontal();

            if(!_data.IsFolded)
            {
                for (var i = 0; i < _data.Count; ++i)
                {
                    OnElementGUI(_data[i], position);
                }
            }

            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height, position.width, 1f), new Color(0f, 0f, 0f, 0.5f));
            EditorGUILayout.EndVertical();
            GUILayout.Space(1f);
            HandleDragAndDrop(rect);

            _data = null;
        }

        private void OnElementGUI(Object obj, Rect position)
        {
            if(obj == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            var isSelected = _controller.IsSelected(obj);

            using (new EditorClipboardGUI.ColorScope(isSelected ? new Color(0f, .5f, 1f, .5f) : UnityEngine.GUI.color))
            {
                if (GUILayout.Button(EditorClipboardStyle.GetElementContent(obj, GetObjectName(obj, position)), EditorClipboardStyle.ElementStyle))
                {
                    _controller?.Select(obj, Event.current.button == 1);
                }
            }

            if(isSelected)
            {
                if(GUILayout.Button(EditorClipboardStyle.OpenAssetIcon, EditorStyles.miniButtonRight, EditorClipboardStyle.Width))
                {
                    AssetDatabase.OpenAsset(obj);
                    _controller.ClearSelected();
                }
            }
            else if (GUILayout.Button(EditorClipboardStyle.GetIcon(Icons.CloseIcon), EditorStyles.miniButtonRight, EditorClipboardStyle.Width))
            {
                _data.Remove(obj);
            }

            EditorGUILayout.EndHorizontal();
        }

        private (string, bool) GetObjectName(Object obj, Rect position)
        {
            if (obj == null)
            {
                return ("Unknown", false);
            }


            var textSize = UnityEngine.GUI.skin.label.CalcSize(new GUIContent(obj.name));
            if (textSize.x + 45f < position.width)
            {
                return (obj.name, AssetDatabase.Contains(obj));
            }

            var ratio = ( position.width - 45f ) / textSize.x;
            var availableCharacters = Mathf.Clamp(Mathf.FloorToInt(obj.name.Length * ratio), 3, obj.name.Length);
            return (obj.name.Length > availableCharacters ? obj.name.Substring(0, availableCharacters - 3) + "..." : obj.name, AssetDatabase.Contains(obj));
        }

        private void HandleDragAndDrop(Rect rect)
        {
            if(_data == null)
            {
                return;
            }

            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.3f));
            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform when rect.Contains(Event.current.mousePosition):
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (Event.current.type != EventType.DragPerform)
                    {
                        Event.current.Use();
                        break;
                    }

                    DragAndDrop.AcceptDrag();
                    _data.Add(DragAndDrop.objectReferences.ToList());
                    Event.current.Use();
                    return;
                }
            }
        }
    }
}