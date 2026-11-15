using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Code.Logic.Zuma.Balls
{
    public class BallRotator : MonoBehaviour
    {
        [SerializeField] private Transform _rotationTransform;
        [SerializeField, Min(0f)] private float _chainSpinDegreesPerUnit = 90f;
        [SerializeField, Min(0f)] private float _projectileSpinDegreesPerUnit = 37.5f;

        private const float ReloadSpinDegreesPerSecond = 360f;

        private CancellationTokenSource _cts;
        private Quaternion _restRotation;

        private void Awake()
        {
            _restRotation = _rotationTransform.localRotation;
        }

        public void StartRotate()
        {
            if (_cts != null) 
                return;

            _cts = new CancellationTokenSource();
            RotateAsync(_cts.Token).Forget();
        }

        public void StopRotate()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            ResetRotation();
        }

        public void ResetRotation() => _rotationTransform.localRotation = _restRotation;

        public void SpinOnChain(Vector3 displacement) => SpinByDistance(displacement, _chainSpinDegreesPerUnit);

        public void SpinInFlight(Vector3 displacement) => SpinByDistance(displacement, _projectileSpinDegreesPerUnit);

        private void SpinByDistance(Vector3 displacement, float degreesPerUnit)
        {
            displacement.y = 0f;
            float distanceSquared = displacement.sqrMagnitude;
            if (distanceSquared <= 0f || degreesPerUnit <= 0f)
                return;

            float angle = Mathf.Sqrt(distanceSquared) * degreesPerUnit;
            Spin(angle);
        }

        private void Spin(float angle) => _rotationTransform.Rotate(0f, 0f, angle, Space.Self);

        private async UniTaskVoid RotateAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Spin(ReloadSpinDegreesPerSecond * Time.deltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }
}