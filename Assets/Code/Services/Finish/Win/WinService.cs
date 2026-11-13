using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.Services.PersistenceProgress;
using Code.Services.PersistenceProgress.Player;
using Code.Services.SaveLoad;
using Code.Services.Timer;
using Code.Services.Window;
using Code.Window;
using Code.Window.Finish.Win;
using UnityEngine;

namespace Code.Services.Finish.Win
{
    public class WinService : IWinService
    {
        private readonly IWindowService _windowService;
        private readonly ILevelLocalProgressService _levelLocalProgressService;
        private readonly ILevelService _levelService;
        private readonly ISaveLoadFacade _saveLoadFacade;
        private readonly IPersistenceProgressService _persistenceProgressService;
        private readonly ITimeService _timeService;

        public WinService(
            IWindowService windowService, 
            ILevelLocalProgressService levelLocalProgressService,
            ILevelService levelService,
            ISaveLoadFacade saveLoadFacade,
            IPersistenceProgressService persistenceProgressService,
            ITimeService timeService)
        {
            _windowService = windowService;
            _levelLocalProgressService = levelLocalProgressService;
            _levelService = levelService;
            _saveLoadFacade = saveLoadFacade;
            _persistenceProgressService = persistenceProgressService;
            _timeService = timeService;
        }
        
        public void Win()
        {
            (string, string) recordTime = GetRecordText();
            string scoreText = GetScoreText();
            CompleteLevel();
            CompleteTutor();
            
            SaveProgress();
            
            RectTransform window = _windowService.Open(WindowTypeId.Win);
            WinWindow winWindow = window.GetComponent<WinWindow>();
            winWindow.SetTime(recordTime.Item1 + recordTime.Item2);
            winWindow.SetScore(scoreText);
            winWindow.Initialize();
            winWindow.ResetWindow();
            winWindow.OpenWindow(null, _levelLocalProgressService.Score, _timeService.GetElapsedTime());
        }

        public void BonusWin()
        {
            (string, string) recordTime = GetRecordText();
            string scoreText = GetScoreText();
            CompleteLevel();
            CompleteTutor();
            
            SaveProgress();
            
            var window = _windowService.Open(WindowTypeId.Bonus);
            var bonusWindow = window.GetComponent<BonusWindow>();
            bonusWindow.SetTime(recordTime.Item1 + recordTime.Item2);
            bonusWindow.SetScore(scoreText);
            bonusWindow.Initialize();
            bonusWindow.ResetWindow();
            bonusWindow.OpenWindow(null, _levelLocalProgressService.Score, _timeService.GetElapsedTime());
        }
        
        private void CompleteLevel()
        {
            _levelService.LevelsComplete();
        }

        private void CompleteTutor()
        {
            if (_levelService.Mode == ZumaMode.Adventure)
                _persistenceProgressService.PlayerData.PlayerTutorialData.HasFirstCompleteLevel = true;
        }

        private float GetCurrentRecordTime()
        {
            LevelContainer currentLevelContainer = _levelService.GetCurrentLevelContainer();
            
            if(currentLevelContainer == null)
                return 0;
            
            return currentLevelContainer.Time;
        }

        private (string, string) GetRecordText()
        {
            if (_levelService.Mode != ZumaMode.Adventure)
                return ("Time: ", _timeService.GetFormattedElapsedTime());

            float currentRecordTime = GetCurrentRecordTime();
            float currentTime = _timeService.GetElapsedTime();
            
            if(currentRecordTime == 0 || currentTime < currentRecordTime)
                return ("New Record! Time: ", _timeService.GetFormattedElapsedTime());

            return ("Record: ", System.TimeSpan.FromSeconds(currentRecordTime).ToString(@"hh\:mm\:ss"));
        }
        
        private string GetScoreText()
        {
            int score = _levelLocalProgressService.Score;
            return _levelService.Mode == ZumaMode.Adventure
                ? $"Score: {score}  Lives: {_levelService.AdventureLives}"
                : $"Score: {score}  {_levelService.GetGauntletRank(score)}";
        }

        private void SaveProgress()
        {
            _saveLoadFacade.SaveProgress(SaveMethodType.PlayerPrefs);
        }
    }
}
