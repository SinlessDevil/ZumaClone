namespace Code.Services.Timer
{
    public interface ITimeService
    {
        void StartTimer();
        float GetElapsedTime();
        void StopTimer();
        string GetFormattedElapsedTime();
        void SlowMode();
        void SimpleMode();
        void Pause();
        void ResetTimer();
    }
}