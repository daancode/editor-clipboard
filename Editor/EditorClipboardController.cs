using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Daancode.Utils
{
    public class EditorClipboardController : System.IDisposable
    {
        private readonly List<string> _categories = new List<string>();
        private readonly List<EditorClipboardData> _clipboards = new List<EditorClipboardData>();
        private readonly List<Object> _selection = new List<Object>();


        public EditorClipboardData this[int index] => index >= 0 && index < _clipboards.Count ? _clipboards[index] : null;
        public int ClipboardsCount => _clipboards.Count;

        public string SelectedCategory { get; set; } = string.Empty;

        public void Initialize()
        {
            _categories.Clear();
            _categories.AddRange(EditorClipboardData.LoadCategories());

            for(var i = 0; i < _categories.Count; ++i)
            {
                _clipboards.Add(new EditorClipboardData(_categories[i]));
            }
        }

        public void Dispose()
        {
            Save();
        }

        public void Save(bool force = false)
        {
            for (var i = 0; i < _clipboards.Count; ++i)
            {
                _clipboards[i].Save(force);
            }

            EditorClipboardData.SaveCategories(_categories);
        }

        public bool AddCategory(string categoryName)
        {
            if(_categories.Contains(categoryName))
            {
                Debug.LogError($"Category '{categoryName}' already exist.");
                return false;
            }

            _categories.Add(categoryName);
            _clipboards.Add(new EditorClipboardData(categoryName));
            Save();
            return true;
        }

        public bool IsSelected(Object obj)
        {
            return _selection.Contains(obj);
        }

        public void SortSelectedCategory()
        {
            if (string.IsNullOrEmpty(SelectedCategory))
            {
                return;
            }

            var data = _clipboards.Find(clipboard => clipboard.Category == SelectedCategory);
            if(data == null)
            {
                return;
            }

            data.Sort();
        }

        public void Select(Object obj, bool rightClick = false)
        {
            SelectedCategory = string.Empty;

            if (!rightClick)
            {
                ClearSelected();
                Selection.SetActiveObjectWithContext(obj, null);
                return;
            }

            if(_selection.Contains(obj))
            {
                _selection.Remove(obj);
            }
            else
            {
                _selection.Add(obj);
            }

            Selection.objects = _selection.ToArray();
        }

        public void ClearSelected()
        {
            if(_selection.Count == 0)
            {
                return;
            }

            _selection.Clear();
            Selection.objects = _selection.ToArray();
        }

        public void ClearRemoveQueue()
        {

        }
    }
}
