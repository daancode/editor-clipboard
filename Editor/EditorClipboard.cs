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
        private class SerializableList
        {
            public List<Object> Objects = new List<Object>();
        }

        private const string PREFS_KEY = "daancode:editor_clipboard";

        private readonly SerializableList _serializableList = new SerializableList();
        private readonly List<Object> _selected = new List<Object>();
        private readonly List<Object> _toRemove = new List<Object>();
        private Vector2 _scrollPosition = Vector2.zero;

        [MenuItem("Window/Editor Clipboard", false, 1000)]
        public static void OpenWindow()
        {
            var window = GetWindow<EditorClipboard>();
            window.titleContent = new GUIContent
            {
                text = " Clip",
                image = EditorGUIUtility.IconContent("Clipboard").image
            };
            window.minSize = new Vector2(120, 220);
            window.maxSize = new Vector2(120, 4000);
            window.Show();
        }

        private void OnEnable()
        {
            LoadObjectsFromPrefs();
        }

        private void SaveObjectsInPrefs()
        {
            var serializedObjects = JsonUtility.ToJson(_serializableList);
            EditorPrefs.SetString(PREFS_KEY, serializedObjects);
            Debug.Log($"Saved {_serializableList.Objects.Count} objects in editor prefs.");
        }

        private void LoadObjectsFromPrefs()
        {
            if (!EditorPrefs.HasKey(PREFS_KEY))
            {
                return;
            }
            
            var objects = EditorPrefs.GetString(PREFS_KEY);
            JsonUtility.FromJsonOverwrite(objects, _serializableList);
        }

        private void OnGUI()
        {
            OnToolbarGUI();
            var rect = EditorGUILayout.BeginVertical();
            
            if(_serializableList.Objects.Count == 0)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Drop here...", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUIStyle.none, GUIStyle.none);
            EditorGUIUtility.SetIconSize(new Vector2(13f, 13f));
            for (var i = 0; i < _serializableList.Objects.Count; ++i)
            {
                var obj = _serializableList.Objects[i];
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
                    _serializableList.Objects.Remove(_toRemove[i]);
                }
                _toRemove.Clear();
            }
        }

        private void OnToolbarGUI()
        {
            var style = EditorStyles.toolbarButton;
            var width = GUILayout.Width(30f);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_SaveAs"), style, width))
            {
                SaveObjectsInPrefs();
            }

            if (_selected.Count > 0)
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_scenepicking_notpickable_hover"), EditorStyles.toolbarButton, width))
                {
                    ClearSelected();
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_TreeEditor.Trash"), EditorStyles.toolbarButton, width))
                {
                    _toRemove.AddRange(_selected);
                    ClearSelected();
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Settings"), style, width))
            {
                ShowSettingsMenu();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnElementGUI(Object obj)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            var content = new GUIContent()
            {
                text = GetObjectName(obj),
                image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(obj)),
                tooltip = obj.name
            };
                
            var style = new GUIStyle(EditorStyles.miniButtonLeft)
            {
                fontSize = 10, 
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
                
            var color = UnityEngine.GUI.color;
            UnityEngine.GUI.color = _selected.Contains(obj) ? new Color(0f, .5f, 1f, .5f) : color;
            if (GUILayout.Button(content, style))
            {
                if (Event.current.button == 1)
                {
                    if(_selected.Contains(obj))
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
            UnityEngine.GUI.color = color;
            
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_winbtn_win_close"), EditorStyles.miniButtonRight, GUILayout.Width(20f)))
            {
                _toRemove.Insert(0, obj);
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
                if (_serializableList.Objects.Contains(obj))
                {
                    continue;
                }
                
                _serializableList.Objects.Add(obj);
            }
        }

        private string GetObjectName(Object obj)
        {
            var textSize = UnityEngine.GUI.skin.label.CalcSize(new GUIContent(obj.name));
            if (textSize.x + 35f < position.width)
            {
                return obj.name;
            }

            var ratio = (position.width - 35f) / textSize.x;
            var availableCharacters = Mathf.Clamp(Mathf.FloorToInt(obj.name.Length * ratio), 3, obj.name.Length);
            return obj.name.Length > availableCharacters ? obj.name.Substring(0, availableCharacters - 3) + "..." : obj.name;
        }
        
        private void ShowSettingsMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Load from Prefs"), false, LoadObjectsFromPrefs);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Clear"), false, _serializableList.Objects.Clear);
            menu.AddItem(new GUIContent("Remove from Prefs"), false, () => EditorPrefs.DeleteKey(PREFS_KEY));
            menu.ShowAsContext();
        }

        private void ClearSelected()
        {
            _selected.Clear();
            Selection.objects = null;
        }
    }
}
