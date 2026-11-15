using UnityEngine;

namespace Code.Logic.Zuma.Players
{
    [DefaultExecutionOrder(100)]
    public sealed class PlayerTurnSteps : MonoBehaviour
    {
        [SerializeField] private Transform _body;
        [SerializeField] private Transform _frontLeft;
        [SerializeField] private Transform _frontRight;
        [SerializeField] private Transform _rearLeft;
        [SerializeField] private Transform _rearRight;

        [SerializeField, Range(1f, 45f)] private float _stepAngle = 12f;
        [SerializeField, Range(1f, 45f)] private float _maxPawLag = 22f;
        [SerializeField, Min(0.01f)] private float _stepDuration = 0.14f;
        [SerializeField, Min(0f)] private float _stepHeight = 0.11f;

        private readonly float[] _plantedYaw = new float[2];
        private Paw[] _paws;
        private int _nextPair;
        private int _steppingPair;
        private float _stepElapsed;

        private void Awake()
        {
            _paws = new[]
            {
                new Paw(_frontLeft), new Paw(_rearRight),
                new Paw(_frontRight), new Paw(_rearLeft)
            };
        }

        private void OnEnable()
        {
            float yaw = _body.eulerAngles.y;
            _plantedYaw[0] = yaw;
            _plantedYaw[1] = yaw;
            _nextPair = 0;
            _steppingPair = -1;
        }

        private void OnDisable()
        {
            foreach (Paw paw in _paws)
                paw.Pivot.SetLocalPositionAndRotation(paw.RestPosition, paw.RestRotation);
        }

        private void LateUpdate()
        {
            if (Time.deltaTime <= 0f)
                return;

            float yaw = _body.eulerAngles.y;
            if (_steppingPair == -1)
                TryStartStep(yaw);

            float progress = 0f;
            float lift = 0f;
            if (_steppingPair != -1)
            {
                _stepElapsed += Time.deltaTime;
                progress = Mathf.Clamp01(_stepElapsed / _stepDuration);
                lift = Mathf.Sin(progress * Mathf.PI) * _stepHeight;
            }

            ApplyPairPose(0, yaw, progress, lift);
            ApplyPairPose(1, yaw, progress, lift);

            if (progress >= 1f)
            {
                _plantedYaw[_steppingPair] = yaw;
                _nextPair = 1 - _steppingPair;
                _steppingPair = -1;
            }
        }

        private void TryStartStep(float yaw)
        {
            float threshold = Mathf.Min(_stepAngle, _maxPawLag);
            if (Mathf.Abs(Mathf.DeltaAngle(_plantedYaw[_nextPair], yaw)) >= threshold)
                _steppingPair = _nextPair;
            else if (Mathf.Abs(Mathf.DeltaAngle(_plantedYaw[1 - _nextPair], yaw)) >= threshold)
                _steppingPair = 1 - _nextPair;

            _stepElapsed = 0f;
        }

        private void ApplyPairPose(int pair, float yaw, float progress, float lift)
        {
            float offset = Mathf.Clamp(Mathf.DeltaAngle(yaw, _plantedYaw[pair]), -_maxPawLag, _maxPawLag);
            // Limit how far a snap turn can pull a planted paw away from the body.
            _plantedYaw[pair] = yaw + offset;
            bool stepping = pair == _steppingPair;
            float angle = stepping ? Mathf.SmoothStep(offset, 0f, progress) : offset;
            Quaternion correction = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 stepOffset = Vector3.up * (stepping ? lift : 0f);

            ApplyPawPose(_paws[pair * 2], correction, stepOffset);
            ApplyPawPose(_paws[pair * 2 + 1], correction, stepOffset);
        }

        private void ApplyPawPose(Paw paw, Quaternion correction, Vector3 lift)
        {
            Vector3 origin = _body.position;
            Transform parent = paw.Pivot.parent;
            Vector3 restPosition = parent.TransformPoint(paw.RestPosition);
            Quaternion restRotation = parent.rotation * paw.RestRotation;
            paw.Pivot.SetPositionAndRotation(origin + correction * (restPosition - origin) + lift,
                correction * restRotation);
        }

        private readonly struct Paw
        {
            public readonly Transform Pivot;
            public readonly Vector3 RestPosition;
            public readonly Quaternion RestRotation;

            public Paw(Transform pivot)
            {
                Pivot = pivot;
                RestPosition = pivot.localPosition;
                RestRotation = pivot.localRotation;
            }
        }
    }
}
