using System;
using UnityEngine;

namespace Code.Services.Timer
{
    public class TimeService : MonoBehaviour, ITimeService
    {
        private float _startTime;
        private float _elapsedTime;
        private bool _isRunning;

        public void StartTimer()
        {
            _startTime = Time.unscaledTime - _elapsedTime;
            _isRunning = true;
        }

        public void StopTimer()
        {
            _elapsedTime = Time.unscaledTime - _startTime;
            _isRunning = false;
        }
        
        public float GetElapsedTime()
        {
            if (_isRunning)
                return Time.unscaledTime - _startTime;
            return _elapsedTime;
        }

        public string GetFormattedElapsedTime()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(GetElapsedTime());
            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        public void ResetTimer()
        {
            _startTime = 0f;
            _elapsedTime = 0f;
            _isRunning = false;
        }
        
        public void SlowMode()
        {
            Time.timeScale = 0.2f;
        }

        public void SimpleMode()
        {
            Time.timeScale = 1f;
        }

        public void Pause()
        {
            Time.timeScale = 0f;
        }
    }
}