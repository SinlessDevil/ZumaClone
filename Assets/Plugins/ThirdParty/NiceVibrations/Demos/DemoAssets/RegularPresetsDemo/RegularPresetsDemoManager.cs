using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoreMountains.NiceVibrations
{
    public class RegularPresetsDemoManager : DemoManager
    {

        [Header("Image")]
        public Image IconImage;
        public Animator IconImageAnimator;

        [Header("Sprites")]
        public Sprite IdleSprite;

        public Sprite SelectionSprite;
        public Sprite SuccessSprite;
        public Sprite WarningSprite;
        public Sprite FailureSprite;
        public Sprite RigidSprite;
        public Sprite SoftSprite;
        public Sprite LightSprite;
        public Sprite MediumSprite;
        public Sprite HeavySprite;

        protected WaitForSeconds _turnDelay;
        protected WaitForSeconds _shakeDelay;
        protected int _idleAnimationParameter;

        protected virtual void Awake()
        {
            _turnDelay = new WaitForSeconds(0.02f);
            _shakeDelay = new WaitForSeconds(0.3f);
            _idleAnimationParameter = Animator.StringToHash("Idle");
            IconImageAnimator.SetBool(_idleAnimationParameter, true);
            IconImageAnimator.speed = 2f;
        }

        protected virtual void ChangeImage(Sprite newSprite)
        {
            StartCoroutine(ChangeImageCoroutine(newSprite));
        }

        protected virtual IEnumerator ChangeImageCoroutine(Sprite newSprite)
        {
            DebugAudioTransient.Play();
            IconImageAnimator.SetBool(_idleAnimationParameter, false);
            yield return _turnDelay;
            IconImage.sprite = newSprite;
            yield return _shakeDelay;
            IconImageAnimator.SetBool(_idleAnimationParameter, true);
            yield return _turnDelay;
            IconImage.sprite = IdleSprite;
        }



        public virtual void SelectionButton()
        {
            MMVibrationManager.Haptic(HapticTypes.Selection, false, true, this);
            ChangeImage(SelectionSprite);
        }

        public virtual void SuccessButton()
        {
            MMVibrationManager.Haptic(HapticTypes.Success, false, true, this);
            ChangeImage(SuccessSprite);
        }

        public virtual void WarningButton()
        {
            MMVibrationManager.Haptic(HapticTypes.Warning, false, true, this);
            ChangeImage(WarningSprite);
        }

        public virtual void FailureButton()
        {
            MMVibrationManager.Haptic(HapticTypes.Failure, false, true, this);
            ChangeImage(FailureSprite);
        }

        public virtual void RigidButton()
        {
            MMVibrationManager.Haptic(HapticTypes.RigidImpact, false, true, this);
            ChangeImage(RigidSprite);
        }

        public virtual void SoftButton()
        {
            MMVibrationManager.Haptic(HapticTypes.SoftImpact, false, true, this);
            ChangeImage(SoftSprite);
        }

        public virtual void LightButton()
        {
            MMVibrationManager.Haptic(HapticTypes.LightImpact, false, true, this);
            ChangeImage(LightSprite);
        }

        public virtual void MediumButton()
        {
            MMVibrationManager.Haptic(HapticTypes.MediumImpact, false, true, this);
            ChangeImage(MediumSprite);
        }

        public virtual void HeavyButton()
        {
            MMVibrationManager.Haptic(HapticTypes.HeavyImpact, false, true, this);
            ChangeImage(HeavySprite);
        }
    }
}