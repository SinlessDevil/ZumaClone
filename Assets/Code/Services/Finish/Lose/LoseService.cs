using Code.Services.Levels;
using Code.Services.LocalProgress;
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
            string elapsed = _timeService.GetFormattedElapsedTime();
            string scoreText = GetScoreText();
            
            RectTransform window = _windowService.Open(WindowTypeId.Lose);
            
            LoseWindow loseWindow = window.GetComponent<LoseWindow>();
            loseWindow.SetTime("Time: " + elapsed);
            loseWindow.SetScore(scoreText);
            loseWindow.Initialize();
            loseWindow.ResetWindow();
            loseWindow.OpenWindow(null, _levelLocalProgressService.Score, _timeService.GetElapsedTime());
        }
        
        private string GetScoreText()
        {
            int score = _levelLocalProgressService.Score;
            if (_levelService.Mode != ZumaMode.Adventure)
                return $"Score: {score}  {_levelService.GetGauntletRank(score)}";

            return _levelService.CanContinueAdventure ? $"Score: {score}  Lives: {_levelService.AdventureLives}" : $"Game Over  Score: {score}";
        }
    }
}