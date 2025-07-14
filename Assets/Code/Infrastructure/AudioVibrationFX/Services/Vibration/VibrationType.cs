using System;

namespace Code.Infrastructure.AudioVibrationFX.Services.Vibration
{
    [Serializable]
    public enum VibrationType
    {
        Unknown = -1,
        SuccessPreset = 0,
        ConstantBuzz = 1,
        HitPulse = 2,
        WaveVibration = 3,
    }
}
