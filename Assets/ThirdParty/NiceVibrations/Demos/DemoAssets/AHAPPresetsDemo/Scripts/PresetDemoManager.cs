using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoreMountains.NiceVibrations
{
    [Serializable]
    public class PresetDemoItem
    {
        public string Name;
        public TextAsset AHAPFile;
        public Sprite AssociatedSprite;
        public AudioSource AssociatedSound;
        public MMNVAndroidWaveFormAsset WaveFormAsset;
        public MMNVRumbleWaveFormAsset RumbleWaveFormAsset;
    }

    public class PresetDemoManager : DemoManager
    {
        [Header("Image")]
        public Image IconImage;
        public Animator IconImageAnimator;
        public List<PresetDemoItem> DemoItems;

        protected WaitForSeconds _ahapTurnDelay;
        protected int _idleAnimationParameter;

        protected virtual void Awake()
        {                
            _ahapTurnDelay = new WaitForSeconds(0.02f);
            _idleAnimationParameter = Animator.StringToHash("Idle");
            IconImageAnimator.SetBool(_idleAnimationParameter, true);
        }
        
        // AHAP ------------------------------------------------------------------------------------------------

        public virtual void PlayAHAP(int index)
        {
            Logo.Shaking = true;

            // for the purpose of the demo, and to be able to observe the difference, if any, on certain devices,
            // the first 4 effects (dice, drums, game over, heart beats) will be called on the main thread, and the remaining ones on a secondary thread
            if (index < 5)
            {
                MMVibrationManager.AdvancedHapticPattern(DemoItems[index].AHAPFile.text,
                                                     DemoItems[index].WaveFormAsset.WaveForm.Pattern, DemoItems[index].WaveFormAsset.WaveForm.Amplitudes, -1,
                                                     DemoItems[index].RumbleWaveFormAsset.WaveForm.Pattern, DemoItems[index].RumbleWaveFormAsset.WaveForm.LowFrequencyAmplitudes,
                                                     DemoItems[index].RumbleWaveFormAsset.WaveForm.HighFrequencyAmplitudes, -1,
                                                     HapticTypes.LightImpact, this, -1, false); 
                DemoItems[index].AssociatedSound.Play();
                StartCoroutine(ChangeIcon(DemoItems[index].AssociatedSprite));
            }
            else
            {
                MMVibrationManager.AdvancedHapticPattern(DemoItems[index].AHAPFile.text,
                                                     DemoItems[index].WaveFormAsset.WaveForm.Pattern, DemoItems[index].WaveFormAsset.WaveForm.Amplitudes, -1,
                                                     DemoItems[index].RumbleWaveFormAsset.WaveForm.Pattern, DemoItems[index].RumbleWaveFormAsset.WaveForm.LowFrequencyAmplitudes,
                                                     DemoItems[index].RumbleWaveFormAsset.WaveForm.HighFrequencyAmplitudes, -1,
                                                     HapticTypes.LightImpact, this, -1, true); 
                DemoItems[index].AssociatedSound.Play();
                StartCoroutine(ChangeIcon(DemoItems[index].AssociatedSprite));
            }            
        }
        
        // ICON ----------------------------------------------------------------------------------------------

        protected virtual IEnumerator ChangeIcon(Sprite newSprite)
        {
            IconImageAnimator.SetBool(_idleAnimationParameter, false);
            yield return _ahapTurnDelay;
            IconImage.sprite = newSprite;
        }

        // CALLBACKS ----------------------------------------------------------------------------------------------

            public virtual void Test()
        {
            StartCoroutine(BackToIdle());

        }

        protected virtual void OnHapticsStopped()
        {
            StartCoroutine(BackToIdle());
        }

        protected virtual IEnumerator BackToIdle()
        {
            Logo.Shaking = false;
            IconImageAnimator.SetBool(_idleAnimationParameter, true);
            yield return _ahapTurnDelay;
            IconImage.sprite = DemoItems[0].AssociatedSprite;
        }

        protected virtual void OnHapticsError()
        {

        }

        protected virtual void OnHapticsReset()
        {

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
            MMNVAndroid.ClearThreads();
        }
    }
}
