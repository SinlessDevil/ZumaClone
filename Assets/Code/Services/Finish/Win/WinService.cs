using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.Services.PersistenceProgress;
using Code.Services.SaveLoad;
using Code.Services.Timer;
using Code.Services.Window;
using Code.Window;
using Code.Window.Finish.Win;

namespace Code.Services.Finish.Win
{
    public class WinService : IWinService
    {
        private readonly IWindowService _windowService;
        private readonly ILevelLocalProgressService _levelLocalProgressService;
        private readonly ILevelService _levelService;
        private readonly ISaveLoadService _saveLoadService;
        private readonly IPersistenceProgressService _persistenceProgressService;
        private readonly ITimeService _timeService;

        public WinService(
            IWindowService windowService, 
            ILevelLocalProgressService levelLocalProgressService,
            ILevelService levelService,
            ISaveLoadService saveLoadService,
            IPersistenceProgressService persistenceProgressService,
            ITimeService timeService)
        {
            _windowService = windowService;
            _levelLocalProgressService = levelLocalProgressService;
            _levelService = levelService;
            _saveLoadService = saveLoadService;
            _persistenceProgressService = persistenceProgressService;
            _timeService = timeService;
        }
        
        public void Win()
        {
            CompleteLevel();
            
            CompleteTutor();

            var recordTime = GetRecordText();
            var scoreText = GetScoreText();

            SetRecordText();
            
            SaveProgress();
            
            var window = _windowService.Open(WindowTypeId.Win);
            var winWindow = window.GetComponent<WinWindow>();
            winWindow.SetTime(recordTime);
            winWindow.SetScore(scoreText);
            winWindow.Initialize();
            winWindow.ResetWindow();
            winWindow.OpenWindow(null);
        }

        public void BonusWin()
        {
            CompleteLevel();
            
            CompleteTutor();

            var recordTime = GetRecordText();
            var scoreText = GetScoreText();

            SetRecordText();
            
            SaveProgress();
            
            var window = _windowService.Open(WindowTypeId.Bonus);
            var bonusWindow = window.GetComponent<BonusWindow>();
            bonusWindow.SetTime(recordTime);
            bonusWindow.SetScore(scoreText);
            bonusWindow.Initialize();
            bonusWindow.ResetWindow();
            bonusWindow.OpenWindow(null);
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
            var currentRecordTime = GetCurrentRecordTime();
            var currentTime = _timeService.GetElapsedTime();
            var currentLevelContainer = _levelService.GetCurrentLevelContainer();
            
            if(currentRecordTime == 0)
            { 
                return;   
            }
            
            if (currentTime > currentRecordTime)
            {
                var existingLevel = _persistenceProgressService.PlayerData.PlayerLevelData.LevelsComleted.Find(level => level == currentLevelContainer);
                existingLevel.Time = currentTime;
            }
        }

        private float GetCurrentRecordTime()
        {
            var currentLevelContainer = _levelService.GetCurrentLevelContainer();
            if(currentLevelContainer == null)
            {
                return 0;
            }
            
            return currentLevelContainer.Time;
        }

        private string GetRecordText()
        {
            var currentRecordTime = GetCurrentRecordTime();
            var currentTime = _timeService.GetElapsedTime();
            
            if(currentRecordTime == 0 || currentTime > currentRecordTime)
            {
                return "New Record! Time: " + _timeService.GetFormattedElapsedTime();
            }

            return "Record: " + _timeService.GetFormattedElapsedTime();
        }

        private string GetScoreText()
        {
            return "Score: " + _levelLocalProgressService.Score;
        }
        
        private void SaveProgress()
        {
            _saveLoadService.SaveProgress();
        }
    }
}
