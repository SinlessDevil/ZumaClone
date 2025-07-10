#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

namespace Code.Editor
{
    public class SaveConfigExporterWindow : OdinEditorWindow
    {
        private const string FolderName = "ExportedData";

        [InfoBox("Drag any ScriptableObject here and save or load as JSON")]
        [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
        [SerializeField] private ScriptableObject _targetAsset;

        private ScriptableObject _lastTargetAsset;
        
        private List<string> _jsonFiles = new();

        private string SaveFolderPath => Path.Combine(Application.dataPath, FolderName);

        private int _selectedFileIndex = 0;
        
        private string GenerateJsonPath() =>
            _targetAsset != null ? Path.Combine(SaveFolderPath, $"{_targetAsset.name}_{DateTime.Now:yyyyMMdd_HHmmss}.json") : null;

        [MenuItem("Tools/Save System Kit/Save Config Exporter Window")]
        private static void OpenWindow()
        {
            var window = GetWindow<SaveConfigExporterWindow>();
            window.titleContent = new GUIContent("Save Config Exporter Window");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        protected override void OnEnable()
        {
            Directory.CreateDirectory(SaveFolderPath);
            RefreshJsonFiles();
        }

        protected override void DrawEditor(int index)
        {
            DrawTargetAssetSection();

            if (_targetAsset == null)
                return;

            if (_targetAsset != _lastTargetAsset)
            {
                _lastTargetAsset = _targetAsset;
                RefreshJsonFiles();
            }

            GUILayout.Space(10);
            base.DrawEditor(index);
            DrawJsonControls();
        }

        private void DrawTargetAssetSection()
        {
            SirenixEditorGUI.BeginBox("Target Asset");
            _targetAsset = (ScriptableObject)SirenixEditorFields.UnityPreviewObjectField(
                _targetAsset, typeof(ScriptableObject), true);
            SirenixEditorGUI.EndBox();

            if (_targetAsset == null)
            {
                EditorGUILayout.HelpBox("Assign a ScriptableObject to export/import as JSON.", MessageType.Warning);
            }
        }

        private void DrawJsonControls()
        {
            SirenixEditorGUI.BeginBox("Save / Load JSON");

            GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
            if (GUILayout.Button("Save New JSON", GUILayout.Height(30))) 
                SaveNewJson();

            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);

            if (_jsonFiles.Count > 0)
            {
                GUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
                GUILayoutOption[] options = { GUILayout.Width(position.width * 0.4f), GUILayout.Height(30) };
                if (GUILayout.Button("Overwrite Selected", options))
                {
                    OverwriteJsonFile(_jsonFiles[_selectedFileIndex]);
                }

                GUI.backgroundColor = Color.white;
                _selectedFileIndex = EditorGUILayout
                    .Popup(_selectedFileIndex, _jsonFiles.Select(Path.GetFileName)
                    .ToArray());

                GUILayout.EndHorizontal();
            }

            GUIStyle centerStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("All Files", centerStyle);

            foreach (var filePath in _jsonFiles)
            {
                DrawJsonFileEntry(filePath);
            }

            SirenixEditorGUI.EndBox();
        }

        private void OverwriteJsonFile(string path)
        {
            try
            {
                string json = JsonUtility.ToJson(_targetAsset, true);
                File.WriteAllText(path, json);
                Debug.Log($"Overwrote JSON: {path}");
                RefreshJsonFiles();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to overwrite JSON: {e}");
            }
        }

        private void DrawJsonFileEntry(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
            if (GUILayout.Button("Load", GUILayout.Width(position.width * 0.3f)))
            {
                LoadJsonToTarget(filePath);
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Label(fileName, GUILayout.ExpandWidth(true));

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Delete", GUILayout.Width(200)))
            {
                DeleteJsonFile(filePath);
                return;
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void SaveNewJson()
        {
            try
            {
                string json = JsonUtility.ToJson(_targetAsset, true);
                string path = GenerateJsonPath();
                File.WriteAllText(path, json);
                Debug.Log($"Saved JSON to: {path}");
                RefreshJsonFiles();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save JSON: {e}");
            }
        }

        private void LoadJsonToTarget(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, _targetAsset);
                EditorUtility.SetDirty(_targetAsset);
                AssetDatabase.SaveAssets();
                Debug.Log($"Loaded JSON from: {Path.GetFileName(path)}");
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
                Debug.Log($"Deleted file: {Path.GetFileName(path)}");
                RefreshJsonFiles();
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
                .Where(path => _targetAsset != null && Path.GetFileName(path).StartsWith(_targetAsset.name))
                .ToList();
        }
    }
}
#endif