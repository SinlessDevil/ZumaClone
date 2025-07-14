namespace Code.Infrastructure.AudioVibrationFX.Services.Vibration
{
    public interface IVibrationService
    {
        void Play(VibrationType type);
        void Stop();
        bool IsEnabled { get; }
    }
}