using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoreMountains.NiceVibrations
{
    public class TransientHapticsDemoManager : DemoManager
    {
        [Header("Transient Haptics")]
        public MMProgressBar IntensityProgressBar;
        public MMProgressBar SharpnessProgressBar;
        public HapticCurve TargetCurve;
        public float TransientIntensity = 1f;
        public float TransientSharpness = 1f;
        public Text TransientIntensityText;
        public Text TransientSharpnessText;

        protected virtual void Start()
        {
            SharpnessProgressBar.UpdateBar(1f, 0f, 1f);
            IntensityProgressBar.UpdateBar(1f, 0f, 1f);
            TargetCurve.UpdateCurve(TransientIntensity, TransientSharpness);
        }

        public virtual void UpdateTransientIntensity(float newIntensity)
        {
            TransientIntensity = newIntensity;
            TransientIntensityText.text = NiceVibrationsDemoHelpers.Round(newIntensity, 2).ToString();
            IntensityProgressBar.UpdateBar(TransientIntensity, 0f, 1f);
            TargetCurve.UpdateCurve(TransientIntensity, TransientSharpness);
        }

        public virtual void UpdateTransientSharpness(float newSharpness)
        {
            TransientSharpness = newSharpness;
            TransientSharpnessText.text = NiceVibrationsDemoHelpers.Round(newSharpness, 2).ToString();
            SharpnessProgressBar.UpdateBar(TransientSharpness, 0f, 1f);
            TargetCurve.UpdateCurve(TransientIntensity, TransientSharpness);
        }

        public virtual void TransientHapticsButton()
        {
            MMVibrationManager.TransientHaptic(TransientIntensity, TransientSharpness, true, this);
            StartCoroutine(Logo.Shake(0.2f));
            DebugAudioTransient.volume = TransientIntensity;
            DebugAudioTransient.pitch = 0.5f + TransientSharpness / 2f;
            DebugAudioTransient.Play();
        }

        protected virtual void OnHapticsStopped()
        {
            ResetPlayState();
        }

        protected virtual void ResetPlayState()
        {


        }

        protected virtual void OnEnable()
        {
            MMNViOSCoreHaptics.OnHapticPatternStopped += OnHapticsStopped;
        }

        protected virtual void OnDisable()
        {
            MMNViOSCoreHaptics.OnHapticPatternStopped -= OnHapticsStopped;
        }
    }
}
