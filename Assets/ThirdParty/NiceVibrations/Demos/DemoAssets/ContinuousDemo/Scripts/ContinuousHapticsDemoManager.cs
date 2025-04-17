using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoreMountains.NiceVibrations
{
    public class ContinuousHapticsDemoManager : DemoManager
    {
        [Header("Texts")]        
        public float ContinuousIntensity = 1f;
        public float ContinuousSharpness = 1f;
        public float ContinuousDuration = 3f;
        public Text ContinuousIntensityText;
        public Text ContinuousSharpnessText;
        public Text ContinuousDurationText;
        public Text ContinuousButtonText;
        [Header("Interface")]
        public MMTouchButton ContinuousButton;
        public MMProgressBar IntensityProgressBar;
        public MMProgressBar SharpnessProgressBar;
        public MMProgressBar DurationProgressBar;
        public MMProgressBar ContinuousProgressBar;
        public HapticCurve TargetCurve;
        public Slider DurationSlider;

        protected float _timeLeft;
        protected Color _continuousButtonOnColor = new Color32(216, 85, 85, 255);
        protected Color _continuousButtonOffColor = new Color32(242, 27, 80, 255);
        protected bool _continuousActive = false;
        protected float _intensityLastFrame = -1f;
        protected float _sharpnessLastFrame = -1f;

        /// <summary>
        /// On Awake, we initialize our iOS haptics.
        /// Of course, this only needs to be done when on iOS, or targeting iOS. 
        /// A test will be done and this method will do nothing if running on anything else
        /// </summary>
        protected virtual void Awake()
        {
            //MMVibrationIOS.iOSInitializeHaptics ();
            ContinuousButton.ReturnToInitialSpriteAutomatically = false;

            ContinuousIntensityText.text = ContinuousIntensity.ToString();
            ContinuousSharpnessText.text = ContinuousSharpness.ToString();
            ContinuousDurationText.text = ContinuousDuration.ToString();

            IntensityProgressBar.UpdateBar(ContinuousIntensity, 0f, 1f);
            SharpnessProgressBar.UpdateBar(ContinuousSharpness, 0f, 1f);
            DurationProgressBar.UpdateBar(ContinuousDuration, 0f, 5f);
        }
        
        protected virtual void Update()
        {
            UpdateContinuousDemo();
        }

        protected virtual void UpdateContinuousDemo()
        {
            if (_timeLeft > 0f)
            {
                ContinuousProgressBar.UpdateBar(_timeLeft, 0f, ContinuousDuration);
                _timeLeft -= Time.deltaTime;
                Logo.Shaking = true;
                TargetCurve.Move = true;
                Logo.Intensity = NiceVibrationsDemoHelpers.Remap(ContinuousIntensity, 0f, 1f, 1f, 8f);
                Logo.Sharpness = NiceVibrationsDemoHelpers.Remap(ContinuousSharpness, 0f, 1f, 10f, 25f);
            }
            else
            {
                ContinuousProgressBar.UpdateBar(0f, 0f, ContinuousDuration);
                Logo.Shaking = false;
                TargetCurve.Move = false;
                if (_continuousActive)
                {
                    MMVibrationManager.StopContinuousHaptic(true);
                    OnHapticsStopped();
                }
            }
            if ((_sharpnessLastFrame != ContinuousSharpness) || (_intensityLastFrame != ContinuousIntensity))
            {
                TargetCurve.UpdateCurve(ContinuousIntensity, ContinuousSharpness);
            }
            _intensityLastFrame = ContinuousIntensity;
            _sharpnessLastFrame = ContinuousSharpness;
        }

        public virtual void UpdateContinuousIntensity(float newIntensity)
        {
            ContinuousIntensity = newIntensity;
            IntensityProgressBar.UpdateBar(ContinuousIntensity, 0f, 1f);
            ContinuousIntensityText.text = NiceVibrationsDemoHelpers.Round(newIntensity, 2).ToString();
            UpdateContinuous();
        }

        public virtual void UpdateContinuousSharpness(float newSharpness)
        {
            ContinuousSharpness = newSharpness;
            SharpnessProgressBar.UpdateBar(ContinuousSharpness, 0f, 1f);
            ContinuousSharpnessText.text = NiceVibrationsDemoHelpers.Round(newSharpness, 2).ToString();
            UpdateContinuous();
        }

        public virtual void UpdateContinuousDuration(float newDuration)
        {
            ContinuousDuration = newDuration;
            DurationProgressBar.UpdateBar(ContinuousDuration, 0f, 5f);
            ContinuousDurationText.text = NiceVibrationsDemoHelpers.Round(newDuration, 2).ToString();
        }

        protected virtual void UpdateContinuous()
        {
            if (_continuousActive)
            {
                MMVibrationManager.UpdateContinuousHaptic(ContinuousIntensity, ContinuousSharpness, true, -1, true);
                DebugAudioContinuous.volume = ContinuousIntensity;
                DebugAudioContinuous.pitch = 0.5f + ContinuousSharpness / 2f;
            }
        }

        public virtual void ContinuousHapticsButton()
        {
            if (!_continuousActive)
            {
                // START
                MMVibrationManager.ContinuousHaptic(ContinuousIntensity, ContinuousSharpness, ContinuousDuration, HapticTypes.LightImpact, this, true, -1, true);
                _timeLeft = ContinuousDuration;
                ContinuousButtonText.text = "Stop continuous haptic pattern";
                DurationSlider.interactable = false;
                _continuousActive = true;
                DebugAudioContinuous.Play();
            }
            else
            {
                // STOP
                MMVibrationManager.StopContinuousHaptic(true);
                ResetPlayState();
            }
        }

        protected virtual void OnHapticsStopped()
        {
            ResetPlayState();
        }

        protected virtual void OnHapticsError()
        {

        }

        protected virtual void OnHapticsReset()
        {

        }

        protected virtual void ResetPlayState()
        {
            _timeLeft = 0f;
            ContinuousButtonText.text = "Play continuous haptic pattern";
            _continuousActive = false;
            DebugAudioContinuous?.Stop();
            DurationSlider.interactable = true;

        }

        protected virtual void OnEnable()
        {
            MMNViOSCoreHaptics.OnHapticPatternStopped += OnHapticsStopped;
            MMNViOSCoreHaptics.OnHapticPatternError += OnHapticsError;
            MMNViOSCoreHaptics.OnHapticPatternReset += OnHapticsReset;
        }

        protected virtual void OnDisable()
        {
            MMNViOSCoreHaptics.OnHapticPatternStopped -= OnHapticsStopped;
            MMNViOSCoreHaptics.OnHapticPatternError -= OnHapticsError;
            MMNViOSCoreHaptics.OnHapticPatternReset -= OnHapticsReset;
        }
    }
}