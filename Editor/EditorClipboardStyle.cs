using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Daancode.Utils
{
    internal class Icons
    {
        public const string CloseIcon = "winbtn_win_close";
        public const string SettingsIcon = "Settings";
        public const string UnselectIcon = "scenepicking_notpickable_hover";
        public const string SaveIcon = "SaveAs";
        public const string Accept = "Valid";
        public const string ObjectIcon = "GameObject Icon";
        public const string Sort = "AlphabeticalSorting";
        public const string OpenAsset = "Import";
        public const string Trash = "TreeEditor.Trash";
    }

    internal class EditorClipboardGUI
    {
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

        public class BackgroundColorScope : System.IDisposable
        {
            private Color _originalColor;

            public BackgroundColorScope(Color color)
            {
                _originalColor = UnityEngine.GUI.backgroundColor;
                UnityEngine.GUI.backgroundColor = color;
            }

            public void Dispose()
            {
                UnityEngine.GUI.backgroundColor = _originalColor;
            }
        }
    }

    internal class EditorClipboardStyle : MonoBehaviour
    {
        private static GUIStyle _elementStyle = null;
        private static GUIStyle _noMarginsStyle = null;

        public static GUILayoutOption Width => GUILayout.Width(20f);

        public static GUIContent ToolbarPlusIcon => GetIcon("Toolbar Plus More");
        public static GUIContent SettingsIcon => GetIcon(Icons.SettingsIcon);
        public static GUIContent CloseIcon => GetIcon(Icons.CloseIcon);
        public static GUIContent AcceptIcon => GetIcon(Icons.Accept);
        public static GUIContent SortIcon => GetIcon(Icons.Sort);
        public static GUIContent OpenAssetIcon => GetIcon(Icons.OpenAsset);
        public static GUIContent TrashIcon => GetIcon(Icons.Trash);

        public static GUIStyle NoMargins
        {
            get
            {
                _noMarginsStyle ??= new GUIStyle() { margin = new RectOffset(0, 0, 0, 0) };
                return _noMarginsStyle;
            }
        }

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
                image = title.IsAsset ? AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(obj)) : GetIcon("GameObject Icon").image,
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
    }
}