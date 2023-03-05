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
using UnityEngine.Events;

namespace Daancode.Utils
{
    public class EditorClipboardWindow : EditorWindow
    {
        [SerializeField] private List<bool> _foldedInfo = new List<bool>();

        private bool _addCategoryMode = false;
        private string _newCategoryName = "New Category";

        private Vector2 _scrollPosition = Vector2.zero;

        private readonly EditorClipboardController _controller = new EditorClipboardController();
        private readonly EditorClipboardView _view = new EditorClipboardView();

        [MenuItem("Window/Editor Clipboard", false, 1000)]
        public static void OpenWindow()
        {
            var window = GetWindow<EditorClipboardWindow>();
            window.titleContent = new GUIContent
            {
                text = string.Empty,
                image = EditorClipboardStyle.GetIcon("Favorite").image
            };
            window.minSize = new Vector2(120, 220);
            window.maxSize = new Vector2(120, 4000);
            window.Show();
        }

        private void OnEnable()
        {
            _controller.Initialize();
            _view.Initialize(_controller);

            wantsMouseMove = true;
        }

        private void OnDisable()
        {
            _controller.Save();
        }

        private void OnGUI()
        {
            OnToolbarGUI();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUIStyle.none, GUIStyle.none);
            EditorGUIUtility.SetIconSize(new Vector2(13f, 13f));

            for (var i = 0; i < _controller.ClipboardsCount; ++i)
            {
                var clipboard = _controller[i];
                _view?.OnGUI(position, clipboard);
                clipboard.TryRemove();
            }

            GUILayout.FlexibleSpace();

            EditorGUIUtility.SetIconSize(Vector2.zero);
            EditorGUILayout.EndScrollView();
        }

        private void OnToolbarGUI()
        {
            if (_addCategoryMode)
            {
                OnAddingCategoryGUI();
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if(GUILayout.Button(EditorClipboardStyle.ToolbarPlusIcon, EditorStyles.toolbarButton))
            {
                ShowSettingsMenu();
            }
            GUILayout.FlexibleSpace();

            if(!string.IsNullOrEmpty(_controller.SelectedCategory))
            {
                if (GUILayout.Button(EditorClipboardStyle.SortIcon, EditorStyles.toolbarButton))
                {
                    _controller.SortSelectedCategory();
                }

                if (GUILayout.Button(EditorClipboardStyle.TrashIcon, EditorStyles.toolbarButton) &&
                    EditorUtility.DisplayDialog("Remove Category", "Are you sure?", $"Remove {_controller.SelectedCategory}", "Cancel"))
                {
                    _controller.RemoveCategory(_controller.SelectedCategory);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OnAddingCategoryGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(EditorClipboardStyle.CloseIcon, EditorStyles.toolbarButton))
                {
                    _addCategoryMode = false;
                }

                _newCategoryName = EditorGUILayout.TextField(_newCategoryName, EditorStyles.toolbarTextField);   

                if (GUILayout.Button(EditorClipboardStyle.AcceptIcon, EditorStyles.toolbarButton))
                {
                    TryAddCategory();
                }
            }

            void TryAddCategory()
            {
                if(_controller.AddCategory(_newCategoryName))
                {
                    _newCategoryName = "New Category";
                    _addCategoryMode = false;
                }
            }
        }


        private void ShowSettingsMenu()
        {
            DeselectCategory();
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add category..."), false, () => _addCategoryMode = !_addCategoryMode);
            menu.ShowAsContext();
        }


        private (string, bool) GetObjectName(Object obj)
        {
            if (obj == null)
            {
                return ("Unknown", false);
            }

            var textSize = UnityEngine.GUI.skin.label.CalcSize(new GUIContent(obj.name));
            if (textSize.x + 35f < position.width)
            {
                return (obj.name, AssetDatabase.Contains(obj));
            }

            var ratio = ( position.width - 35f ) / textSize.x;
            var availableCharacters = Mathf.Clamp(Mathf.FloorToInt(obj.name.Length * ratio), 3, obj.name.Length);
            return (obj.name.Length > availableCharacters ? obj.name.Substring(0, availableCharacters - 3) + "..." : obj.name, AssetDatabase.Contains(obj));
        }

        private void DeselectCategory()
        {
            _controller.SelectedCategory = string.Empty;
            Repaint();
        }
    }
}
