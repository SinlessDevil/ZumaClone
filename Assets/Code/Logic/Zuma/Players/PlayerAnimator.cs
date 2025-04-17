using DG.Tweening;
using UnityEngine;

namespace Code.Logic.Zuma.Players
{
    public class PlayerAnimator : MonoBehaviour
    {
        private const string IsOpenEye = "IsOpenEye";
        
        [SerializeField] private Animator _animator;
        
        private Tween _rotationTween;

        public void PlayLoopRotation()
        {
            _rotationTween = transform.DORotate(new Vector3(0, 360, 0), 0.5f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental)
                .SetEase(Ease.Linear)
                .SetId("RotationTween");
        }

        public void StopRotation()
        {
            _rotationTween?.Kill();
        }
        
        public void PlayOpenEyeAnimation()
        {
            _animator.SetBool(IsOpenEye, true);
        }
        
        public void PlayCloseEyeAnimation()
        {
            _animator.SetBool(IsOpenEye, false);
        }
    }
}