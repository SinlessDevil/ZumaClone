using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.Logic;
using Code.Logic.Zuma;
using Code.Logic.Zuma.Balls;
using Code.Services.Factories.Game;
using Code.Services.Finish;
using Code.Services.Input;
using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.Services.Providers.Balls;
using Code.Services.Providers.Widgets;
using Code.Services.Random;
using Code.Services.Timer;
using Cysharp.Threading.Tasks;
using PathCreation;
using UnityEngine;

namespace Code.Services.BallController
{
    public class BallChainController : IBallChainController
    {
        private bool _isBoosting = true;
        
        private List<Color> _colorItems = new();
        private int _countItems = 0;

        private CancellationTokenSource _startBallSpawning;
        private PathCreator _pathCreator;
        private BallChainDTO _ballChainDto;

        private ChainTracker _chainTracker;
        private ParticleChainHandler _particleChainHandler;
        private WidgetBallChainProvider _widgetBallChainProvider;
        private MouthChainHandler _mouthChainHandler;
        private WinBallChainHandler _winBallChainHandler;
        private LoseBallChainHandler _loseBallChainHandler;
        private AttachingBallChainHandler _attachingBallChainHandler;
        
        private readonly IBallProvider _ballProvider;
        private readonly IWidgetProvider _widgetProvider;
        private readonly IRandomService _randomService;
        private readonly ILevelService _levelService;
        private readonly ILevelLocalProgressService _levelLocalProgressService;
        private readonly IFinishService _finishService;
        private readonly IInputService _inputService;
        private readonly IGameFactory _gameFactory;
        private readonly ITimeService _timeService;

        public BallChainController(
            IBallProvider ballProvider,
            IWidgetProvider widgetProvider,
            IRandomService randomService,
            ILevelService levelService,
            ILevelLocalProgressService levelLocalProgressService,
            IFinishService finishService,
            IInputService inputService,
            IGameFactory gameFactory,
            ITimeService timeService)
        {
            _ballProvider = ballProvider;
            _widgetProvider = widgetProvider;
            _randomService = randomService;
            _levelService = levelService;
            _levelLocalProgressService = levelLocalProgressService;
            _finishService = finishService;
            _inputService = inputService;
            _gameFactory = gameFactory;
            _timeService = timeService;
        }
        
        public List<Item> ActiveItems => _chainTracker.Balls.Cast<Item>().ToList();

        public void Initialize(PathCreator pathCreator, BallChainDTO ballChainDto)
        {
            _pathCreator = pathCreator;
            _ballChainDto = ballChainDto;
            
            _chainTracker = new ChainTracker();
            
            _particleChainHandler = new ParticleChainHandler(_ballChainDto, _pathCreator, _chainTracker);
            _widgetBallChainProvider = new WidgetBallChainProvider(_widgetProvider, _pathCreator);
            _mouthChainHandler = new MouthChainHandler(_particleChainHandler, _chainTracker, _levelService);
            _winBallChainHandler = new WinBallChainHandler(_ballChainDto, _pathCreator, _particleChainHandler,
                _widgetBallChainProvider, _chainTracker, _inputService, _timeService, _finishService, 
                _levelLocalProgressService, _levelService);
            _loseBallChainHandler = new LoseBallChainHandler(_ballChainDto, _pathCreator,_chainTracker, _timeService, 
                _levelService, _inputService, _gameFactory, _finishService);
            _attachingBallChainHandler = new AttachingBallChainHandler(_ballChainDto, _chainTracker, 
                _widgetBallChainProvider, _winBallChainHandler, _levelService, _levelLocalProgressService);
        }
        
        public void Update()
        {
            MoveBalls();
        }
        
        public void StartBallSpawning()
        {
            if (_pathCreator == null)
                return;
            
            _startBallSpawning?.Cancel();
            _startBallSpawning = new CancellationTokenSource();

            _colorItems = _randomService.GetColorsByLevelRandomConfig();
            _countItems = _levelService.GetCurrentLevelStaticData().LevelConfig.CountItem;

            BoostSpeedAsync(_startBallSpawning.Token).Forget();
            SpawnInitialBallsAsync(_startBallSpawning.Token).Forget();
            
            _timeService.StartTimer();
        }
        
        public void StopBallSpawning()
        {
            _startBallSpawning?.Cancel();
            _pathCreator = null;
            
            _chainTracker.ClearBalls();
            _colorItems.Clear();
            
            _countItems = 0;
            _chainTracker.ResetDistanceTravelled();
            
            _isBoosting = true;
        }

        public void TryAttachBall(Ball newBall)
        {
            _attachingBallChainHandler.TryAttachBall(newBall);
        }

        public async UniTask MoveParticleAlongPathAsync(ParticleSystemHolder particle)
        {
            await _particleChainHandler.MoveParticleAlongPathAsync(particle);
        }

        private async UniTaskVoid SpawnInitialBallsAsync(CancellationToken token)
        {
            for (int i = 0; i < _countItems; i++)
            {
                if (token.IsCancellationRequested)
                    return;

                var color = _colorItems.FirstOrDefault();

                float spawnDistance = i * _ballChainDto.SpacingBalls;
                Ball newBall = _ballProvider.GetBall(_pathCreator.path.GetPointAtDistance(spawnDistance), 
                    Quaternion.identity);
                newBall.SetColor(color);

                _colorItems.Remove(color);

                AddBall(newBall);
                newBall.SetIndex(i);
                await UniTask.Delay((int)(_ballChainDto.DurationSpawnBall * 1000), cancellationToken: token);
            }
        }
        
        private async UniTaskVoid BoostSpeedAsync(CancellationToken token)
        {
            float elapsedTime = 0f;
            float startSpeed = _ballChainDto.InitialSpeedMultiplier;
            float endSpeed = _ballChainDto.MoveSpeed;

            _ballChainDto.MoveSpeed = startSpeed;
            
            while (elapsedTime < _ballChainDto.BoostDuration)
            {
                elapsedTime += Time.deltaTime / 2;
                _ballChainDto.MoveSpeed = Mathf.Lerp(startSpeed, endSpeed, elapsedTime);
                await UniTask.Yield(cancellationToken: token);
            }

            _isBoosting = false;
        }
        
        private void MoveBalls()
        {
            if (_chainTracker.Balls.Count == 0)
                return;

            var currentSpeed = GetCurrentSpeed();
            _chainTracker.AddDistanceTravelled(currentSpeed * Time.deltaTime);
            
            MoveFistBall();
            HandleRemoveBallNearEndOfPath(CurrentBalls[0]);
            
            for (int i = 1; i < CurrentBalls.Count; i++)
            {
                MoveBall(CurrentBalls[i], i);

                if (HandleRemoveBallNearEndOfPath(CurrentBalls[i]))
                    i--;
            }

            if (!_loseBallChainHandler.IsLose)
                _mouthChainHandler.TryUpdateMouthProgress((float)_ballChainDto.PercentToDetectionLose / 100);
        }

        private void MoveBall(Ball ball, int index)
        {
            float targetDistance = Mathf.Max(_chainTracker.DistanceTravelled - (index * _ballChainDto.SpacingBalls), 0);
            Vector3 targetPosition = _pathCreator.path.GetPointAtDistance(targetDistance);
            ball.transform.position = Vector3.Lerp(ball.transform.position, targetPosition, Time.deltaTime / _ballChainDto.DurationMovingOffset);
        }
        
        private void MoveFistBall()
        {
            float targetDistance = _chainTracker.DistanceTravelled;
            Vector3 targetPosition = _pathCreator.path.GetPointAtDistance(targetDistance);
            CurrentBalls[0].transform.position = Vector3.Lerp(CurrentBalls[0].transform.position, targetPosition, Time.deltaTime / _ballChainDto.DurationMovingOffset);
        }
        
        private bool HandleRemoveBallNearEndOfPath(Ball ball)
        {
            var currentPosition = ball.transform.position;
            
            if (_pathCreator.path.GetClosestDistanceAlongPath(currentPosition) >= _pathCreator.path.length - 0.5f)
            {
                _loseBallChainHandler.TryLose(ball.transform.position);
                
                _chainTracker.SubtractDistanceTravelled(_ballChainDto.SpacingBalls);
                ball.Deactivate();
                _chainTracker.RemoveBall(ball);
                
                return true;
            }

            return false;
        }

        private float GetCurrentSpeed()
        {
            float currentSpeed = _isBoosting ? _ballChainDto.MoveSpeed * _ballChainDto.InitialSpeedMultiplier : _ballChainDto.MoveSpeed;
            return currentSpeed;
        }

        private void AddBall(Ball ball)
        {
            if (CurrentBalls.Count == 0)
            {
                ball.transform.position = _pathCreator.path.GetPointAtDistance(_chainTracker.DistanceTravelled);
            }
            else
            {
                Vector3 lastBallPosition = CurrentBalls[^1].transform.position;
                ball.transform.position = lastBallPosition;
            }

            _chainTracker.AddBall(ball);
        }

        private List<Ball> CurrentBalls => _chainTracker.Balls;
    }
}