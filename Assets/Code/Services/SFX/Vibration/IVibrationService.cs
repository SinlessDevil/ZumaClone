namespace Code.Services.SFX.Vibration
{
    public interface IVibrationService
    {
        void Play(VibrationType type);
        void Stop();
        bool IsEnabled { get; }
    }
}