using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Editor
{
    public class TextToTMPConverterEditorWindow : EditorWindow
    {
        private GameObject targetRoot;

        [MenuItem("Tools/Text <-> TMP Converter")]
        public static void ShowWindow()
        {
            GetWindow<TextToTMPConverterEditorWindow>("Text <-> TMP Converter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Select Prefab or GameObject to Convert", EditorStyles.boldLabel);

            DrawObjectField();

            DrawButtonConvertTextToTMP_Pro();

            DrawButtonConvertTMP_ProToText();
        }

        private void DrawObjectField()
        {
            targetRoot = (GameObject)EditorGUILayout.ObjectField("Target Root", targetRoot, typeof(GameObject), true);
        }

        private void DrawButtonConvertTMP_ProToText()
        {
            if (!GUILayout.Button("Convert TextMeshPro -> Text")) 
                return;
            
            if (targetRoot != null)
                ConvertTMPToText(targetRoot);
            else
                Debug.LogWarning("Please assign a target GameObject");
        }

        private void DrawButtonConvertTextToTMP_Pro()
        {
            if (!GUILayout.Button("Convert Text -> TextMeshPro")) 
                return;
            
            if (targetRoot != null)
                ConvertTextToTMP(targetRoot);
            else
                Debug.LogWarning("Please assign a target GameObject");
        }

        private void ConvertTextToTMP(GameObject root)
        {
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            foreach (Text oldText in texts)
            {
                string textValue = oldText.text;
                Font font = oldText.font;
                int fontSize = oldText.fontSize;
                Color color = oldText.color;
                TextAnchor alignment = oldText.alignment;

                GameObject go = oldText.gameObject;
                DestroyImmediate(oldText);

                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = textValue;
                tmp.fontSize = fontSize;
                tmp.color = color;
                tmp.alignment = ConvertAlignment(alignment);
            }
        }

        private void ConvertTMPToText(GameObject root)
        {
            TextMeshProUGUI[] tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var oldTMP in tmps)
            {
                string textValue = oldTMP.text;
                float fontSize = oldTMP.fontSize;
                Color color = oldTMP.color;
                TextAlignmentOptions alignment = oldTMP.alignment;

                GameObject gameObject = oldTMP.gameObject;
                DestroyImmediate(oldTMP);

                Text text = gameObject.AddComponent<Text>();
                text.text = textValue;
                text.fontSize = Mathf.RoundToInt(fontSize);
                text.color = color;
                text.alignment = ConvertAlignment(alignment);
            }
        }

        private TextAlignmentOptions ConvertAlignment(TextAnchor anchor) =>
            anchor switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center,
            };

        private TextAnchor ConvertAlignment(TextAlignmentOptions options) =>
            options switch
            {
                TextAlignmentOptions.TopLeft => TextAnchor.UpperLeft,
                TextAlignmentOptions.Top => TextAnchor.UpperCenter,
                TextAlignmentOptions.TopRight => TextAnchor.UpperRight,
                TextAlignmentOptions.Left => TextAnchor.MiddleLeft,
                TextAlignmentOptions.Center => TextAnchor.MiddleCenter,
                TextAlignmentOptions.Right => TextAnchor.MiddleRight,
                TextAlignmentOptions.BottomLeft => TextAnchor.LowerLeft,
                TextAlignmentOptions.Bottom => TextAnchor.LowerCenter,
                TextAlignmentOptions.BottomRight => TextAnchor.LowerRight,
                _ => TextAnchor.MiddleCenter,
            };
    }
}