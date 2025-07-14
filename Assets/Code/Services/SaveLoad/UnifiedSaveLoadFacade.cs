using System;
using Code.Services.PersistenceProgress;
using Code.Services.PersistenceProgress.Player;

namespace Code.Services.SaveLoad
{
    public class UnifiedSaveLoadFacade : ISaveLoadFacade
    {
        private readonly ISaveLoadService _prefsService;
        private readonly ISaveLoadService _jsonService;
        private readonly ISaveLoadService _xmlService;
        private readonly IPersistenceProgressService _progressService;

        public UnifiedSaveLoadFacade(
            IPersistenceProgressService progressService)
        {
            _progressService = progressService;
            _prefsService = new PrefsSaveLoadService(_progressService);
            _jsonService = new JsonSaveLoadService(_progressService);
            _xmlService = new XmlSaveLoadService(_progressService);
        }

        public void SaveProgress(SaveMethodType methodType)
        {
            Save(methodType, _progressService.PlayerData);
        }
        
        public void Save(SaveMethodType methodType, PlayerData data)
        {
            GetService(methodType).Save(data);
        }

        public PlayerData Load(SaveMethodType methodType) => GetService(methodType).Load();

        private ISaveLoadService GetService(SaveMethodType methodType) => methodType switch
        {
            SaveMethodType.PlayerPrefs => _prefsService,
            SaveMethodType.Json => _jsonService,
            SaveMethodType.Xml => _xmlService,
            _ => throw new Exception("Unknown save methodType.")
        };
    }
}