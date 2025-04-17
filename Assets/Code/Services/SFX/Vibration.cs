using System.Collections;
using MoreMountains.NiceVibrations;
using UnityEngine;

namespace Code.Services.SFX
{
    public class Vibration : MonoBehaviour
    {
        public static Vibration Instance;

        public static bool player_prefs_vibration_enabled { get; set; } =
            true; // caching value to reduce reading from playerprefs during gameplay

        public static bool player_prefs_vibration_force_off { get; set; } =
            false; // caching value to reduce reading from playerprefs during gameplay

        public static bool IsIOS => Application.platform == RuntimePlatform.IPhonePlayer;

        private void Awake()
        {
            if (!PlayerPrefs.HasKey("vibration_enabled"))
            {
                SetVibration(true, false);
            }

            if (Instance == null)
                SetInstance();
            else
                Destroy(gameObject);
        }


        private void SetInstance()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void SetVibration(bool state, bool change)
        {
            if (change)
            {
                if (state)
                {
                    state = false;
                    StopContinuousHaptic();
                }
                else
                {
                    state = true;
                }
            }

            player_prefs_vibration_enabled = state;
        }

        public static IEnumerator VibrationPulses(float intensity, int pulses_amount, float delay, bool fadeout,
            bool iosOnly = false)
        {
            if (!IsIOS)
                intensity *= 0.65f;

            float sharpness = 1.0f;
            float fade_curve = 1f;
            float fadeout_step = intensity / pulses_amount;
            // print ("vibration fadeout_step " + fadeout_step);
            for (int i = 1; i <= pulses_amount; i++)
            {
                Vibration.TransientHaptic(Mathf.Pow(intensity, fade_curve), sharpness, iosOnly);
                if (fadeout)
                {
                    intensity -= fadeout_step;
                    intensity = Mathf.Max(intensity, 0.01f);
                    delay += fadeout_step;
                }

                yield return new WaitForSeconds(delay);
            }
        }


        public static void Selection(bool iosOnly = false)
        {
            Haptic(HapticTypes.Selection, iosOnly);
        }

        public static void LightImpact(bool iosOnly = false)
        {
            Haptic(HapticTypes.LightImpact, iosOnly);
        }

        public static void MediumImpact(bool iosOnly = false)
        {
            Haptic(HapticTypes.MediumImpact, iosOnly);
        }

        public static void SoftImpact(bool iosOnly = false)
        {
            Haptic(HapticTypes.SoftImpact, iosOnly);
        }

        public static void RigidImpact(bool iosOnly = false)
        {
            Haptic(HapticTypes.RigidImpact, iosOnly);
        }

        public static void HeavyImpact(bool iosOnly = false)
        {
            Haptic(HapticTypes.HeavyImpact, iosOnly);
        }

        public static void Failure(bool iosOnly = false)
        {
            Haptic(HapticTypes.Failure, iosOnly);
        }

        public static void SelectInactive(bool iosOnly = false)
        {
            Haptic(HapticTypes.Warning, iosOnly);
        }

        public static void TransientHaptic(float intensity, float sharpness, bool iosOnly = false)
        {
            if (IsHapticAllowed(iosOnly))
            {
                if (Application.platform == RuntimePlatform.Android)
                    ContinuousHaptic(intensity * 0.33f, sharpness, 0.04f * Mathf.Max(0.1f, sharpness), iosOnly);
                else
                    MMVibrationManager.TransientHaptic(Mathf.Clamp01(intensity), sharpness);
            }
        }

        public static void ContinuousHaptic(float intensity, float sharpness, float duration, bool iosOnly = false)
        {
            if (IsHapticAllowed(iosOnly))
            {
                MMVibrationManager.ContinuousHaptic(Mathf.Clamp01(intensity), Mathf.Clamp01(sharpness), duration);
            }
        }

        public static void StopContinuousHaptic(bool iosOnly = false)
        {
            if (IsHapticAllowed(iosOnly))
            {
                MMVibrationManager.StopContinuousHaptic();
            }
        }

        public static void AdvancedHapticPattern(TextAsset ahap, bool iosOnly = false)
        {
            //print("vibrate");
            if (IsHapticAllowed(iosOnly))
            {
                if (Application.platform != RuntimePlatform.Android)
                {
                    //MMVibrationManager.AdvancedHapticPattern(ahap.text, null, null, -1, HapticTypes.LightImpact);
                    MMVibrationManager.Haptic(HapticTypes.LightImpact, alsoRumble: true);
                }
                else
                    MMVibrationManager.Haptic(HapticTypes.Failure);
            }
        }

        private static void Haptic(HapticTypes hapticType, bool iosOnly)
        {
            if (IsHapticAllowed(iosOnly))
            {
                MMVibrationManager.Haptic(hapticType);
            }
        }

        private static bool IsHapticAllowed(bool iosOnly)
        {
            return player_prefs_vibration_force_off == false && player_prefs_vibration_enabled  && !(iosOnly && !IsIOS);
        }

        private static void SetAmplitudes()
        {
            SetAmplitude(ref MMVibrationManager.HeavyAmplitude, 255);
            SetAmplitude(ref MMVibrationManager.LightAmplitude, 40);
            SetAmplitude(ref MMVibrationManager.MediumAmplitude, 120);
            SetAmplitude(ref MMVibrationManager.RigidAmplitude, 255);
            SetAmplitude(ref MMVibrationManager.SoftAmplitude, 40);
        }

        private static void SetAmplitude(ref int value, int defaultValue)
        {
            value = (int)Mathf.Clamp(defaultValue * 1, 0, 255);
        }
    }
}