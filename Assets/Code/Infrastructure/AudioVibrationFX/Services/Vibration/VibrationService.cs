using System.Threading;
using UnityEngine;
using MoreMountains.NiceVibrations;
using Cysharp.Threading.Tasks;
using Code.Infrastructure.AudioVibrationFX.Services.StaticData;
using Code.Infrastructure.AudioVibrationFX.StaticData;

namespace Code.Infrastructure.AudioVibrationFX.Services.Vibration
{
    public class VibrationService : IVibrationService
    {
        private readonly IAudioVibrationStaticDataService _staticDataService;

        private CancellationTokenSource _curveCts;

        public bool IsEnabled { get; private set; } = true;

        public VibrationService(IAudioVibrationStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
        }

        public void Play(VibrationType type)
        {
            var data = _staticDataService.VibrationsData.Vibrations.Find(v => v.VibrationType == type);
            if (data == null || !IsEnabled)
            {
                Debug.LogWarning($"[VibrationService] Vibration not found or disabled: {type}");
                return;
            }

            switch (data.Mode)
            {
                case VibrationMode.Preset:
                    MMVibrationManager.Haptic(data.HapticType);
                    break;

                case VibrationMode.Constant:
                    MMVibrationManager.ContinuousHaptic(
                        Mathf.Clamp01(data.ConstantIntensity),
                        0.5f,
                        data.ConstantDuration);
                    break;

                case VibrationMode.Emphasis:
                    MMVibrationManager.TransientHaptic(
                        Mathf.Clamp01(data.EmphasisIntensity),
                        Mathf.Clamp01(data.EmphasisSharpness));
                    break;

                case VibrationMode.CustomCurve:
                    _curveCts?.Cancel();
                    _curveCts = new CancellationTokenSource();
                    PlayCurveAsync(data.Curve, data.CurveDuration, _curveCts.Token).Forget();
                    break;
            }
        }

        public void Stop()
        {
            MMVibrationManager.StopAllHaptics();
            _curveCts?.Cancel();
            _curveCts = null;
        }

        private async UniTaskVoid PlayCurveAsync(AnimationCurve curve, float duration, CancellationToken token)
        {
            float time = 0f;
            float step = 0.05f;

            while (time < duration)
            {
                token.ThrowIfCancellationRequested();

                float t = time / duration;
                float intensity = Mathf.Clamp01(curve.Evaluate(t));
                MMVibrationManager.ContinuousHaptic(intensity, 1f, step);

                time += step;
                await UniTask.Delay((int)(step * 1000), cancellationToken: token);
            }

            MMVibrationManager.StopAllHaptics();
        }

    }
}
