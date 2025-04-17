using System.Collections;
using UnityEngine;

namespace Code.Logic.Zuma.Balls
{
    public class BallRotator : MonoBehaviour
    {
        [SerializeField] private Transform _rotationTransform;
        
        private float _rotationSpeed = 360f;
        
        private Coroutine _rotationCoroutine;

        public void StartRotate()
        {
            _rotationCoroutine ??= StartCoroutine(RotateRoutine());
        }

        public void StopRotate()
        {
            if (_rotationCoroutine != null)
            {
                StopCoroutine(_rotationCoroutine);
                _rotationCoroutine = null;
            }
        }

        private IEnumerator RotateRoutine()
        {
            float currentRotationX = _rotationTransform.localEulerAngles.x;

            while (true)
            {
                currentRotationX += _rotationSpeed * Time.deltaTime;
                _rotationTransform.localEulerAngles = new Vector3(currentRotationX, 0, 0);
                yield return null;
            }
        }
    }
}

