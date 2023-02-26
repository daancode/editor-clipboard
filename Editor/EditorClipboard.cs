//  MIT License

//  Copyright(c) 2023 Damian Barczynski

//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:

//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.

//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

// Repository: https://github.com/daancode/editor-clipboard

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Daancode.Utils
{
    public class EditorClipboard : EditorWindow
    {
        [System.Serializable]
        private class ClipboardData
        {
            public const string KEY = "daancode:editor_clipboard";

            public List<string> Assets = new List<string>();
            public bool AutoSave = false;
        }
       
        private ClipboardData _clipboardData = new ClipboardData();
        private readonly List<Object> _clipboard = new List<Object>();
        private readonly List<Object> _selected = new List<Object>();
        private readonly List<Object> _toRemove = new List<Object>();
        private Vector2 _scrollPosition = Vector2.zero;

        [MenuItem("Window/Editor Clipboard", false, 1000)]
        public static void OpenWindow()
        {
            var window = GetWindow<EditorClipboard>();
            window.titleContent = new GUIContent
            {
                text = string.Empty,
                image = Style.GetIcon("Favorite").image
            };
            window.minSize = new Vector2(120, 220);
            window.maxSize = new Vector2(120, 4000);
            window.Show();
        }

        private void OnEnable()
        {
            LoadObjectsFromPrefs();
        }

        private void SaveObjectsInPrefs(bool notify = true)
        {
            _clipboardData.Assets.Clear();
            for (var i = 0; i < _clipboard.Count; ++i)
            {
                var asset = _clipboard[i];
                if(asset == null)
                {
                    continue;
                }

                var assetPath = AssetDatabase.GetAssetPath(asset);
                _clipboardData.Assets.Add(AssetDatabase.AssetPathToGUID(assetPath));
            }

            EditorPrefs.SetString(ClipboardData.KEY, JsonUtility.ToJson(_clipboardData));

            if(notify)
            {
                ShowNotification(new GUIContent($"Saved."));
            }
        }

        private void LoadObjectsFromPrefs(bool notify = true)
        {
            if (!EditorPrefs.HasKey(ClipboardData.KEY))
            {
                return;
            }

            _clipboardData.Assets.Clear();
            var serializedAssets = EditorPrefs.GetString(ClipboardData.KEY);
            JsonUtility.FromJsonOverwrite(serializedAssets, _clipboardData);

            for(var i = 0; i < _clipboardData.Assets.Count; ++i)
            {
                var assetGUID = _clipboardData.Assets[i];
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if(asset == null || _clipboard.Contains(asset))
                {
                    continue;
                }

                _clipboard.Add(asset);
            }

            if (notify)
            {
                ShowNotification(new GUIContent($"Loaded."));
            }
        }

        private void OnGUI()
        {
            OnToolbarGUI();
            var rect = EditorGUILayout.BeginVertical();
            
            if(_clipboard.Count == 0)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Drop here...", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUIStyle.none, GUIStyle.none);
            EditorGUIUtility.SetIconSize(new Vector2(13f, 13f));
            for (var i = 0; i < _clipboard.Count; ++i)
            {
                var obj = _clipboard[i];

                if(obj == null)
                {
                    _toRemove.Add(obj);
                    continue;
                }

                OnElementGUI(obj);
            }
            EditorGUIUtility.SetIconSize(Vector2.zero);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            HandleDragAndDrop(rect);
            
            if (_toRemove != null && _toRemove.Count > 0)
            {
                for (var i = 0; i < _toRemove.Count; ++i)
                {
                    _clipboard.Remove(_toRemove[i]);
                }
                _toRemove.Clear();
                
                if(_clipboardData.AutoSave)
                {
                    SaveObjectsInPrefs(false);
                }
            }
        }

        private void OnToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(Style.SettingsIcon, EditorStyles.toolbarButton, Style.Width))
            {
                ShowSettingsMenu();
            }

            GUILayout.FlexibleSpace();

            if (!_clipboardData.AutoSave && GUILayout.Button(Style.SaveIcon, EditorStyles.toolbarButton, Style.Width))
            {
                SaveObjectsInPrefs();
            }

            if (_selected.Count > 0)
            {
                if (GUILayout.Button(Style.UnselectIcon, EditorStyles.toolbarButton, Style.Width))
                {
                    ClearSelected();
                }
                if (GUILayout.Button(Style.CloseIcon, EditorStyles.toolbarButton, Style.Width))
                {
                    _toRemove.AddRange(_selected);
                    ClearSelected();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OnElementGUI(Object obj)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

            using(new Style.ColorScope(_selected.Contains(obj) ? new Color(0f, .5f, 1f, .5f) : UnityEngine.GUI.color))
            {
                if (GUILayout.Button(Style.GetElementContent(obj, GetObjectName(obj)), Style.ElementStyle))
                {
                    if (Event.current.button == 1)
                    {
                        if (_selected.Contains(obj))
                        {
                            _selected.Remove(obj);
                        }
                        else
                        {
                            _selected.Add(obj);
                        }
                        Selection.objects = _selected.ToArray();
                    }
                    else
                    {
                        ClearSelected();
                        Selection.SetActiveObjectWithContext(obj, null);
                    }
                }
            }

            if (GUILayout.Button(Style.CloseIcon, EditorStyles.miniButtonRight, GUILayout.Width(20f)))
            {
                _toRemove.Add(obj);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void HandleDragAndDrop(Rect rect)
        {
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
                    OnObjectDropped(DragAndDrop.objectReferences);
                    Event.current.Use();
                    return;
                }
            }
        }
        
        private void OnObjectDropped(Object[] objects)
        {
            for (var i = 0; i < objects.Length; ++i)
            {
                var obj = objects[i];
                if (_clipboard.Contains(obj))
                {
                    continue;
                }

                _clipboard.Insert(0, obj);
            }

            if (_clipboardData.AutoSave)
            {
                SaveObjectsInPrefs(false);
            }
        }

        private (string, bool) GetObjectName(Object obj)
        {
            if(obj == null)
            {
                return ("Unknown", false);
            }

 
            var textSize = UnityEngine.GUI.skin.label.CalcSize(new GUIContent(obj.name));
            if (textSize.x + 35f < position.width)
            {
                return (obj.name, AssetDatabase.Contains(obj));
            }

            var ratio = (position.width - 35f) / textSize.x;
            var availableCharacters = Mathf.Clamp(Mathf.FloorToInt(obj.name.Length * ratio), 3, obj.name.Length);
            return (obj.name.Length > availableCharacters ? obj.name.Substring(0, availableCharacters - 3) + "..." : obj.name, AssetDatabase.Contains(obj));
        }
        
        private void ShowSettingsMenu()
        {
            var menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent("Editor Clipboard"));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Auto Save"), _clipboardData.AutoSave, () => _clipboardData.AutoSave = !_clipboardData.AutoSave);
            menu.AddSeparator(string.Empty);
            if(_clipboardData.AutoSave)
            {
                menu.AddItem(new GUIContent("Save"), false, () => SaveObjectsInPrefs());
            }
            menu.AddItem(new GUIContent("Load"), false, () => LoadObjectsFromPrefs());
            menu.AddItem(new GUIContent("Clear"), false, () => { _clipboard.Clear(); ClearSelected(); });
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Delete Prefs"), false, () => EditorPrefs.DeleteKey(ClipboardData.KEY));
            menu.ShowAsContext();
        }

        private void ClearSelected()
        {
            _selected.Clear();
            Selection.objects = null;
        }

        private static class Style
        {
            private static GUIStyle _elementStyle = null;

            public static GUILayoutOption Width => GUILayout.Width(30f);

            public static GUIContent CloseIcon => GetIcon("winbtn_win_close");
            public static GUIContent SettingsIcon => GetIcon("Settings");
            //public static GUIContent TrashIcon => Style.GetIcon("TreeEditor.Trash");
            public static GUIContent UnselectIcon => Style.GetIcon("scenepicking_notpickable_hover");
            public static GUIContent SaveIcon => Style.GetIcon("SaveAs");
            public static GUIContent ObjectIcon => Style.GetIcon("GameObject Icon");

            public static GUIStyle ElementStyle
            {
                get
                {
                    _elementStyle ??= new GUIStyle(EditorStyles.miniButtonLeft)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip
                    };

                    return _elementStyle;
                }
            }

            public static GUIContent GetElementContent(Object obj, (string Text, bool IsAsset) title)
            {
                return new GUIContent()
                {
                    text = title.Text,
                    image = title.IsAsset ? AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(obj)) : ObjectIcon.image,
                    tooltip = title.IsAsset ? obj.name : obj.name + " - Not an asset, can't be saved"
                };
            }

            public static GUIContent GetIcon(string iconId)
            {
                if (iconId.StartsWith("d_"))
                {
                    iconId = iconId.Substring(2);
                }

                return EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? $"d_{iconId}" : iconId);
            }

            public class ColorScope : System.IDisposable
            {
                private Color _originalColor;

                public ColorScope(Color color)
                {
                    _originalColor = UnityEngine.GUI.color;
                    UnityEngine.GUI.color = color;
                }

                public void Dispose()
                {
                    UnityEngine.GUI.color = _originalColor;
                }
            }
        }
    }
}
