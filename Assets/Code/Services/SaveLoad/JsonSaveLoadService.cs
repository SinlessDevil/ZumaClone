using System;
using System.IO;
using Code.Services.PersistenceProgress;
using Code.Services.PersistenceProgress.Player;
using Sirenix.Serialization;
using UnityEngine;

namespace Code.Services.SaveLoad
{
    public class JsonSaveLoadService : ISaveLoadService
    {
        private const string FileName = "player_data.json";

        private readonly IPersistenceProgressService _progressService;
        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public JsonSaveLoadService(IPersistenceProgressService progressService)
        {
            _progressService = progressService;
        }

        public void SaveProgress() => Save(_progressService.PlayerData);

        public void Save(PlayerData playerData)
        {
            try
            {
                byte[] data = SerializationUtility.SerializeValue(playerData, DataFormat.JSON);
                File.WriteAllBytes(FilePath, data);

                Debug.Log($"JSON Save complete at: {FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save JSON: {e.Message}");
            }
        }

        public PlayerData Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    Debug.LogWarning($"JSON file not found at: {FilePath}");
                    return null;
                }

                byte[] data = File.ReadAllBytes(FilePath);
                return SerializationUtility.DeserializeValue<PlayerData>(data, DataFormat.JSON);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load JSON: {e.Message}");
                return null;
            }
        }
    }
}