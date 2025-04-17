using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Logic.Zuma.Balls
{
    public class Ball : Item
    {
        [SerializeField] private BallMover _ballMover;
        [SerializeField] private BallRotator _ballRotator;
        [SerializeField] private BallIntersectionTracker _ballIntersectionTracker;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private ParticleSystemHolder _particleSystem;
        [Space(10)] [Header("Debug")] 
        [SerializeField] private Text _text;

        private int _index;
        private bool _isInteractive;
        
        private void OnValidate()
        {
            if (_ballMover == null)
                _ballMover = GetComponent<BallMover>();

            if (_ballRotator == null)
                _ballRotator = GetComponent<BallRotator>();

            if (BallIntersectionTracker == null)
                _ballIntersectionTracker = GetComponent<BallIntersectionTracker>();
        }

        public BallRotator BallRotator => _ballRotator;
        public BallMover BallMover => _ballMover;
        public BallIntersectionTracker BallIntersectionTracker => _ballIntersectionTracker;

        public int Index => _index;
        public bool IsInteractive => _isInteractive;
        
        public void Initialize()
        {
            _ballIntersectionTracker.Initialize(this);
        }

        public void Dispose()
        {
            _ballMover.StopMove();
            _ballRotator.StopRotate();
            _ballIntersectionTracker.StopTracker();
        }

        public void Activate(Vector3 position, Quaternion rotation)
        {
            transform.localScale = Vector3.one;
            transform.SetPositionAndRotation(position, rotation);

            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            _text.text = "";
            
            Dispose();

            gameObject.SetActive(false);
        }

        public void SetInteractive(bool isInteractive)
        {
            _isInteractive = isInteractive;
        }
        
        public void SetIndex(int Index)
        {
            _text.text = Index.ToString();
            _index = Index;
        }

        public override void SetColor(Color color)
        {
            _meshRenderer.material.color = color;
            _color = color;
            _particleSystem.SetColor(_color);
        }

        public void PlayDestroyAnimation(System.Action onComplete)
        {
            _particleSystem.Play();

            transform.localScale = Vector3.one;
            transform.DOScale(Vector3.zero, 0.5f)
                .SetEase(Ease.InCubic)
                .OnComplete(() => { onComplete?.Invoke(); });
        }
    }
}