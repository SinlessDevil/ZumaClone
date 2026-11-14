using System.Collections.Generic;
using Code.Extensions;
using Code.Logic.Zuma.Balls;
using Code.Services.BallController;
using Code.Services.Factories.UIFactory;
using Code.Services.Input;
using Code.Services.Providers.Balls;
using Code.Services.Random;
using DG.Tweening;
using Code.PathCreation;
using UnityEngine;
using Zenject;

namespace Code.Logic.Zuma.Players
{
    public class PlayerShooting : MonoBehaviour
    {
        private const float SwitchBallDistance = 0.5f;

        [SerializeField] private Transform _spawnPointBall;
        [SerializeField] private MeshRenderer _spareBallIndicator;

        private const float ShootSpeed = 9.6f;
        private const float BallSpawnDuration = 0.12f;
        private const float ShootCooldown = 0.18f;
        private float _shootTimer = 0f;

        private Ball _spareBall;
        private Ball _currentBall;
        private List<Color> _fallbackColors;
        private LineRenderer _accuracyBeam;
        private Material _accuracyMaterial;

        private Vector3 _savedStartPoint;
        private Vector3 _savedEndPoint;

        private Transform _rotationTransform;
        private PlayerAnimator _playerAnimator;

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

        public void Initialize(
            Transform rotationTransform,
            PathCreator pathCreator,
            PlayerAnimator playerAnimator)
        {
            _rotationTransform = rotationTransform;
            _playerAnimator = playerAnimator;

            SubscribeEvents();
        }

        public void Dispose()
        {
            UnsubscribeEvents();
            if (_accuracyMaterial != null)
                Destroy(_accuracyMaterial);
        }

        private void Update()
        {
            _shootTimer = Mathf.Max(0f, _shootTimer - Time.deltaTime);
            if (_shootTimer == 0f && _currentBall != null)
                _playerAnimator.PlayOpenEyeAnimation();
        }

        private void LateUpdate()
        {
            if (_currentBall == null || !_ballChainController.HasAccuracy)
            {
                if (_accuracyBeam != null)
                    _accuracyBeam.enabled = false;
                return;
            }

            if (_accuracyBeam == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    return;
                _accuracyMaterial = new Material(shader);
                _accuracyBeam = gameObject.AddComponent<LineRenderer>();
                _accuracyBeam.sharedMaterial = _accuracyMaterial;
                _accuracyBeam.widthMultiplier = 0.035f;
                _accuracyBeam.startColor = _accuracyBeam.endColor = Color.yellow;
            }

            Vector3 start = _spawnPointBall.position;
            Vector3 end = _ballChainController.AccuracyEnd(start, start + _rotationTransform.forward * 30f);
            _accuracyBeam.enabled = true;
            _accuracyBeam.SetPosition(0, start);
            _accuracyBeam.SetPosition(1, end);
        }

        public void Activate(List<Color> colors)
        {
            _fallbackColors = colors;
            SetUpFirstBalls(colors, transform);
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

            _shootTimer = _ballChainController.HasAccuracy ? ShootCooldown * 0.5f : ShootCooldown;
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
            shotBall.transform.position = startPoint;
            _ballChainController.RegisterShot(shotBall);
            shotBall.BallMover.StartMoveToDirection(direction, ShootSpeed);
            shotBall.BallRotator.StartRotate();
            shotBall.SetInteractive(true);

            _playerAnimator.PlayCloseEyeAnimation();

            _currentBall = _spareBall;
            PlayAnimationReloading(_currentBall, _spawnPointBall.localPosition);
            SetUpSpareBall(transform);
            SetIndicatorSpareBall();

        }

        private void SetUpSpareBall(Transform parent)
        {
            Color spareColor = _randomService.GetColorByCurrentItems(_ballChainController.ActiveItems, _currentBall);
            if (spareColor.a == 0f)
                spareColor = _randomService.GetColorByCurrentItems(_fallbackColors, _currentBall.Color);
            _spareBall = _ballProvider.GetBall(Vector3.zero, Quaternion.identity);
            _spareBall.SetColor(spareColor);
            _spareBall.transform.SetParent(parent, false);
        }

        private void SetUpFirstBalls(List<Color> colors, Transform parent)
        {
            Color currentColor = _randomService.GetColorByCurrentItems(colors, null);
            _currentBall = _ballProvider.GetBall(Vector3.zero, Quaternion.identity);
            _currentBall.SetColor(currentColor);
            _currentBall.transform.SetParent(parent, false);

            Color spareColor = _randomService.GetColorByCurrentItems(colors, _currentBall.Color);
            _spareBall = _ballProvider.GetBall(Vector3.zero, Quaternion.identity);
            _spareBall.SetColor(spareColor);
            _spareBall.transform.SetParent(parent, false);
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
            _spareBallIndicator.material.color = _spareBall.Color;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_savedStartPoint, _savedEndPoint);
        }
    }
}
