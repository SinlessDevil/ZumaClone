using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Code.Logic.Zuma.Balls
{
    public class BallRotator : MonoBehaviour
    {
        [SerializeField] private Transform _rotationTransform;

        private float _rotationSpeed = 360f;

        private CancellationTokenSource _cts;

        public void StartRotate()
        {
            if (_cts != null) 
                return;

            _cts = new CancellationTokenSource();
            RotateAsync(_cts.Token).Forget();
        }

        public void StopRotate()
        {
            if (_cts == null) 
                return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private async UniTaskVoid RotateAsync(CancellationToken token)
        {
            float currentRotationX = _rotationTransform.localEulerAngles.x;

            while (!token.IsCancellationRequested)
            {
                currentRotationX += _rotationSpeed * Time.deltaTime;
                _rotationTransform.localEulerAngles = new Vector3(currentRotationX, 0, 0);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }
}