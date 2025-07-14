using Code.Infrastructure.AudioVibrationFX.StaticData;
using UnityEngine;

namespace Code.Infrastructure.AudioVibrationFX.Services.StaticData
{
    public class AudioVibrationStaticDataService : IAudioVibrationStaticDataService
    {
        private const string SoundsDataPath = "StaticData/Sounds/Sounds";
        private const string VibrationDataPath = "StaticData/Vibration/VibrationsData";

        private SoundsData _soundsData;
        private VibrationsData _vibrationData;
        
        public SoundsData SoundsData => _soundsData;
        public VibrationsData VibrationsData => _vibrationData;

        public void LoadData()
        {
            _soundsData = Resources.Load<SoundsData>(SoundsDataPath);
            _vibrationData = Resources.Load<VibrationsData>(VibrationDataPath);
        }
    }
}