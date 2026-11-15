using Code.StaticData;
using DG.Tweening;
using UnityEngine;

namespace Code.Logic.Zuma.Balls
{
    public class Ball : Item
    {
        private const float BaseEmission = 0.65f;
        private static readonly Quaternion HeadingOffset = Quaternion.Euler(0f, 90f, 0f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int NormalMapId = Shader.PropertyToID("_BumpMap");
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private BallMover _ballMover;
        [SerializeField] private BallRotator _ballRotator;
        [SerializeField] private BallIntersectionTracker _ballIntersectionTracker;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private BallPowerupPulse _powerupPulse;
        [SerializeField] private ParticleSystemHolder _particleSystem;

        private BallStaticData _appearance;
        private MaterialPropertyBlock _appearanceProperties;
        private int _index;
        private bool _isInteractive;
        private float _powerupExpiresAt;
        private Vector3 _chainEntryStart;
        private Quaternion _chainEntryRotation;
        private float _chainEntryDuration;
        private float _chainEntryRemaining;
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
        public bool IsEnteringChain => _chainEntryRemaining > 0f;
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
            RefreshAppearance();
            _powerupPulse.SetPulsing(powerup != BallPowerup.None);
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
        }

        public void SetChainPosition(Vector3 position)
        {
            PreviousChainPosition = transform.position;
            transform.position = position;
        }
        public void BeginChainEntry(float duration)
        {
            _chainEntryStart = transform.position;
            _chainEntryRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            _chainEntryDuration = _chainEntryRemaining = duration;
        }

        public void MoveAlongChain(Pose pose, float deltaTime)
        {
            if (IsEnteringChain)
            {
                _chainEntryRemaining = Mathf.Max(0f, _chainEntryRemaining - deltaTime);
                float progress = Mathf.SmoothStep(0f, 1f, 1f - _chainEntryRemaining / _chainEntryDuration);
                pose.position = Vector3.Lerp(_chainEntryStart, pose.position, progress);
                pose.rotation = Quaternion.Slerp(_chainEntryRotation, pose.rotation, progress);
            }

            PreviousChainPosition = transform.position;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            _ballRotator.SpinOnChain(pose.position - PreviousChainPosition);
        }

        public bool IsInteractive => _isInteractive;
        
        public void Initialize(BallStaticData appearance)
        {
            _appearance = appearance;
            _meshRenderer.sharedMaterial = appearance.Material;
            _ballIntersectionTracker.Initialize(this);
        }

        internal static Quaternion RotationForHeading(Quaternion heading) => heading * HeadingOffset;

        public bool AdvanceProjectile(float deltaTime)
        {
            Vector3 start = transform.position;
            if (_ballMover.Step(deltaTime))
                return true;

            _ballRotator.SpinInFlight(transform.position - start);
            return false;
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
            _chainEntryRemaining = 0f;
            ShotId = 0;
            ChainId = 0;
            _passedGaps.Clear();
            _bestGap = 0;
            SetTrackVisibility(true);
            gameObject.SetActive(true);
            _ballRotator.ResetRotation();
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
            base.SetColor(color);
            _particleSystem.SetColor(color);
            RefreshAppearance();
        }

        internal void SetPowerupHighlight(float pulse, float peakEmission)
        {
            if (!_meshRenderer.enabled)
                return;

            Color glow = Color.Lerp(Color, Color.white, pulse) * Mathf.Lerp(BaseEmission, peakEmission, pulse);
            _appearanceProperties.SetColor(EmissionColorId, glow);
            _meshRenderer.SetPropertyBlock(_appearanceProperties);
        }

        private void RefreshAppearance()
        {
            _appearanceProperties ??= new MaterialPropertyBlock();
            _appearanceProperties.SetColor(BaseColorId, Color);
            _appearanceProperties.SetColor(EmissionColorId, Color * BaseEmission);
            if (_appearance != null)
            {
                BallTextureSet textures = _appearance.GetTextures(Color, Powerup);
                _appearanceProperties.SetTexture(BaseMapId, textures.BaseColor);
                _appearanceProperties.SetTexture(NormalMapId, textures.Normal);
                _appearanceProperties.SetTexture(EmissionMapId, textures.RuneMask);
            }

            _meshRenderer.SetPropertyBlock(_appearanceProperties);
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