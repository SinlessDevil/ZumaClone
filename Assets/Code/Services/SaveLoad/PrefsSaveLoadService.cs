using System;
using Code.Services.PersistenceProgress;
using Code.Services.PersistenceProgress.Player;
using Sirenix.Serialization;
using UnityEngine;

namespace Code.Services.SaveLoad
{
    public class PrefsSaveLoadService : ISaveLoadService
    {
        private const string PlayerDataKey = "PlayerData";
        
        private readonly IPersistenceProgressService _progressService;

        public PrefsSaveLoadService(IPersistenceProgressService progressService)
        {
            _progressService = progressService;
        }
        
        public void SaveProgress() => Save(_progressService.PlayerData);
        
        public void Save(PlayerData playerData)
        {
            byte[] serializedValue = SerializationUtility.SerializeValue(playerData, DataFormat.JSON);
            string base64String = Convert.ToBase64String(serializedValue);
            PlayerPrefs.SetString(key: PlayerDataKey, base64String);
            PlayerPrefs.Save();
            Debug.Log($"PlayerData saved to PlayerPrefs: {PlayerDataKey}");
        }

        public PlayerData Load()
        {
            string base64String = PlayerPrefs.GetString(key: PlayerDataKey, string.Empty);

            if (base64String == string.Empty)
                return null;

            byte[] serializedValue = Convert.FromBase64String(base64String);
            return SerializationUtility.DeserializeValue<PlayerData>(serializedValue, DataFormat.JSON);
        }
    }
}