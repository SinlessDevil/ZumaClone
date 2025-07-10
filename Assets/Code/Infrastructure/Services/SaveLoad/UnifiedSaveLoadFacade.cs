using System;
using Code.Infrastructure.Services.PersistenceProgress;
using Code.Infrastructure.Services.PersistenceProgress.Player;

namespace Code.Infrastructure.Services.SaveLoad
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

        public void SaveProgress(SaveMethod method)
        {
            Save(method, _progressService.PlayerData);
        }
        
        public void Save(SaveMethod method, PlayerData data)
        {
            GetService(method).Save(data);
        }

        public PlayerData Load(SaveMethod method)
        {
            return GetService(method).Load();
        }
        
        private ISaveLoadService GetService(SaveMethod method) => method switch
        {
            SaveMethod.PlayerPrefs => _prefsService,
            SaveMethod.Json => _jsonService,
            SaveMethod.Xml => _xmlService,
            _ => throw new Exception("Unknown save method.")
        };
    }
}