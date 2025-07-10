using System;
using System.IO;
using System.Xml.Serialization;
using Code.Infrastructure.Services.PersistenceProgress;
using Code.Infrastructure.Services.PersistenceProgress.Player;
using UnityEngine;

namespace Code.Infrastructure.Services.SaveLoad
{
    public class XmlSaveLoadService : ISaveLoadService
    {
        private const string FileName = "player_data.xml";

        private readonly IPersistenceProgressService _progressService;
        private readonly string _savePath;

        public XmlSaveLoadService(IPersistenceProgressService progressService)
        {
            _progressService = progressService;
            _savePath = Path.Combine(Application.persistentDataPath, FileName);
        }

        public void SaveProgress()
        {
            Save(_progressService.PlayerData);
        }

        public void Save(PlayerData playerData)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(PlayerData));
                using FileStream stream = new FileStream(_savePath, FileMode.Create);
                serializer.Serialize(stream, playerData);
                Debug.Log($"💾 XML Save complete at: {_savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to save XML: {e.Message}");
            }
        }

        public PlayerData Load()
        {
            try
            {
                if (!File.Exists(_savePath))
                {
                    Debug.LogWarning("⚠️ XML save file not found.");
                    return null;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(PlayerData));
                using FileStream stream = new FileStream(_savePath, FileMode.Open);
                var data = (PlayerData)serializer.Deserialize(stream);
                Debug.Log("✅ XML Load complete.");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to load XML: {e.Message}");
                return null;
            }
        }
    }
}
