using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.Services.SFX.Music;
using Code.Services.SFX.Sound;
using Code.StaticData.SFX;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Code.Editor.SFX
{
    public class SoundLibraryEditorWindow : EditorWindow
    {
        private const string SoundPath      = "Assets/Code/Services/SFX/Sound/";
        private const string MusicPath      = "Assets/Code/Services/SFX/Music/";
        private const string NameSpaceSound = "namespace Code.Services.SFX.Sound";
        private const string NameSpaceMusic = "namespace Code.Services.SFX.Music";
        private const string UxmlPath       = "Assets/Code/Editor/SFX/SoundLibraryEditorWindow.uxml";
        private const string UssPath        = "Assets/Code/Editor/SFX/SoundLibraryEditorWindow.uss";

        [SerializeField] private SoundsData _soundsData;

        private string _search2D    = string.Empty;
        private string _search3D    = string.Empty;
        private string _searchMusic = string.Empty;

        private TextField     _enum2DField;
        private TextField     _enum3DField;
        private TextField     _enumMusicField;
        private VisualElement _list2D;
        private VisualElement _list3D;
        private VisualElement _listMusic;

        [MenuItem("Tools/AudioVibrationKit/Sound Library")]
        private static void OpenWindow() => GetWindow<SoundLibraryEditorWindow>().Show();

        public void CreateGUI()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath).CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath));

            if (_soundsData == null)
                LoadSoundsData();

            BindEnumsSection();

            _list2D = rootVisualElement.Q<VisualElement>("list-2d");
            rootVisualElement.Q<TextField>("search-2d-field")
                .RegisterValueChangedCallback(evt => { _search2D = evt.newValue; Rebuild2DList(); });
            rootVisualElement.Q<Button>("add-2d-btn").clicked += Add2DSound;

            _list3D = rootVisualElement.Q<VisualElement>("list-3d");
            rootVisualElement.Q<TextField>("search-3d-field")
                .RegisterValueChangedCallback(evt => { _search3D = evt.newValue; Rebuild3DList(); });
            rootVisualElement.Q<Button>("add-3d-btn").clicked += Add3DSound;

            _listMusic = rootVisualElement.Q<VisualElement>("list-music");
            rootVisualElement.Q<TextField>("search-music-field")
                .RegisterValueChangedCallback(evt => { _searchMusic = evt.newValue; RebuildMusicList(); });
            rootVisualElement.Q<Button>("add-music-btn").clicked += AddMusic;

            rootVisualElement.Q<Button>("generate-btn").clicked += GenerateEnums;

            Rebuild2DList();
            Rebuild3DList();
            RebuildMusicList();
        }

        private void OnDisable()
        {
            if (_soundsData != null)
                EditorUtility.SetDirty(_soundsData);
        }

        private void LoadSoundsData()
        {
            _soundsData = Resources.Load<SoundsData>("StaticData/Sounds/Sounds");
            if (_soundsData == null)
                Debug.LogError("SoundsData not found at Resources/StaticData/Sounds/Sounds.asset");
        }

        // ── Enums section ─────────────────────────────────────────────────────────

        private void BindEnumsSection()
        {
            _enum2DField    = rootVisualElement.Q<TextField>("enum-2d-field");
            _enum3DField    = rootVisualElement.Q<TextField>("enum-3d-field");
            _enumMusicField = rootVisualElement.Q<TextField>("enum-music-field");

            _enum2DField.isReadOnly    = true;
            _enum3DField.isReadOnly    = true;
            _enumMusicField.isReadOnly = true;

            RefreshEnumFields();
        }

        private void RefreshEnumFields()
        {
            if (_enum2DField == null) return;
            _enum2DField.SetValueWithoutNotify(GetEnumValues(typeof(Sound2DType)));
            _enum3DField.SetValueWithoutNotify(GetEnumValues(typeof(Sound3DType)));
            _enumMusicField.SetValueWithoutNotify(GetEnumValues(typeof(MusicType)));
        }

        // ── List rebuild ──────────────────────────────────────────────────────────

        private void Rebuild2DList()
        {
            if (_soundsData == null) return;
            RebuildList(_list2D, _soundsData.Sounds2DData, _search2D,
                data => BuildSoundItem(data, () => { _soundsData.Sounds2DData.Remove(data); Rebuild2DList(); }));
        }

        private void Rebuild3DList()
        {
            if (_soundsData == null) return;
            RebuildList(_list3D, _soundsData.Sounds3DData, _search3D,
                data => BuildSound3DItem(data, () => { _soundsData.Sounds3DData.Remove(data); Rebuild3DList(); }));
        }

        private void RebuildMusicList()
        {
            if (_soundsData == null) return;
            RebuildList(_listMusic, _soundsData.MusicData, _searchMusic,
                data => BuildSoundItem(data, () => { _soundsData.MusicData.Remove(data); RebuildMusicList(); }));
        }

        private static void RebuildList<T>(VisualElement container, List<T> source, string search,
            Func<T, VisualElement> builder) where T : SoundData
        {
            container.Clear();
            IEnumerable<T> items = string.IsNullOrEmpty(search)
                ? source
                : source.Where(s => s.Name != null &&
                                    s.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (var data in items)
                container.Add(builder(data));
        }

        // ── Add ───────────────────────────────────────────────────────────────────

        private void Add2DSound()
        {
            _soundsData.Sounds2DData.Add(new SoundData { Name = "NewSound2D" });
            EditorUtility.SetDirty(_soundsData);
            Rebuild2DList();
        }

        private void Add3DSound()
        {
            _soundsData.Sounds3DData.Add(new Sound3DData { Name = "NewSound3D" });
            EditorUtility.SetDirty(_soundsData);
            Rebuild3DList();
        }

        private void AddMusic()
        {
            _soundsData.MusicData.Add(new SoundData { Name = "NewMusic" });
            EditorUtility.SetDirty(_soundsData);
            RebuildMusicList();
        }

        // ── Item builders ─────────────────────────────────────────────────────────

        private VisualElement BuildSoundItem(SoundData data, Action onRemove)
        {
            var foldout = CreateItemFoldout(data.Name);
            AddBaseFields(foldout, data);
            foldout.Add(MakeRemoveButton(onRemove));
            return foldout;
        }

        private VisualElement BuildSound3DItem(Sound3DData data, Action onRemove)
        {
            var foldout = CreateItemFoldout(data.Name);
            AddBaseFields(foldout, data);
            foldout.Add(BindSlider("Spatial Blend", data.SpatialBlend, 0f, 1f, v => data.SpatialBlend = v));
            foldout.Add(BindEnum("Rolloff Mode", data.RolloffMode, v => data.RolloffMode = (AudioRolloffMode)v));
            foldout.Add(BindFloat("Min Distance", data.MinDistance, v => data.MinDistance = v));
            foldout.Add(BindFloat("Max Distance", data.MaxDistance, v => data.MaxDistance = v));
            foldout.Add(MakeRemoveButton(onRemove));
            return foldout;
        }

        private static void AddBaseFields(Foldout foldout, SoundData data)
        {
            var nameField = new TextField("Name") { value = data.Name };
            nameField.RegisterValueChangedCallback(evt =>
            {
                data.Name = evt.newValue;
                foldout.text = string.IsNullOrEmpty(evt.newValue) ? "(unnamed)" : evt.newValue;
            });
            foldout.Add(nameField);

            var clipField = new ObjectField("Clip") { objectType = typeof(AudioClip), value = data.Clip };
            clipField.RegisterValueChangedCallback(evt => data.Clip = evt.newValue as AudioClip);
            foldout.Add(clipField);

            foldout.Add(BindSlider("Volume", data.Volume, 0f, 1f, v => data.Volume = v));
            foldout.Add(BindToggle("Loop", data.Loop, v => data.Loop = v));
            foldout.Add(BindToggle("Play On Awake", data.PlayOnAwake, v => data.PlayOnAwake = v));
        }

        // ── Field helpers ─────────────────────────────────────────────────────────

        private static Foldout CreateItemFoldout(string name)
        {
            var f = new Foldout { text = string.IsNullOrEmpty(name) ? "(unnamed)" : name, value = false };
            f.AddToClassList("sound-item");
            return f;
        }

        private static Slider BindSlider(string label, float value, float min, float max, Action<float> onChange)
        {
            var f = new Slider(label, min, max) { value = value, showInputField = true };
            f.RegisterValueChangedCallback(evt => onChange(evt.newValue));
            return f;
        }

        private static Toggle BindToggle(string label, bool value, Action<bool> onChange)
        {
            var f = new Toggle(label) { value = value };
            f.RegisterValueChangedCallback(evt => onChange(evt.newValue));
            return f;
        }

        private static EnumField BindEnum(string label, Enum value, Action<Enum> onChange)
        {
            var f = new EnumField(label, value);
            f.RegisterValueChangedCallback(evt => onChange(evt.newValue));
            return f;
        }

        private static FloatField BindFloat(string label, float value, Action<float> onChange)
        {
            var f = new FloatField(label) { value = value };
            f.RegisterValueChangedCallback(evt => onChange(evt.newValue));
            return f;
        }

        private static Button MakeRemoveButton(Action onClick)
        {
            var btn = new Button(onClick) { text = "Remove" };
            btn.AddToClassList("btn-red");
            return btn;
        }

        // ── Enum generation ───────────────────────────────────────────────────────

        private void GenerateEnums()
        {
            GenerateEnumFile($"{SoundPath}Sound2DType.cs", "Sound2DType", NameSpaceSound,
                _soundsData.Sounds2DData, TypeSound.Sound2D);
            GenerateEnumFile($"{SoundPath}Sound3DType.cs", "Sound3DType", NameSpaceSound,
                _soundsData.Sounds3DData.Cast<SoundData>().ToList(), TypeSound.Sound3D);
            GenerateEnumFile($"{MusicPath}MusicType.cs", "MusicType", NameSpaceMusic,
                _soundsData.MusicData, TypeSound.Music);

            EditorUtility.SetDirty(_soundsData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void GenerateEnumFile(string path, string enumName, string nameSpace,
            List<SoundData> list, TypeSound type)
        {
            var names = list
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => s.Name.Replace(" ", "_").Replace("-", "_").Replace(".", "_").Trim())
                .Distinct()
                .ToList();

            using (var w = new StreamWriter(path))
            {
                w.WriteLine("using System;");
                w.WriteLine();
                w.WriteLine(nameSpace);
                w.WriteLine("{");
                w.WriteLine("    [Serializable]");
                w.WriteLine($"    public enum {enumName}");
                w.WriteLine("    {");
                w.WriteLine("        Unknown = -1,");
                for (int i = 0; i < names.Count; i++)
                    w.WriteLine($"        {names[i]} = {i},");
                w.WriteLine("    }");
                w.WriteLine("}");
            }

            foreach (var sound in list)
            {
                string s = sound.Name.Replace(" ", "_").Replace("-", "_").Replace(".", "_").Trim();
                switch (type)
                {
                    case TypeSound.Sound2D when Enum.TryParse(s, out Sound2DType t): sound.Sound2DType = t; break;
                    case TypeSound.Sound3D when Enum.TryParse(s, out Sound3DType t): sound.Sound3DType = t; break;
                    case TypeSound.Music   when Enum.TryParse(s, out MusicType   t): sound.MusicType   = t; break;
                    default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }

            EditorUtility.SetDirty(_soundsData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{enumName} enum generated.");
        }

        // Called by EnumSyncPostCompile after domain reload
        public void UpdateSoundTypesAfterReload()
        {
            if (_soundsData == null) LoadSoundsData();
            if (_soundsData == null) return;

            foreach (var s in _soundsData.Sounds2DData)
                s.Sound2DType = Enum.TryParse(s.Name, out Sound2DType t) ? t : Sound2DType.Unknown;
            foreach (var s in _soundsData.Sounds3DData)
                s.Sound3DType = Enum.TryParse(s.Name, out Sound3DType t) ? t : Sound3DType.Unknown;
            foreach (var s in _soundsData.MusicData)
                s.MusicType = Enum.TryParse(s.Name, out MusicType t) ? t : MusicType.Unknown;

            EditorUtility.SetDirty(_soundsData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshEnumFields();
        }

        private static string GetEnumValues(Type type) => string.Join(", ", Enum.GetNames(type));

        private enum TypeSound { Sound2D, Sound3D, Music }
    }
}
