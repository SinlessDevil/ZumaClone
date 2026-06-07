using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.Services.SFX.Vibration;
using Code.StaticData.SFX;
using MoreMountains.NiceVibrations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Code.Editor.SFX
{
    public class VibrationLibraryEditorWindow : EditorWindow
    {
        private const string VibrationDataPath  = "StaticData/Vibration/VibrationsData";
        private const string EnumOutputPath     = "Assets/Code/Services/SFX/Vibration/VibrationType.cs";
        private const string NameSpaceVibration = "namespace Code.Services.SFX.Vibration";
        private const string UxmlPath           = "Assets/Code/Editor/SFX/VibrationLibraryEditorWindow.uxml";
        private const string UssPath            = "Assets/Code/Editor/SFX/VibrationLibraryEditorWindow.uss";

        [SerializeField] private VibrationsData _vibrationsData;

        private string        _search = string.Empty;
        private TextField     _enumPreviewField;
        private VisualElement _vibrationsList;

        [MenuItem("Tools/AudioVibrationKit/Vibration Library")]
        private static void OpenWindow() => GetWindow<VibrationLibraryEditorWindow>().Show();

        public void CreateGUI()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath).CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath));

            if (_vibrationsData == null)
                LoadData();

            _enumPreviewField = rootVisualElement.Q<TextField>("enum-preview-field");
            _enumPreviewField.isReadOnly = true;
            RefreshEnumPreview();

            _vibrationsList = rootVisualElement.Q<VisualElement>("vibrations-list");

            rootVisualElement.Q<TextField>("search-field")
                .RegisterValueChangedCallback(evt => { _search = evt.newValue; RebuildList(); });

            rootVisualElement.Q<Button>("add-btn").clicked      += AddVibration;
            rootVisualElement.Q<Button>("generate-btn").clicked += GenerateEnum;

            RebuildList();
        }

        private void OnDisable()
        {
            if (_vibrationsData != null)
                EditorUtility.SetDirty(_vibrationsData);
        }

        private void LoadData()
        {
            _vibrationsData = Resources.Load<VibrationsData>(VibrationDataPath);
            if (_vibrationsData == null)
                Debug.LogError($"VibrationsData not found at Resources/{VibrationDataPath}.asset");
        }

        // ── List ──────────────────────────────────────────────────────────────────

        private void RebuildList()
        {
            if (_vibrationsData == null) return;

            _vibrationsList.Clear();

            IEnumerable<VibrationData> items = string.IsNullOrEmpty(_search)
                ? _vibrationsData.Vibrations
                : _vibrationsData.Vibrations.Where(v =>
                    v.Name != null && v.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var data in items)
                _vibrationsList.Add(BuildItem(data));
        }

        private void AddVibration()
        {
            _vibrationsData.Vibrations.Add(new VibrationData { Name = "NewVibration" });
            EditorUtility.SetDirty(_vibrationsData);
            RebuildList();
        }

        // ── Item builder ──────────────────────────────────────────────────────────

        private VisualElement BuildItem(VibrationData data)
        {
            var foldout = new Foldout
            {
                text  = string.IsNullOrEmpty(data.Name) ? "(unnamed)" : data.Name,
                value = false
            };
            foldout.AddToClassList("vibration-item");

            // Name
            var nameField = new TextField("Name") { value = data.Name };
            nameField.RegisterValueChangedCallback(evt =>
            {
                data.Name  = evt.newValue;
                foldout.text = string.IsNullOrEmpty(evt.newValue) ? "(unnamed)" : evt.newValue;
            });
            foldout.Add(nameField);

            // Mode
            var modeField = new EnumField("Mode", data.Mode);
            modeField.RegisterValueChangedCallback(evt => data.Mode = (VibrationMode)evt.newValue);
            foldout.Add(modeField);

            // HapticType
            var hapticField = new EnumField("Haptic Type", data.HapticType);
            hapticField.RegisterValueChangedCallback(evt => data.HapticType = (HapticTypes)evt.newValue);
            foldout.Add(hapticField);

            // Constant
            foldout.Add(MakeSlider("Constant Intensity", data.ConstantIntensity, 0f, 1f,
                v => data.ConstantIntensity = v));
            foldout.Add(MakeFloat("Constant Duration", data.ConstantDuration,
                v => data.ConstantDuration = v));

            // Emphasis
            foldout.Add(MakeSlider("Emphasis Intensity", data.EmphasisIntensity, 0f, 1f,
                v => data.EmphasisIntensity = v));
            foldout.Add(MakeSlider("Emphasis Sharpness", data.EmphasisSharpness, 0f, 1f,
                v => data.EmphasisSharpness = v));

            // Curve
            var curveField = new CurveField("Curve") { value = data.Curve };
            curveField.RegisterValueChangedCallback(evt => data.Curve = evt.newValue);
            foldout.Add(curveField);
            foldout.Add(MakeFloat("Curve Duration", data.CurveDuration, v => data.CurveDuration = v));

            // Remove
            var removeBtn = new Button(() => { _vibrationsData.Vibrations.Remove(data); RebuildList(); })
                { text = "Remove" };
            removeBtn.AddToClassList("btn-red");
            foldout.Add(removeBtn);

            return foldout;
        }

        private static Slider MakeSlider(string label, float value, float min, float max, Action<float> onChange)
        {
            var f = new Slider(label, min, max) { value = value, showInputField = true };
            f.RegisterValueChangedCallback(evt => onChange(evt.newValue));
            return f;
        }

        private static FloatField MakeFloat(string label, float value, Action<float> onChange)
        {
            var f = new FloatField(label) { value = value };
            f.RegisterValueChangedCallback(evt => onChange(evt.newValue));
            return f;
        }

        // ── Enum generation ───────────────────────────────────────────────────────

        private void GenerateEnum()
        {
            if (_vibrationsData == null)
            {
                Debug.LogError("[VibrationLibraryEditorWindow] VibrationsData not loaded.");
                return;
            }

            var names = _vibrationsData.Vibrations
                .Where(v => !string.IsNullOrWhiteSpace(v.Name))
                .Select(v => v.Name.Replace(" ", "_").Replace("-", "_").Replace(".", "_").Trim())
                .Distinct()
                .ToList();

            using (var w = new StreamWriter(EnumOutputPath))
            {
                w.WriteLine("using System;");
                w.WriteLine();
                w.WriteLine(NameSpaceVibration);
                w.WriteLine("{");
                w.WriteLine("    [Serializable]");
                w.WriteLine("    public enum VibrationType");
                w.WriteLine("    {");
                w.WriteLine("        Unknown = -1,");
                for (int i = 0; i < names.Count; i++)
                    w.WriteLine($"        {names[i]} = {i},");
                w.WriteLine("    }");
                w.WriteLine("}");
            }

            AssignEnumTypes();
            EditorUtility.SetDirty(_vibrationsData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshEnumPreview();
            Debug.Log($"[VibrationLibraryEditorWindow] VibrationType enum generated at {EnumOutputPath}");
        }

        private void AssignEnumTypes()
        {
            foreach (var v in _vibrationsData.Vibrations)
            {
                string s = v.Name.Replace(" ", "_").Replace("-", "_").Replace(".", "_").Trim();
                v.VibrationType = Enum.TryParse(s, out VibrationType parsed) ? parsed : VibrationType.Unknown;
            }
        }

        // Called by EnumSyncPostCompile after domain reload
        public void UpdateVibrationTypesAfterReload()
        {
            if (_vibrationsData == null) LoadData();
            if (_vibrationsData == null) return;

            AssignEnumTypes();
            EditorUtility.SetDirty(_vibrationsData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshEnumPreview();
        }

        private void RefreshEnumPreview()
        {
            if (_enumPreviewField == null) return;
            _enumPreviewField.SetValueWithoutNotify(string.Join(", ", Enum.GetNames(typeof(VibrationType))));
        }
    }
}
