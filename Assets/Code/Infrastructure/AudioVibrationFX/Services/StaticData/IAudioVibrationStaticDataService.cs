using Code.Infrastructure.AudioVibrationFX.StaticData;

namespace Code.Infrastructure.AudioVibrationFX.Services.StaticData
{
    public interface IAudioVibrationStaticDataService
    {
        SoundsData SoundsData { get; }
        VibrationsData VibrationsData { get; }
        void LoadData();
    }
}