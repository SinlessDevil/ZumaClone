#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Code.Infrastructure.Services.PersistenceProgress.Player;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Code.Editor
{
    public class ViewGameSavesWindow : OdinEditorWindow
    {
        private const string PlayerPrefsKey = "PlayerData";
        private const string JsonFileName = "player_data.json";
        private const string XmlFileName = "player_data.xml";

        private string SavePath => Application.persistentDataPath;
        private string JsonFilePath => Path.Combine(SavePath, JsonFileName);
        private string XmlFilePath => Path.Combine(SavePath, XmlFileName);

        private Vector2 scrollPrefs;
        private Vector2 scrollJson;
        private Vector2 scrollXml;

        private string DecodedPrefsData;
        private string DecodedJsonData;
        private string DecodedXmlData;

        private string PrefsMessage = "No PlayerPrefs data found.";
        private string JsonMessage = "No JSON file found.";
        private string XmlMessage = "No XML file found.";

        // Foldout toggles
        private bool showPlayerPrefs = true;
        private bool showJson = true;
        private bool showXml = true;

        [MenuItem("Tools/Save System Kit/View Game Saves Window")]
        private static void OpenWindow()
        {
            var window = GetWindow<ViewGameSavesWindow>();
            window.titleContent = new GUIContent("All Saves Window");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnEnable() => Refresh();

        protected override void DrawEditor(int index)
        {
            DrawFoldoutSection(ref showPlayerPrefs, "PlayerPrefs Preview", GetPlayerPrefsPath(), PrefsMessage, DecodedPrefsData, ref scrollPrefs, Refresh, DeletePlayerPrefs);
            GUILayout.Space(20);
            DrawFoldoutSection(ref showJson, "JSON File Preview", JsonFilePath, JsonMessage, DecodedJsonData, ref scrollJson, Refresh, DeleteJson);
            GUILayout.Space(20);
            DrawFoldoutSection(ref showXml, "XML File Preview", XmlFilePath, XmlMessage, DecodedXmlData, ref scrollXml, Refresh, DeleteXml);
        }

        private void DrawFoldoutSection(ref bool foldout, string title, string path, string message, string data, ref Vector2 scroll, Action refresh, Action delete)
        {
            foldout = SirenixEditorGUI.Foldout(foldout, title);
            if (!foldout) 
                return;

            SirenixEditorGUI.BeginBox();

            EditorGUILayout.LabelField("Save Location", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(path, MessageType.Info);

            GUILayout.Space(10);

            if (!string.IsNullOrEmpty(data))
            {
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(300));
                EditorGUILayout.TextArea(data, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox(message, MessageType.Warning);
            }

            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
            if (GUILayout.Button("Refresh", GUILayout.Height(35)))
                refresh.Invoke();

            GUILayout.Space(5);
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Delete", GUILayout.Height(35)))
                delete.Invoke();

            GUI.backgroundColor = Color.white;
            SirenixEditorGUI.EndBox();
        }

        private void Refresh()
        {
            RefreshPlayerPrefs();
            RefreshJsonFile();
            RefreshXmlFile();
            Repaint();
        }

        private void RefreshPlayerPrefs()
        {
            DecodedPrefsData = string.Empty;

            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                try
                {
                    string base64 = PlayerPrefs.GetString(PlayerPrefsKey);
                    byte[] data = Convert.FromBase64String(base64);
                    var deserialized = Sirenix.Serialization.SerializationUtility.DeserializeValue<PlayerData>(data, DataFormat.JSON);
                    string json = Encoding.UTF8.GetString(Sirenix.Serialization.SerializationUtility.SerializeValue(deserialized, DataFormat.JSON));
                    DecodedPrefsData = json;
                    PrefsMessage = string.Empty;
                }
                catch (Exception e)
                {
                    DecodedPrefsData = $"Failed to decode PlayerPrefs:\n{e.Message}";
                    PrefsMessage = "PlayerPrefs data decode failed.";
                }
            }
            else
            {
                PrefsMessage = "No PlayerPrefs data found.";
            }
        }

        private void RefreshJsonFile()
        {
            DecodedJsonData = string.Empty;

            if (File.Exists(JsonFilePath))
            {
                try
                {
                    DecodedJsonData = File.ReadAllText(JsonFilePath);
                    JsonMessage = string.Empty;
                }
                catch (Exception e)
                {
                    DecodedJsonData = $"Failed to read JSON:\n{e.Message}";
                    JsonMessage = "Failed to load JSON file.";
                }
            }
            else
            {
                JsonMessage = "No JSON file found.";
            }
        }

        private void RefreshXmlFile()
        {
            DecodedXmlData = string.Empty;

            if (File.Exists(XmlFilePath))
            {
                try
                {
                    DecodedXmlData = File.ReadAllText(XmlFilePath);
                    XmlMessage = string.Empty;
                }
                catch (Exception e)
                {
                    DecodedXmlData = $"Failed to read XML:\n{e.Message}";
                    XmlMessage = "Failed to load XML file.";
                }
            }
            else
            {
                XmlMessage = "No XML file found.";
            }
        }

        private void DeletePlayerPrefs()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                PlayerPrefs.DeleteKey(PlayerPrefsKey);
                PlayerPrefs.Save();
                Debug.Log("PlayerPrefs file deleted.");
                Refresh();
            }
        }

        private void DeleteJson()
        {
            if (File.Exists(JsonFilePath))
            {
                File.Delete(JsonFilePath);
                Debug.Log("JSON file deleted.");
                Refresh();
            }
        }

        private void DeleteXml()
        {
            if (File.Exists(XmlFilePath))
            {
                File.Delete(XmlFilePath);
                Debug.Log("XML file deleted.");
                Refresh();
            }
        }

        private string GetPlayerPrefsPath()
        {
#if UNITY_EDITOR_WIN
            return $@"Windows Registry:\HKEY_CURRENT_USER\Software\Unity\UnityEditor\{Application.companyName}\{Application.productName}";
#elif UNITY_EDITOR_OSX
            return $"~/Library/Preferences/unity.{Application.companyName}.{Application.productName}.plist";
#else
            return "📦 Platform not supported for PlayerPrefs path preview.";
#endif
        }
    }
}
#endif
