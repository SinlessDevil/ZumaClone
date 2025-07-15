using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Editor
{
    public class TextToTMPConverterEditorWindow : EditorWindow
    {
        private GameObject _targetRoot;

        private TMP_FontAsset _tmpFont;
        private Font _unityFont;

        [MenuItem("Tools/Text <-> TMP Converter")]
        public static void ShowWindow()
        {
            GetWindow<TextToTMPConverterEditorWindow>("Text <-> TMP Converter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Select Prefab or GameObject to Convert", EditorStyles.boldLabel);
            
            DrawObjectFieldPrefab();

            GUILayout.Space(10);

            DrawFillFontTextToTMP_Pro();

            DrawButtonConvertTextToTMP_Pro();

            GUILayout.Space(10);

            DrawFillFontTMP_ProToText();

            DrawButtonConventTMP_ProToText();
        }

        private void DrawFillFontTextToTMP_Pro()
        {
            GUILayout.Label("Text ➜ TextMeshProUGUI", EditorStyles.boldLabel);
            _tmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField("TMP Font", _tmpFont, typeof(TMP_FontAsset), false);
        }

        private void DrawButtonConvertTextToTMP_Pro()
        {
            if (GUILayout.Button("Convert Text -> TextMeshPro"))
            {
                if (_targetRoot == null)
                    Debug.LogWarning("Please assign a target GameObject");
                else
                    ConvertTextToTMP(_targetRoot);
            }
        }

        private void DrawFillFontTMP_ProToText()
        {
            GUILayout.Label("TextMeshProUGUI ➜ Text", EditorStyles.boldLabel);
            _unityFont = (Font)EditorGUILayout.ObjectField("Unity Font", _unityFont, typeof(Font), false);
        }

        private void DrawButtonConventTMP_ProToText()
        {
            if (GUILayout.Button("Convert TextMeshPro -> Text"))
            {
                if (_targetRoot == null)
                    Debug.LogWarning("Please assign a target GameObject");
                else
                    ConvertTMPToText(_targetRoot);
            }
        }

        private void DrawObjectFieldPrefab()
        {
            _targetRoot = (GameObject)EditorGUILayout.ObjectField("Target Root", _targetRoot, typeof(GameObject), true);
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

                TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = textValue;
                tmp.fontSize = fontSize;
                tmp.color = color;
                tmp.alignment = ConvertAlignment(alignment);

                if (_tmpFont != null)
                    tmp.font = _tmpFont;
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

                GameObject go = oldTMP.gameObject;
                DestroyImmediate(oldTMP);

                Text text = go.AddComponent<Text>();
                text.text = textValue;
                text.fontSize = Mathf.RoundToInt(fontSize);
                text.color = color;
                text.alignment = ConvertAlignment(alignment);

                if (_unityFont != null)
                    text.font = _unityFont;
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
