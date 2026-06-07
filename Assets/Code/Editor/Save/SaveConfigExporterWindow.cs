#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Code.Editor
{
    public class SaveConfigExporterWindow : EditorWindow
    {
        private const string FolderName = "ExportedData";
        private const string UxmlPath = "Assets/Code/Editor/Save/SaveConfigExporterWindow.uxml";
        private const string UssPath = "Assets/Code/Editor/Save/SaveConfigExporterWindow.uss";

        private ScriptableObject _targetAsset;
        private List<string> _jsonFiles = new();
        private int _selectedFileIndex;

        private VisualElement _inlineInspectorContainer;
        private InspectorElement _inspectorElement;
        private HelpBox _noAssetHelp;
        private VisualElement _jsonControlsBox;
        private VisualElement _overwriteRow;
        private DropdownField _fileDropdown;
        private VisualElement _filesList;

        private string SaveFolderPath => Path.Combine(Application.dataPath, FolderName);

        private string GenerateJsonPath() =>
            _targetAsset != null
                ? Path.Combine(SaveFolderPath, $"{_targetAsset.name}_{DateTime.Now:yyyyMMdd_HHmmss}.json")
                : null;

        [MenuItem("Tools/Save System Kit/Save Config Exporter Window")]
        private static void OpenWindow()
        {
            var window = GetWindow<SaveConfigExporterWindow>();
            window.titleContent = new GUIContent("Save Config Exporter");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        public void CreateGUI()
        {
            Directory.CreateDirectory(SaveFolderPath);

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            uxml.CloneTree(rootVisualElement);

            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            rootVisualElement.styleSheets.Add(uss);

            var assetField = rootVisualElement.Q<ObjectField>("target-asset-field");
            assetField.objectType = typeof(ScriptableObject);
            assetField.RegisterValueChangedCallback(OnTargetAssetChanged);

            _noAssetHelp = rootVisualElement.Q<HelpBox>("no-asset-help");
            _inlineInspectorContainer = rootVisualElement.Q<VisualElement>("inline-inspector-container");
            _jsonControlsBox = rootVisualElement.Q<VisualElement>("json-controls-box");
            _overwriteRow = rootVisualElement.Q<VisualElement>("overwrite-row");
            _filesList = rootVisualElement.Q<VisualElement>("files-list");

            rootVisualElement.Q<Button>("save-new-btn").clicked += SaveNewJson;

            rootVisualElement.Q<Button>("overwrite-btn").clicked += OnOverwriteClicked;

            _fileDropdown = rootVisualElement.Q<DropdownField>("file-dropdown");
            _fileDropdown.RegisterValueChangedCallback(_ => _selectedFileIndex = _fileDropdown.index);

            RefreshUI();
        }

        private void OnTargetAssetChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            _targetAsset = evt.newValue as ScriptableObject;
            RefreshJsonFiles();
            RefreshUI();
        }

        private void OnOverwriteClicked()
        {
            if (_jsonFiles.Count > 0 && _selectedFileIndex < _jsonFiles.Count)
                OverwriteJsonFile(_jsonFiles[_selectedFileIndex]);
        }

        private void RefreshUI()
        {
            bool hasAsset = _targetAsset != null;

            _noAssetHelp.style.display = hasAsset ? DisplayStyle.None : DisplayStyle.Flex;
            _jsonControlsBox.style.display = hasAsset ? DisplayStyle.Flex : DisplayStyle.None;

            RebuildInlineInspector(hasAsset);

            if (hasAsset)
            {
                RebuildOverwriteRow();
                RebuildFilesList();
            }
        }

        private void RebuildInlineInspector(bool hasAsset)
        {
            if (_inspectorElement != null)
            {
                _inlineInspectorContainer.Remove(_inspectorElement);
                _inspectorElement = null;
            }

            if (!hasAsset)
                return;

            _inspectorElement = new InspectorElement(_targetAsset);
            _inlineInspectorContainer.Add(_inspectorElement);
        }

        private void RebuildOverwriteRow()
        {
            bool hasFiles = _jsonFiles.Count > 0;
            _overwriteRow.style.display = hasFiles ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasFiles)
                return;

            _fileDropdown.choices = _jsonFiles.Select(Path.GetFileName).ToList();
            _selectedFileIndex = Mathf.Clamp(_selectedFileIndex, 0, _jsonFiles.Count - 1);
            _fileDropdown.index = _selectedFileIndex;
        }

        private void RebuildFilesList()
        {
            _filesList.Clear();

            foreach (string filePath in _jsonFiles)
            {
                var row = new VisualElement();
                row.AddToClassList("file-row");

                var loadBtn = new Button(() => LoadJsonToTarget(filePath)) { text = "Load" };
                loadBtn.AddToClassList("btn-green");

                var label = new Label(Path.GetFileName(filePath));
                label.AddToClassList("file-name-label");

                var deleteBtn = new Button(() => DeleteJsonFile(filePath)) { text = "Delete" };
                deleteBtn.AddToClassList("btn-red");

                row.Add(loadBtn);
                row.Add(label);
                row.Add(deleteBtn);
                _filesList.Add(row);
            }
        }

        private void SaveNewJson()
        {
            try
            {
                File.WriteAllText(GenerateJsonPath(), JsonUtility.ToJson(_targetAsset, true));
                RefreshJsonFiles();
                RefreshUI();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save JSON: {e}");
            }
        }

        private void OverwriteJsonFile(string path)
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(_targetAsset, true));
                RefreshJsonFiles();
                RefreshUI();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to overwrite JSON: {e}");
            }
        }

        private void LoadJsonToTarget(string path)
        {
            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), _targetAsset);
                EditorUtility.SetDirty(_targetAsset);
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load JSON: {e}");
            }
        }

        private void DeleteJsonFile(string path)
        {
            try
            {
                File.Delete(path);
                RefreshJsonFiles();
                RefreshUI();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete JSON: {e}");
            }
        }

        private void RefreshJsonFiles()
        {
            _jsonFiles = Directory
                .GetFiles(SaveFolderPath, "*.json")
                .Where(f => _targetAsset != null && Path.GetFileName(f).StartsWith(_targetAsset.name))
                .ToList();
        }
    }
}
#endif
