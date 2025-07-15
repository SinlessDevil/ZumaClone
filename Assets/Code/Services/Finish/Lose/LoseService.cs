using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.Services.PersistenceProgress.Player;
using Code.Services.Timer;
using Code.Services.Window;
using Code.Window;
using Code.Window.Finish.Lose;
using UnityEngine;

namespace Code.Services.Finish.Lose
{
    public class LoseService : ILoseService
    {
        private readonly IWindowService _windowService;
        private readonly ILevelService _levelService;
        private readonly ITimeService _timeService;
        private readonly ILevelLocalProgressService _levelLocalProgressService;

        public LoseService(
            IWindowService windowService, 
            ILevelService levelService,
            ITimeService timeService,
            ILevelLocalProgressService levelLocalProgressService)
        {
            _windowService = windowService;
            _levelService = levelService;
            _timeService = timeService;
            _levelLocalProgressService = levelLocalProgressService;
        }
        
        public void Lose()
        {
            (string, string) recordTime = GetRecordText();
            string scoreText = GetScoreText();
            
            RectTransform window = _windowService.Open(WindowTypeId.Lose);
            
            LoseWindow loseWindow = window.GetComponent<LoseWindow>();
            loseWindow.SetTime(recordTime.Item1 + recordTime.Item2);
            loseWindow.SetScore(scoreText);
            loseWindow.Initialize();
            loseWindow.ResetWindow();
            loseWindow.OpenWindow(null, _levelLocalProgressService.Score, _timeService.GetElapsedTime());
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
    }
}