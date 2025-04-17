using Code.Services.Timer;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.UI.Game
{
    public class TimeDisplayer : MonoBehaviour
    {
        [SerializeField] private Text _timeText;
        
        private bool _isTimerActive = false;
        
        private ITimeService _timeService;
        
        [Inject]
        public void Constructor(ITimeService timeService)
        {
            _timeService = timeService;
        }

        private void Update()
        {
            if(_isTimerActive) 
                SetScoreText();
        }

        public void Initialize()
        {
            _timeText.text = "00:00:00";
            _isTimerActive = true;
        }

        public void Dispose()
        {
            _timeText.text = "00:00:00";
            _isTimerActive = false;
        }
        
        private void SetScoreText()
        {
            _timeText.text = _timeService.GetFormattedElapsedTime();
        }
    }
}