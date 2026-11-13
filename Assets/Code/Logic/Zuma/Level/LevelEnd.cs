using UnityEngine;

namespace Code.Logic.Zuma.Level
{
    public class LevelEnd : MonoBehaviour
    {
        private const string MouthAnimation = "OpenedMouth";
        
        [SerializeField] private Animator _animator;

        public void UpdateMouthProgress(float distanceTravelled, float pathLength, float thresholdDistance)
        {
            if (thresholdDistance <= 0f)
            {
                SetMouthProgressionAnimation(0f);
                return;
            }

            float remainingDistance = pathLength - distanceTravelled;
            float normalizedValue = Mathf.Clamp01(1 - (remainingDistance / thresholdDistance));
            SetMouthProgressionAnimation(normalizedValue);
        }
        
        public void SetMouthProgressionAnimation(float progression)
        {
            _animator.SetFloat(MouthAnimation, progression);
        }
    }
}