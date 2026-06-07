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
using Code.Services.Timer;
using Cysharp.Threading.Tasks;
using Code.PathCreation;
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
        private readonly ILevelService _levelService;
        private readonly ILevelLocalProgressService _levelLocalProgressService;
        private readonly IFinishService _finishService;
        private readonly IInputService _inputService;
        private readonly IGameFactory _gameFactory;
        private readonly ITimeService _timeService;

        public BallChainController(
            IBallProvider ballProvider,
            IWidgetProvider widgetProvider,
            ILevelService levelService,
            ILevelLocalProgressService levelLocalProgressService,
            IFinishService finishService,
            IInputService inputService,
            IGameFactory gameFactory,
            ITimeService timeService)
        {
            _ballProvider = ballProvider;
            _widgetProvider = widgetProvider;
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
            _loseBallChainHandler = new LoseBallChainHandler(_ballChainDto, _pathCreator, _chainTracker, _timeService,
                _levelService, _inputService, _gameFactory, _finishService);
            _attachingBallChainHandler = new AttachingBallChainHandler(_pathCreator, _ballChainDto, _chainTracker,
                _widgetBallChainProvider, _winBallChainHandler, _levelService, _levelLocalProgressService);
        }

        public void Update()
        {
            MoveBalls();
        }

        public void StartBallSpawning(List<Color> colorItems)
        {
            if (_pathCreator == null)
                return;

            _startBallSpawning?.Cancel();
            _startBallSpawning = new CancellationTokenSource();

            _colorItems = colorItems;
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

                Color color = _colorItems.FirstOrDefault();
                Ball newBall = _ballProvider.GetBall(Vector3.zero, Quaternion.identity);
                newBall.SetColor(color);
                _colorItems.Remove(color);

                _chainTracker.AddBall(newBall);
                newBall.SetIndex(i);

                float minDistance = i * _ballChainDto.SpacingBalls;
                if (_chainTracker.DistanceTravelled < minDistance)
                    _chainTracker.SetDistanceTravelled(minDistance);

                float initDist = Mathf.Max(_chainTracker.DistanceTravelled - i * _ballChainDto.SpacingBalls, 0f);
                _chainTracker.SetPathDistance(i, initDist);

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
            if (CurrentBalls.Count == 0)
                return;

            _chainTracker.AddDistanceTravelled(GetCurrentSpeed() * Time.deltaTime);

            var path = _pathCreator.path;

            for (int i = 0; i < CurrentBalls.Count; i++)
            {
                // Logical position: rigid, computed directly — no chain-propagation
                float logicalDist = _chainTracker.DistanceTravelled - i * _ballChainDto.SpacingBalls;

                if (logicalDist >= path.length)
                {
                    _loseBallChainHandler.TryLose(CurrentBalls[i].transform.position);
                    CurrentBalls[i].Deactivate();
                    _chainTracker.RemoveBall(CurrentBalls[i]);
                    i--;
                    continue;
                }

                // Visual position: SmoothDamp toward logical — smooth but arrives cleanly
                float visualDist = _chainTracker.GetPathDistance(i);
                float vel = _chainTracker.GetVelocity(i);
                float gap = Mathf.Abs(logicalDist - visualDist);
                float smoothTime = gap > _ballChainDto.SpacingBalls * 1.5f
                    ? 1f / _ballChainDto.ChainGapSpringStrength
                    : 1f / _ballChainDto.ChainSpringStrength;

                float newVisual = Mathf.SmoothDamp(visualDist, logicalDist, ref vel, smoothTime);
                _chainTracker.SetPathDistance(i, newVisual);
                _chainTracker.SetVelocity(i, vel);

                CurrentBalls[i].transform.position = path.GetPointAtDistance(
                    Mathf.Max(newVisual, 0f), EndOfPathInstruction.Stop);
            }

            if (!_loseBallChainHandler.IsLose)
                _mouthChainHandler.TryUpdateMouthProgress((float)_ballChainDto.PercentToDetectionLose / 100);
        }

        private float GetCurrentSpeed()
        {
            float currentSpeed = _isBoosting
                ? _ballChainDto.MoveSpeed * _ballChainDto.InitialSpeedMultiplier
                : _ballChainDto.MoveSpeed;
            return currentSpeed;
        }

        private List<Ball> CurrentBalls => _chainTracker.Balls;
    }
}