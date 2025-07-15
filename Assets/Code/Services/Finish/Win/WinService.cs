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
            CompleteLevel();
            
            CompleteTutor();

            (string, string) recordTime = GetRecordText();
            string scoreText = GetScoreText();

            SetRecordText();
            
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
            CompleteLevel();
            
            CompleteTutor();

            (string, string) recordTime = GetRecordText();
            string scoreText = GetScoreText();

            SetRecordText();
            
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
            _persistenceProgressService.PlayerData.PlayerTutorialData.HasFirstCompleteLevel = true;
        }

        private void SetRecordText()
        {
            float currentRecordTime = GetCurrentRecordTime();
            float currentTime = _timeService.GetElapsedTime();
            LevelContainer currentLevelContainer = _levelService.GetCurrentLevelContainer();
            
            if(currentRecordTime == 0)
                return;   

            if (!(currentTime > currentRecordTime)) 
                return;
            
            LevelContainer existingLevel = _persistenceProgressService.PlayerData.PlayerLevelData.LevelsComleted.Find(level => level == currentLevelContainer);
            existingLevel.Time = currentTime;
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
            float currentRecordTime = GetCurrentRecordTime();
            float currentTime = _timeService.GetElapsedTime();
            
            if(currentRecordTime == 0 || currentTime > currentRecordTime)
                return ("New Record! Time: ", _timeService.GetFormattedElapsedTime());

            return ("Record: ", _timeService.GetFormattedElapsedTime());
        }
        
        private string GetScoreText() => "Score: " + _levelLocalProgressService.Score;

        private void SaveProgress()
        {
            _saveLoadFacade.SaveProgress(SaveMethodType.PlayerPrefs);
        }
    }
}
