using Code.Services.Timer;
using TMPro;
using UnityEngine;
using Zenject;

namespace Code.UI.Game
{
    public class TimeDisplayer : MonoBehaviour
    {
        private const string TimeTextText = "00:00:00";
        
        [SerializeField] private TMP_Text _timeText;
        
        private bool _isTimerActive = false;
        
        private ITimeService _timeService;
        
        [Inject]
        public void Constructor(ITimeService timeService)
        {
            _timeService = timeService;
        }
        
        public void Initialize()
        {
            _timeText.text = TimeTextText;
            _isTimerActive = true;
        }

        public void Dispose()
        {
            _timeText.text = TimeTextText;
            _isTimerActive = false;
        }
        
        private void Update()
        {
            if(_isTimerActive) 
                SetScoreText();
        }
        
        private void SetScoreText()
        {
            _timeText.text = _timeService.GetFormattedElapsedTime();
        }
    }
}