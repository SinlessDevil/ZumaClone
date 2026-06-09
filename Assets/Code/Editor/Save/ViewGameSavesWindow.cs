#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Code.Editor
{
    public class ViewGameSavesWindow : EditorWindow
    {
        private const string PlayerPrefsKey = "PlayerData";
        private const string JsonFileName = "player_data.json";
        private const string XmlFileName = "player_data.xml";
        private const string UxmlPath = "Assets/Code/Editor/Save/ViewGameSavesWindow.uxml";
        private const string UssPath = "Assets/Code/Editor/Save/ViewGameSavesWindow.uss";

        private string SavePath => Application.persistentDataPath;
        private string JsonFilePath => Path.Combine(SavePath, JsonFileName);
        private string XmlFilePath => Path.Combine(SavePath, XmlFileName);

        private TextField _prefsText;
        private HelpBox _prefsWarn;
        private TextField _jsonText;
        private HelpBox _jsonWarn;
        private TextField _xmlText;
        private HelpBox _xmlWarn;

        [MenuItem("Tools/Save System Kit/View Game Saves Window")]
        private static void OpenWindow()
        {
            var window = GetWindow<ViewGameSavesWindow>();
            window.titleContent = new GUIContent("All Saves Window");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        public void CreateGUI()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            uxml.CloneTree(rootVisualElement);

            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            rootVisualElement.styleSheets.Add(uss);

            BindSection("prefs", out _prefsText, out _prefsWarn,
                GetPlayerPrefsPath(), RefreshPlayerPrefs, DeletePlayerPrefs);

            BindSection("json", out _jsonText, out _jsonWarn,
                JsonFilePath, RefreshJsonFile, DeleteJson);

            BindSection("xml", out _xmlText, out _xmlWarn,
                XmlFilePath, RefreshXmlFile, DeleteXml);

            Refresh();
        }

        private void BindSection(string prefix, out TextField textField, out HelpBox warnBox,
            string pathText, Action onRefresh, Action onDelete)
        {
            rootVisualElement.Q<HelpBox>($"{prefix}-path-help").text = pathText;

            textField = rootVisualElement.Q<TextField>($"{prefix}-text");
            textField.isReadOnly = true;

            warnBox = rootVisualElement.Q<HelpBox>($"{prefix}-warn");

            rootVisualElement.Q<Button>($"{prefix}-refresh-btn").clicked += onRefresh;
            rootVisualElement.Q<Button>($"{prefix}-delete-btn").clicked += onDelete;
        }

        private void Refresh()
        {
            RefreshPlayerPrefs();
            RefreshJsonFile();
            RefreshXmlFile();
        }

        private void UpdateSection(TextField textField, HelpBox warnBox, string data, string emptyMessage)
        {
            bool hasData = !string.IsNullOrEmpty(data);
            textField.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            warnBox.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;

            if (hasData)
                textField.value = data;
            else
                warnBox.text = emptyMessage;
        }

        private void RefreshPlayerPrefs()
        {
            string data = string.Empty;

            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                try
                {
                    byte[] bytes = Convert.FromBase64String(PlayerPrefs.GetString(PlayerPrefsKey));
                    data = Encoding.UTF8.GetString(bytes);
                }
                catch (Exception e)
                {
                    data = $"Failed to decode PlayerPrefs:\n{e.Message}";
                }
            }

            UpdateSection(_prefsText, _prefsWarn, data, "No PlayerPrefs data found.");
        }

        private void RefreshJsonFile()
        {
            string data = string.Empty;

            if (File.Exists(JsonFilePath))
            {
                try { data = File.ReadAllText(JsonFilePath); }
                catch (Exception e) { data = $"Failed to read JSON:\n{e.Message}"; }
            }

            UpdateSection(_jsonText, _jsonWarn, data, "No JSON file found.");
        }

        private void RefreshXmlFile()
        {
            string data = string.Empty;

            if (File.Exists(XmlFilePath))
            {
                try { data = File.ReadAllText(XmlFilePath); }
                catch (Exception e) { data = $"Failed to read XML:\n{e.Message}"; }
            }

            UpdateSection(_xmlText, _xmlWarn, data, "No XML file found.");
        }

        private void DeletePlayerPrefs()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey))
                return;
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
            RefreshPlayerPrefs();
        }

        private void DeleteJson()
        {
            if (!File.Exists(JsonFilePath))
                return;
            File.Delete(JsonFilePath);
            RefreshJsonFile();
        }

        private void DeleteXml()
        {
            if (!File.Exists(XmlFilePath))
                return;
            File.Delete(XmlFilePath);
            RefreshXmlFile();
        }

        private string GetPlayerPrefsPath()
        {
#if UNITY_EDITOR_WIN
            return $@"Windows Registry:\HKEY_CURRENT_USER\Software\Unity\UnityEditor\{Application.companyName}\{Application.productName}";
#elif UNITY_EDITOR_OSX
            return $"~/Library/Preferences/unity.{Application.companyName}.{Application.productName}.plist";
#else
            return "Platform not supported for PlayerPrefs path preview.";
#endif
        }
    }
}
#endif
