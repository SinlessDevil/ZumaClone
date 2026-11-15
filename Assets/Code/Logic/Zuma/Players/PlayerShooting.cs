using System.Collections.Generic;
using Code.Extensions;
using Code.Logic.Zuma.Balls;
using Code.Services.BallController;
using Code.Services.Factories.UIFactory;
using Code.Services.Input;
using Code.Services.Providers.Balls;
using Code.Services.Random;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Code.Logic.Zuma.Players
{
    [DefaultExecutionOrder(50)]
    public class PlayerShooting : MonoBehaviour
    {
        private const float SwitchBallDistance = 0.5f;
        private const int SpareBallMaterialIndex = 1;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Transform _spawnPointBall;
        [SerializeField] private MeshRenderer _spareBallIndicator;
        [SerializeField] private LineRenderer _accuracyBeam;

        private const float ShootSpeed = 9.6f;
        private const float AccuracyShootSpeedMultiplier = 1.5f;
        private const float BallSpawnDuration = 0.12f;
        private const float ShootCooldown = 0.18f;
        private float _shootTimer = 0f;

        private Ball _spareBall;
        private Ball _currentBall;
        private List<Color> _fallbackColors;
        private readonly List<Color> _activeColors = new();

        private Vector3 _savedStartPoint;
        private Vector3 _savedEndPoint;

        private Transform _rotationTransform;
        private MaterialPropertyBlock _spareBallProperties;

        private IBallProvider _ballProvider;
        private IInputService _inputService;
        private IRandomService _randomService;
        private IBallChainController _ballChainController;

        [Inject]
        public void Constructor(
            IInputService inputService,
            IBallProvider ballProvider,
            IRandomService randomService,
            IBallChainController ballChainController,
            IUIFactory uiFactory)
        {
            _inputService = inputService;
            _ballProvider = ballProvider;
            _randomService = randomService;
            _ballChainController = ballChainController;
        }

        public void Initialize(Transform rotationTransform)
        {
            _rotationTransform = rotationTransform;
            _spareBallProperties = new MaterialPropertyBlock();

            SubscribeEvents();
        }

        public void Dispose()
        {
            UnsubscribeEvents();
            _accuracyBeam.enabled = false;
        }

        private void Update()
        {
            _shootTimer = Mathf.Max(0f, _shootTimer - Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (_currentBall != null && _spareBall != null)
                RefreshAmmoColors();

            _accuracyBeam.enabled = _currentBall != null && _ballChainController.HasAccuracy;
            if (!_accuracyBeam.enabled)
                return;

            Vector3 start = _spawnPointBall.position;
            Vector3 end = _ballChainController.AccuracyEnd(start, start + _rotationTransform.forward * 30f);
            _accuracyBeam.SetPosition(0, start);
            _accuracyBeam.SetPosition(1, end);
        }

        public void Activate(List<Color> colors)
        {
            _fallbackColors = colors;
            SetUpFirstBalls(colors);
            PlayAnimationReloading(_currentBall, _spawnPointBall.localPosition);
            SetIndicatorSpareBall();
        }

        private void SubscribeEvents()
        {
            _inputService.TapEvent += OnHandleTap;
            _inputService.AlternatePointerEvent += OnAlternatePointer;
        }

        private void UnsubscribeEvents()
        {
            _inputService.TapEvent -= OnHandleTap;
            _inputService.AlternatePointerEvent -= OnAlternatePointer;
        }

        private void OnHandleTap(Vector2 position)
        {
            if (_currentBall == null)
                return;

            Vector3 target = InputServiceExtensions.ScreenToField(position, _rotationTransform.position);
            if (!_inputService.UsesMouse && IsAtFrog(target))
                OnSwitchBall();
            else
                OnShoot(target);
        }

        private void OnAlternatePointer(Vector2 position)
        {
            if (_currentBall != null && (_inputService.UsesMouse ||
                IsAtFrog(InputServiceExtensions.ScreenToField(position, _rotationTransform.position))))
                OnSwitchBall();
        }

        private bool IsAtFrog(Vector3 target) =>
            (target - _rotationTransform.position).sqrMagnitude < SwitchBallDistance * SwitchBallDistance;

        private void OnSwitchBall()
        {
            (_currentBall, _spareBall) = (_spareBall, _currentBall);

            PlayAnimationReloading(_currentBall, _spawnPointBall.localPosition);
            PlayAnimationReloading(_spareBall, Vector3.zero);
            SetIndicatorSpareBall();
        }

        private void OnShoot(Vector3 target)
        {
            if (_shootTimer > 0f)
                return;

            bool hasAccuracy = _ballChainController.HasAccuracy;
            _shootTimer = hasAccuracy ? ShootCooldown * 0.5f : ShootCooldown;
            Vector3 direction = target - _rotationTransform.position;
            direction = new Vector3(direction.x, 0, direction.z).normalized;

            if (direction == Vector3.zero)
                direction = _rotationTransform.forward;

            _rotationTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 startPoint = _spawnPointBall.position;
            var endPoint = startPoint + direction * 100;
            _savedStartPoint = startPoint;
            _savedEndPoint = endPoint;

            Ball shotBall = _currentBall;
            shotBall.Dispose();
            shotBall.transform.SetParent(null);
            shotBall.transform.SetPositionAndRotation(startPoint, Ball.RotationForHeading(_rotationTransform.rotation));
            _ballChainController.RegisterShot(shotBall);
            float speed = hasAccuracy ? ShootSpeed * AccuracyShootSpeedMultiplier : ShootSpeed;
            shotBall.BallMover.StartMoveToDirection(direction, speed);
            shotBall.SetInteractive(true);

            _currentBall = _spareBall;
            PlayAnimationReloading(_currentBall, _spawnPointBall.localPosition);
            SetUpSpareBall();
            SetIndicatorSpareBall();

        }

        private void RefreshAmmoColors()
        {
            _ballChainController.GetActiveColors(_activeColors);
            if (_activeColors.Count == 0)
                return;

            if (!_activeColors.Contains(_currentBall.Color))
                _currentBall.SetColor(_randomService.GetColorByCurrentItems(_activeColors, null));

            if (!_activeColors.Contains(_spareBall.Color))
            {
                _spareBall.SetColor(_randomService.GetColorByCurrentItems(_activeColors, null));
                SetIndicatorSpareBall();
            }
        }

        private void SetUpSpareBall()
        {
            _ballChainController.GetActiveColors(_activeColors);
            Color spareColor = _randomService.GetColorByCurrentItems(_activeColors, null);
            if (spareColor.a == 0f)
                spareColor = _randomService.GetColorByCurrentItems(_fallbackColors, null);
            _spareBall = CreateAmmoBall(spareColor);
        }

        private void SetUpFirstBalls(List<Color> colors)
        {
            Color currentColor = _randomService.GetColorByCurrentItems(colors, null);
            _currentBall = CreateAmmoBall(currentColor);

            Color spareColor = _randomService.GetColorByCurrentItems(colors, null);
            _spareBall = CreateAmmoBall(spareColor);
        }
        
        private Ball CreateAmmoBall(Color color)
        {
            Ball ball = _ballProvider.GetBall(Vector3.zero, Ball.RotationForHeading(Quaternion.identity));
            ball.SetColor(color);
            ball.transform.SetParent(_rotationTransform, false);
            return ball;
        }

        private void PlayAnimationReloading(Ball ball, Vector3 localTargetPos)
        {
            ball.transform.DOKill();
            ball.BallRotator.StartRotate();

            ball.transform.DOLocalMove(localTargetPos, BallSpawnDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    ball.BallRotator.StopRotate();
                });
        }

        private void SetIndicatorSpareBall()
        {
            _spareBallIndicator.GetPropertyBlock(_spareBallProperties, SpareBallMaterialIndex);
            _spareBallProperties.SetColor(BaseColorId, _spareBall.Color);
            _spareBallIndicator.SetPropertyBlock(_spareBallProperties, SpareBallMaterialIndex);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_savedStartPoint, _savedEndPoint);
        }
    }
}
