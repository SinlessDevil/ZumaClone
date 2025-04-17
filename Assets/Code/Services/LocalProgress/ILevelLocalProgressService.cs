using System;

namespace Code.Services.LocalProgress
{
    public interface ILevelLocalProgressService
    {
        void AddScore(int score);
        int Score { get; }
        event Action<int> UpdateScoreEvent;
        void Cleanup();
    }   
}