using UnityEditor;
using UnityEngine;

namespace TheFoundation.Editor
{
    public class ImagePreviewWindow : EditorWindow
    {
        private Texture2D _image;

        public static void Show(Texture2D image)
        {
            var w = GetWindow<ImagePreviewWindow>(true, "Preview", true);
            w._image = image;
            float ratio = (float)image.height / image.width;
            float width = Mathf.Min(700f, Screen.currentResolution.width * 0.8f);
            w.minSize = new Vector2(width, width * ratio);
            w.maxSize = w.minSize;
            w.ShowUtility();
        }

        private void OnGUI()
        {
            if (_image == null) { Close(); return; }
            var rect = GUILayoutUtility.GetRect(position.width, position.height);
            GUI.DrawTexture(rect, _image, ScaleMode.ScaleToFit);
        }
    }

}
