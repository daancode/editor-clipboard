using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Daancode.Utils
{
    public class EditorClipboardData
    {
        private static StringBuilder Serializer = new StringBuilder();

        public const string KEY_PREFIX = "daancode:editor_clipboard";
        public const string CATEGORIES_KEY = KEY_PREFIX + ":___categories";

        private string _category = string.Empty;
        private List<Object> _objects = new List<Object>();
        private List<Object> _objectsToRemove = new List<Object>();
        private bool _isDirty = false;

        public string Key => $"{KEY_PREFIX}:{_category}";
        public string Category => _category;

        public bool IsFolded { get; set; } = false;

        public Object this[int index] => index >= 0 && index < _objects.Count ? _objects[index] : null;
        public int Count => _objects.Count;

        public EditorClipboardData(string category)
        {
            _category = category;

            if(EditorPrefs.HasKey(Key))
            {
                Load();
            }
            else
            {
                _isDirty = true;
            }
        }

        public void Add(List<Object> objects)
        {
            foreach(var asset in objects.Where(obj => obj != null && !_objects.Contains(obj)))
            {
                _objects.Add(asset);
                _isDirty = true;
            }
        }

        public void Remove(Object asset)
        {
            if(_objectsToRemove.Contains(asset))
            {
                return;
            }

            _objectsToRemove.Add(asset);
            _isDirty = true;
        }

        public void TryRemove()
        {
            _objects.RemoveAll(obj => _objectsToRemove.Contains(obj));
            _objectsToRemove.Clear();
        }

        public void Remove()
        {
            EditorPrefs.DeleteKey(Key);
            _objectsToRemove.AddRange(_objects);
            _isDirty = true;
        }

        public void SaveIfDirty()
        {
            if(!_isDirty)
            {
                return;
            }

            Save();
        }

        public void Load()
        {
            if (!EditorPrefs.HasKey(Key))
            {
                return;
            }

            var serializedObjects = EditorPrefs.GetString(Key).Split(';');

            for (var i = 0; i < serializedObjects.Length; ++i)
            {
                var assetGUID = serializedObjects[i];
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset == null || _objects.Contains(asset))
                {
                    continue;
                }

                _objects.Add(asset);
            }
        }

        public void Save(bool force = false)
        {
            if (string.IsNullOrEmpty(_category) || (!_isDirty && !force))
            {
                return;
            }

            TryRemove();

            Serializer.Clear();
            foreach (var asset in _objects.Where(obj => obj != null))
            {
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
                Serializer.Append(Serializer.Length > 0 ? ";" : "").Append(guid);
            }
            EditorPrefs.SetString(Key, Serializer.ToString());
            Serializer.Clear();
        }

        public void Sort()
        {
            _objects = _objects.OrderBy(obj => obj.name).ToList();
            _isDirty = true;
        }

        public static List<string> LoadCategories()
        {
            if(!EditorPrefs.HasKey(CATEGORIES_KEY))
            {
                SaveCategories(new List<string>() { "Default" });
            }

            return EditorPrefs.GetString(CATEGORIES_KEY).Split(';').ToList();
        }

        public static void SaveCategories(List<string> categories)
        {
            if (categories.Count == 0)
            {
                return;
            }

            Serializer.Clear();
            foreach(var category in categories.Where(c => c != null))
            {
                Serializer.Append(Serializer.Length > 0 ? ";" : string.Empty).Append(category);
            }
            EditorPrefs.SetString(CATEGORIES_KEY, Serializer.ToString());
            Serializer.Clear();
        }
    }
}
