using System.Collections;
using Code.Extensions;
using Code.Logic.Zuma.Balls;
using Code.Services.BallController;
using Code.Services.Factories.UIFactory;
using Code.Services.Input;
using Code.Services.Providers.Balls;
using Code.Services.Random;
using DG.Tweening;
using PathCreation;
using UnityEngine;
using Zenject;

namespace Code.Logic.Zuma.Players
{
    public class PlayerShooting : MonoBehaviour
    {
        private const float SwitchBallDistance = 0.5f;
        
        [SerializeField] private Transform _spawnPointBall;
        [SerializeField] private MeshRenderer _spareBallIndicator;
        
        private float _shootSpeed = 5f;
        private float _ballSpawnDuration = 0.25f;
        
        private float _shootCooldown = 0.5f;
        private float _shootTimer = 0f;
        
        private Ball _spareBall;
        private Ball _currentBall;
        private bool _canShoot = true;

        private Vector3 _savedStartPoint;
        private Vector3 _savedEndPoint;
        
        private Coroutine _reloadCoroutine;
        
        private Transform _rotationTransform;
        private PathCreator _pathCreator;
        private PlayerAnimator _playerAnimator;
        private BezierIntersection _bezierIntersection;
        
        private IBallProvider _ballProvider;
        private IInputService _inputService;
        private IRandomService _randomService;
        private IBallChainController _ballChainController;
        private IUIFactory _uiFactory;

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
            _uiFactory = uiFactory;
        }
        
        public void Initialize(
            Transform rotationTransform, 
            PathCreator pathCreator,
            PlayerAnimator playerAnimator)
        {
            _rotationTransform = rotationTransform;
            _pathCreator = pathCreator;
            _playerAnimator = playerAnimator;
            _bezierIntersection = new BezierIntersection(_pathCreator);
            
            SubscribeEvents();
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            if (!_canShoot)
            {
                _shootTimer -= Time.deltaTime;
                if (_shootTimer <= 0f)
                {
                    _canShoot = true;
                }
            }
        }

        public void Activate()
        {
            SetUpBall(transform);
            PlayAnimationReloading(_currentBall, _spawnPointBall.position);
            SetIndicatorSpareBall();
        }
        
        private void SubscribeEvents()
        {
            _inputService.PointerUpEvent += OnHandleTouch;
        }
        
        private void UnsubscribeEvents()
        {
            _inputService.PointerUpEvent -= OnHandleTouch;
        }

        private void OnHandleTouch()
        {
            if (!_canShoot || _currentBall == null)
                return;

            if(!_uiFactory.GameHud.InputZona.IsActive)
                return;
            
            float distanceToTouch = _inputService.GetWorldDistanceTo(transform.position);

            if (distanceToTouch < SwitchBallDistance) 
            {
                OnSwitchBall();
            }
            else
            {
                OnShoot();
            }
        }
        
        private void OnSwitchBall()
        {
            (_currentBall, _spareBall) = (_spareBall, _currentBall);
            
            PlayAnimationReloading(_currentBall, _spawnPointBall.position);
            PlayAnimationReloading(_spareBall, transform.position);
            SetIndicatorSpareBall();
        }
        
        private void OnShoot()
        {
            if (!_canShoot || _currentBall == null)
                return;

            if(_reloadCoroutine != null)
                return;
            
            //Start cooldown shoot
            _canShoot = false;
            _shootTimer = _shootCooldown;

            //Get direction
            Vector3 mouseWorldPosition = _inputService.TouchPositionToWorldPosition;
            Vector3 direction = mouseWorldPosition - _rotationTransform.position;
            direction = new Vector3(direction.x, 0, direction.z).normalized;
            
            //Get intersection Points
            var startPoint = transform.position;
            var endPoint = startPoint + direction * 100;
            var intersectionPoints = _bezierIntersection.GetIntersectionPoints(startPoint, endPoint);
            _savedStartPoint = startPoint;
            _savedEndPoint = endPoint;
            
            //Push ball
            Ball shotBall = _currentBall;
            shotBall.transform.SetParent(null);
            shotBall.BallMover.StartMoveToDirection(direction, _shootSpeed);
            shotBall.BallRotator.StartRotate();
            shotBall.BallIntersectionTracker.StartTracker(intersectionPoints);
            shotBall.SetInteractive(true);
            
            _reloadCoroutine ??= StartCoroutine(WaitToReloadRoutine());
        }

        private IEnumerator WaitToReloadRoutine()
        {
            _playerAnimator.PlayCloseEyeAnimation();
            yield return new WaitUntil(() => _currentBall.IsInteractive == false);
            yield return new WaitForSeconds(0.3f);
            _playerAnimator.PlayOpenEyeAnimation();
            
            _currentBall = _spareBall;
            PlayAnimationReloading(_currentBall, _spawnPointBall.position);
            SetUpBall(transform);
            SetIndicatorSpareBall();
            
            _reloadCoroutine = null;
        }
        
        private void SetUpBall(Transform parent)
        {
            if (_currentBall == null)
            {
                var currentItemColor = _randomService.GetColorByCurrentItems(_ballChainController.ActiveItems, null);
                _currentBall = _ballProvider.GetBall(parent.position, Quaternion.identity);
                _currentBall.SetColor(currentItemColor);
                _currentBall.transform.SetParent(parent);
            }
            
            _spareBall = null;
            var spareItemColor = _randomService.GetColorByCurrentItems(_ballChainController.ActiveItems, null);
            _spareBall = _ballProvider.GetBall(parent.position, Quaternion.identity);
            _spareBall.SetColor(spareItemColor);
            _spareBall.transform.SetParent(parent);
        }

        private void PlayAnimationReloading(Ball ball, Vector3 position)
        {
            ball.BallRotator.StartRotate();

            ball.transform.DOMove(position, _ballSpawnDuration)
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
            if(_bezierIntersection != null)
                _bezierIntersection.OnDrawIntersectionGizmos();
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_savedStartPoint, _savedEndPoint);
        }
    }
}