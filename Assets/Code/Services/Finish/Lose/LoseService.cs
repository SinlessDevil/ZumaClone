using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.Services.Timer;
using Code.Services.Window;
using Code.Window;
using Code.Window.Finish.Lose;

namespace Code.Services.Finish.Lose
{
    public class LoseService : ILoseService
    {
        private IWindowService _windowService;
        private ILevelService _levelService;
        private ITimeService _timeService;
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
            var recordTime = GetRecordText();
            var scoreText = GetScoreText();
            
            var window = _windowService.Open(WindowTypeId.Lose);
            var loseWindow = window.GetComponent<LoseWindow>();
            loseWindow.SetTime(recordTime);
            loseWindow.SetScore(scoreText);
            loseWindow.Initialize();
            loseWindow.ResetWindow();
            loseWindow.OpenWindow(null);
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

    }
}