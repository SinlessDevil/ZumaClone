using System;

namespace Code.Services.LocalProgress
{
    public class LevelLocalProgressService : ILevelLocalProgressService
    {
        public event Action<int> UpdateScoreEvent; 
        
        public int Score { get; private set; }
        
        public void AddScore(int score)
        {
            Score += score;
            UpdateScoreEvent?.Invoke(Score);
        }
        
        public void Cleanup()
        {
            Score = 0;
        }
    }
}