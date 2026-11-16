using DG.Tweening;
using UnityEngine;

namespace Code.Logic.Zuma.Level
{
    public sealed class ZumaCoin : MonoBehaviour
    {
        private const float DropHeight = 0.9f;
        private const float DropDuration = 0.36f;
        private const float BounceHeight = 0.12f;

        [SerializeField] private Transform _graphics;
        [SerializeField, Min(0.01f)] private float _spinDuration = 5f;

        private Vector3 _restPosition;
        private Vector3 _restScale;
        private Quaternion _restRotation;
        private Sequence _appearance;
        private Tween _spin;

        public bool CanBeHit { get; private set; }

        public void Show(Pose pose)
        {
            transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
            gameObject.SetActive(true);
            _graphics.localPosition = _restPosition + Vector3.up * DropHeight;
            _graphics.localScale = Vector3.zero;

            _appearance = DOTween.Sequence()
                .Append(_graphics.DOLocalMoveY(_restPosition.y, DropDuration).SetEase(Ease.InQuad))
                .Join(_graphics.DOScale(_restScale, DropDuration).SetEase(Ease.OutCubic))
                .AppendCallback(() => CanBeHit = true)
                .Append(_graphics.DOLocalMoveY(_restPosition.y + BounceHeight, 0.12f).SetEase(Ease.OutQuad))
                .Append(_graphics.DOLocalMoveY(_restPosition.y, 0.15f).SetEase(Ease.InQuad))
                .OnKill(() => _appearance = null);

            _spin = _graphics.DOLocalRotate(Vector3.up * 360f, _spinDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .OnKill(() => _spin = null);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Awake()
        {
            _restPosition = _graphics.localPosition;
            _restScale = _graphics.localScale;
            _restRotation = _graphics.localRotation;
        }

        private void OnDisable()
        {
            CanBeHit = false;
            _appearance?.Kill();
            _spin?.Kill();
            _graphics.SetLocalPositionAndRotation(_restPosition, _restRotation);
            _graphics.localScale = _restScale;
        }
    }
}
