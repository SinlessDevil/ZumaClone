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
        private float _powerupExpiresAt;
        private readonly System.Collections.Generic.HashSet<(int, int)> _passedGaps = new();
        private int _bestGap;
        
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

        public Vector3 PreviousChainPosition { get; private set; }
        public int ShotId { get; set; }
        public int ChainId { get; set; }
        public int GapBonus => _bestGap * _passedGaps.Count;

        public void RecordGap(int frontId, int backId, int points)
        {
            if (_passedGaps.Add((frontId, backId)))
                _bestGap = Mathf.Max(_bestGap, points);
        }
        public BallPowerup Powerup { get; private set; }

        public void SetPowerup(BallPowerup powerup, float lifetime = 0f)
        {
            Powerup = powerup;
            _powerupExpiresAt = Time.time + lifetime;
            _text.text = powerup switch
            {
                BallPowerup.Slowdown => "S",
                BallPowerup.Reverse => "R",
                BallPowerup.Explosion => "X",
                BallPowerup.Accuracy => "A",
                _ => ""
            };
            _text.gameObject.SetActive(powerup != BallPowerup.None);
        }

        public void UpdatePowerup()
        {
            if (Powerup != BallPowerup.None && Time.time >= _powerupExpiresAt)
                SetPowerup(BallPowerup.None);
        }
        public int Index => _index;

        public void SetTrackVisibility(bool visible)
        {
            _meshRenderer.enabled = visible;
            _text.enabled = visible;
        }

        public void SetChainPosition(Vector3 position)
        {
            PreviousChainPosition = transform.position;
            transform.position = position;
        }
        public bool IsInteractive => _isInteractive;
        
        public void Initialize()
        {
            _ballIntersectionTracker.Initialize(this);
        }

        public void Dispose()
        {
            transform.DOKill();
            _ballMover.StopMove();
            _ballRotator.StopRotate();
        }

        public void Activate(Vector3 position, Quaternion rotation)
        {
            transform.SetParent(null, false);
            transform.localScale = Vector3.one;
            transform.SetPositionAndRotation(position, rotation);
            PreviousChainPosition = position;
            ShotId = 0;
            ChainId = 0;
            _passedGaps.Clear();
            _bestGap = 0;
            SetTrackVisibility(true);
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            SetPowerup(BallPowerup.None);
            _particleSystem.Stop();

            Dispose();
            SetInteractive(false);

            gameObject.SetActive(false);
        }

        public void SetInteractive(bool isInteractive) => _isInteractive = isInteractive;
        
        public void SetIndex(int Index)
        {
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