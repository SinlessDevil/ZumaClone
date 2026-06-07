#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.Localization.Code;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Code.Editor.Localization
{
    public class LocalizationEditorWindow : EditorWindow
    {
        private const string LocalizationFolder = "Assets/Code/Localization/Intro/Resources/Localization";
        private const string UxmlPath = "Assets/Code/Editor/Localization/LocalizationEditorWindow.uxml";
        private const string UssPath  = "Assets/Code/Editor/Localization/LocalizationEditorWindow.uss";

        private string _selectedLanguage;
        private string _searchKey = string.Empty;
        private string _newLanguage;
        private string _baseLanguage = "English";

        private List<LocalizationEntry> _allEntries      = new();
        private List<LocalizationEntry> _filteredEntries = new();
        private List<TMPAssetEntry>     _localizersInAssets = new();

        private DropdownField _langDropdown;
        private DropdownField _newLangDropdown;
        private DropdownField _baseLangDropdown;
        private VisualElement _entriesList;
        private VisualElement _assetsList;

        [MenuItem("Tools/Localization Editor Window")]
        private static void OpenWindow()
        {
            var window = GetWindow<LocalizationEditorWindow>();
            window.titleContent = new GUIContent("Localization Editor");
            window.minSize = new Vector2(600, 800);
            window.Show();
        }

        public void CreateGUI()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath).CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath));

            BindLocalizationFileSection();
            BindCreateLanguageSection();
            BindFindAssetsSection();
            rootVisualElement.Q<Button>("add-localizers-btn").clicked += AddTMPLocalizersToResources;

            RefreshLanguageDropdowns();
        }

        // ── Section 1 : Localization File ────────────────────────────────────────

        private void BindLocalizationFileSection()
        {
            _langDropdown = rootVisualElement.Q<DropdownField>("lang-dropdown");
            _langDropdown.RegisterValueChangedCallback(evt =>
            {
                _selectedLanguage = evt.newValue;
                LoadLocalizationFile();
            });

            rootVisualElement.Q<TextField>("search-key-field").RegisterValueChangedCallback(evt =>
            {
                _searchKey = evt.newValue;
                FilterAndRebuild();
            });

            BuildEntriesTable(rootVisualElement.Q<VisualElement>("entries-table-container"));

            rootVisualElement.Q<Button>("save-lang-btn").clicked   += Save;
            rootVisualElement.Q<Button>("delete-lang-btn").clicked += DeleteSelectedLanguage;
        }

        private void BuildEntriesTable(VisualElement container)
        {
            var header = BuildTableHeader(
                ("Key",   "cell-key"),
                ("Value", "cell-value"));

            var scroll = new ScrollView { style = { height = 300 } };
            _entriesList = new VisualElement();
            scroll.Add(_entriesList);

            container.Add(header);
            container.Add(scroll);
        }

        private void RebuildEntriesTable()
        {
            _entriesList.Clear();
            foreach (var entry in _filteredEntries)
            {
                var row = new VisualElement();
                row.AddToClassList("table-row");

                var keyField = MakeEditableCell("cell-key");
                keyField.SetValueWithoutNotify(entry.Key);
                keyField.RegisterValueChangedCallback(evt => entry.Key = evt.newValue);

                var valueField = MakeEditableCell("cell-value");
                valueField.multiline = true;
                valueField.SetValueWithoutNotify(entry.Value);
                valueField.RegisterValueChangedCallback(evt => entry.Value = evt.newValue);

                row.Add(keyField);
                row.Add(valueField);
                _entriesList.Add(row);
            }
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(_selectedLanguage))
                return;

            string path = Path.Combine(LocalizationFolder, _selectedLanguage + ".txt");
            File.WriteAllLines(path, _allEntries.Select(e => $"{e.Key}={e.Value.Replace("\n", "\\n")}"));
            AssetDatabase.Refresh();
            Debug.Log($"Saved language file: {path}");
        }

        private void DeleteSelectedLanguage()
        {
            if (string.IsNullOrEmpty(_selectedLanguage))
            {
                Debug.LogWarning("No language selected to delete.");
                return;
            }

            string path = Path.Combine(LocalizationFolder, _selectedLanguage + ".txt");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"File not found: {path}");
                return;
            }

            File.Delete(path);
            AssetDatabase.Refresh();
            Debug.Log($"Deleted language file: {path}");

            _selectedLanguage = null;
            _allEntries.Clear();
            _filteredEntries.Clear();

            var available = GetAvailableLanguages().ToList();
            RefreshLanguageDropdowns(available);

            if (available.Count > 0)
            {
                _selectedLanguage = available[0];
                _langDropdown.SetValueWithoutNotify(_selectedLanguage);
                LoadLocalizationFile();
            }
            else
            {
                RebuildEntriesTable();
            }
        }

        private void LoadLocalizationFile()
        {
            _allEntries.Clear();
            _filteredEntries.Clear();

            if (string.IsNullOrEmpty(_selectedLanguage))
            {
                RebuildEntriesTable();
                return;
            }

            string path = Path.Combine(LocalizationFolder, _selectedLanguage + ".txt");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"File not found: {path}");
                RebuildEntriesTable();
                return;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                string[] pair = line.Split(new[] { '=' }, 2);
                if (pair.Length == 2)
                    _allEntries.Add(new LocalizationEntry
                    {
                        Key   = pair[0].Trim(),
                        Value = pair[1].Trim().Replace("\\n", "\n")
                    });
            }

            FilterAndRebuild();
        }

        private void FilterAndRebuild()
        {
            _filteredEntries = string.IsNullOrEmpty(_searchKey)
                ? new List<LocalizationEntry>(_allEntries)
                : _allEntries.FindAll(e => e.Key.IndexOf(_searchKey, StringComparison.OrdinalIgnoreCase) >= 0);

            RebuildEntriesTable();
        }

        // ── Section 2 : Create New Language ──────────────────────────────────────

        private void BindCreateLanguageSection()
        {
            _newLangDropdown = rootVisualElement.Q<DropdownField>("new-lang-dropdown");
            _newLangDropdown.RegisterValueChangedCallback(evt => _newLanguage = evt.newValue);

            _baseLangDropdown = rootVisualElement.Q<DropdownField>("base-lang-dropdown");
            _baseLangDropdown.RegisterValueChangedCallback(evt => _baseLanguage = evt.newValue);

            rootVisualElement.Q<Button>("create-lang-btn").clicked += CreateNewLanguageFromBase;
        }

        private void CreateNewLanguageFromBase()
        {
            if (string.IsNullOrEmpty(_newLanguage))
            {
                Debug.LogWarning("No new language selected.");
                return;
            }

            string targetPath = Path.Combine(LocalizationFolder, _newLanguage + ".txt");
            if (File.Exists(targetPath))
            {
                Debug.LogWarning($"Language '{_newLanguage}' already exists.");
                return;
            }

            string basePath = Path.Combine(LocalizationFolder, _baseLanguage + ".txt");
            if (!File.Exists(basePath))
            {
                Debug.LogError($"Base language file '{_baseLanguage}.txt' not found!");
                return;
            }

            File.Copy(basePath, targetPath);
            AssetDatabase.Refresh();
            Debug.Log($"Created '{_newLanguage}' from '{_baseLanguage}'.");
            RefreshLanguageDropdowns();
        }

        // ── Section 3 : Find Localized Assets ────────────────────────────────────

        private void BindFindAssetsSection()
        {
            BuildAssetsTable(rootVisualElement.Q<VisualElement>("assets-table-container"));

            rootVisualElement.Q<Button>("find-localizers-btn").clicked += FindTMPLocalizersInAssets;
            rootVisualElement.Q<Button>("save-changes-btn").clicked    += ApplyLocalizationKeyChangesToAssets;
        }

        private void BuildAssetsTable(VisualElement container)
        {
            var header = BuildTableHeader(
                ("GameObject",       "cell-name"),
                ("Parent",           "cell-parent"),
                ("Asset Path",       "cell-path"),
                ("Localization Key", "cell-lockey"));

            var scroll = new ScrollView { style = { height = 200 } };
            _assetsList = new VisualElement();
            scroll.Add(_assetsList);

            container.Add(header);
            container.Add(scroll);
        }

        private void RebuildAssetsTable()
        {
            _assetsList.Clear();
            foreach (var entry in _localizersInAssets)
            {
                var row = new VisualElement();
                row.AddToClassList("table-row");

                row.Add(MakeReadonlyCell(entry.GameObjectName, "cell-name"));
                row.Add(MakeReadonlyCell(entry.ParentName,     "cell-parent"));
                row.Add(MakeReadonlyCell(entry.AssetPath,      "cell-path"));

                var keyField = MakeEditableCell("cell-lockey");
                keyField.SetValueWithoutNotify(entry.LocalizationKey);
                keyField.RegisterValueChangedCallback(evt => entry.LocalizationKey = evt.newValue);
                row.Add(keyField);

                _assetsList.Add(row);
            }
        }

        private void FindTMPLocalizersInAssets()
        {
            _localizersInAssets.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                if (prefabRoot == null)
                {
                    Debug.LogWarning($"Failed to load prefab: {path}");
                    continue;
                }

                foreach (var localizer in prefabRoot.GetComponentsInChildren<LocalizeBase>(true))
                    _localizersInAssets.Add(new TMPAssetEntry
                    {
                        AssetPath        = path,
                        GameObjectName   = localizer.gameObject.name,
                        ParentName       = localizer.transform.parent != null ? localizer.transform.parent.name : "(root)",
                        LocalizationKey  = localizer.localizationKey
                    });

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            Debug.Log($"Found {_localizersInAssets.Count} TMP_Localizers in prefabs.");
            RebuildAssetsTable();
        }

        private void ApplyLocalizationKeyChangesToAssets()
        {
            int updatedCount = 0;
            var affectedPaths = new HashSet<string>();

            foreach (var entry in _localizersInAssets)
            {
                if (string.IsNullOrEmpty(entry.AssetPath) || string.IsNullOrEmpty(entry.GameObjectName))
                {
                    Debug.LogWarning("Missing path or object name, skipping.");
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"Failed to load prefab: {entry.AssetPath}");
                    continue;
                }

                foreach (var localizer in prefab.GetComponentsInChildren<LocalizeBase>(true))
                {
                    if (localizer.gameObject.name != entry.GameObjectName)
                        continue;

                    if (localizer.localizationKey != entry.LocalizationKey)
                    {
                        Undo.RecordObject(localizer, "Change Localization Key");
                        localizer.localizationKey = entry.LocalizationKey;
                        EditorUtility.SetDirty(localizer);
                        updatedCount++;
                        Debug.Log($"Updated '{entry.GameObjectName}': '{localizer.localizationKey}' → '{entry.LocalizationKey}'");
                    }

                    affectedPaths.Add(entry.AssetPath);
                    break;
                }

                EditorUtility.SetDirty(prefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Done! Updated {updatedCount} key(s) in {affectedPaths.Count} prefab(s).");
        }

        // ── Section 4 : Add Missing Localizers ───────────────────────────────────

        private void AddTMPLocalizersToResources()
        {
            int addedCount = 0;

            foreach (GameObject prefab in Resources.LoadAll<GameObject>(""))
            {
                if (prefab == null) continue;

                string path = AssetDatabase.GetAssetPath(prefab);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool wasModified = false;

                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.GetComponent<LocalizeBase>() != null) continue;

                    Undo.RegisterCompleteObjectUndo(text.gameObject, "Add TMP_Localizer");
                    text.gameObject.AddComponent<TMP_Localizer>();
                    EditorUtility.SetDirty(text.gameObject);
                    wasModified = true;
                    addedCount++;
                }

                if (wasModified)
                {
                    EditorUtility.SetDirty(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Added TMP_Localizer to {addedCount} TMP_Text objects.");
        }

        // ── Dropdown / language helpers ───────────────────────────────────────────

        private void RefreshLanguageDropdowns() =>
            RefreshLanguageDropdowns(GetAvailableLanguages().ToList());

        private void RefreshLanguageDropdowns(List<string> available)
        {
            var newLangs = GetNewLanguages().Select(l => l.ToString()).ToList();

            _langDropdown.choices = available;
            if (_selectedLanguage != null && available.Contains(_selectedLanguage))
                _langDropdown.SetValueWithoutNotify(_selectedLanguage);
            else
                _langDropdown.SetValueWithoutNotify(available.Count > 0 ? available[0] : "");

            _baseLangDropdown.choices = available;
            if (!available.Contains(_baseLanguage))
                _baseLanguage = available.Count > 0 ? available[0] : string.Empty;
            _baseLangDropdown.SetValueWithoutNotify(_baseLanguage);

            _newLangDropdown.choices = newLangs;
            if (_newLanguage == null || !newLangs.Contains(_newLanguage))
                _newLanguage = newLangs.Count > 0 ? newLangs[0] : null;
            _newLangDropdown.SetValueWithoutNotify(_newLanguage ?? "");
        }

        private IEnumerable<string> GetAvailableLanguages()
        {
            if (!Directory.Exists(LocalizationFolder))
                Directory.CreateDirectory(LocalizationFolder);

            return Directory.GetFiles(LocalizationFolder, "*.txt")
                            .Select(Path.GetFileNameWithoutExtension);
        }

        private IEnumerable<SystemLanguage> GetNewLanguages()
        {
            var existing = new HashSet<string>(GetAvailableLanguages(), StringComparer.OrdinalIgnoreCase);
            return Enum.GetValues(typeof(SystemLanguage))
                       .Cast<SystemLanguage>()
                       .Where(l => !existing.Contains(l.ToString()));
        }

        // ── Table building helpers ────────────────────────────────────────────────

        private static VisualElement BuildTableHeader(params (string title, string css)[] columns)
        {
            var header = new VisualElement();
            header.AddToClassList("table-header-row");
            foreach (var (title, css) in columns)
            {
                var label = new Label(title);
                label.AddToClassList("table-header-label");
                label.AddToClassList(css);
                header.Add(label);
            }
            return header;
        }

        private static TextField MakeEditableCell(string cssClass)
        {
            var tf = new TextField();
            tf.AddToClassList(cssClass);
            return tf;
        }

        private static TextField MakeReadonlyCell(string value, string cssClass)
        {
            var tf = new TextField();
            tf.AddToClassList(cssClass);
            tf.AddToClassList("cell-readonly");
            tf.isReadOnly = true;
            tf.SetValueWithoutNotify(value);
            return tf;
        }

        // ── Data classes ──────────────────────────────────────────────────────────

        [Serializable]
        public class LocalizationEntry
        {
            public string Key;
            public string Value;
        }

        [Serializable]
        public class TMPAssetEntry
        {
            public string GameObjectName;
            public string ParentName;
            public string AssetPath;
            public string LocalizationKey;
        }
    }
}
#endif
