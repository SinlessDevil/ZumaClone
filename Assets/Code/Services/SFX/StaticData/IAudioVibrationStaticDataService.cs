using Code.StaticData.SFX;

namespace Code.Services.SFX.StaticData
{
    public interface IAudioVibrationStaticDataService
    {
        SoundsData SoundsData { get; }
        VibrationsData VibrationsData { get; }
        void LoadData();
    }
}