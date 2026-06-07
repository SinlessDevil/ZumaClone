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
        private List<Color> _colorItems = new();
        private int _countItems = 0;

        private CancellationTokenSource _startBallSpawning;
        private PathCreator _pathCreator;
        private BallChainDTO _ballChainDto;

        private readonly List<ChainSegment> _segments = new();
        private ChainState _chainState;

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

        public List<Item> ActiveItems => _segments.SelectMany(s => s.Balls).Cast<Item>().ToList();

        public void Initialize(PathCreator pathCreator, BallChainDTO ballChainDto)
        {
            _pathCreator = pathCreator;
            _ballChainDto = ballChainDto;

            _chainState = new ChainState(_segments);

            _particleChainHandler = new ParticleChainHandler(_ballChainDto, _pathCreator, _chainState);
            _widgetBallChainProvider = new WidgetBallChainProvider(_widgetProvider, _pathCreator);
            _mouthChainHandler = new MouthChainHandler(_particleChainHandler, _chainState, _levelService);
            _winBallChainHandler = new WinBallChainHandler(_ballChainDto, _pathCreator, _particleChainHandler,
                _widgetBallChainProvider, _chainState, _inputService, _timeService, _finishService,
                _levelLocalProgressService, _levelService);
            _loseBallChainHandler = new LoseBallChainHandler(_ballChainDto, _pathCreator, _chainState,
                _timeService, _levelService, _inputService, _gameFactory, _finishService);
            _attachingBallChainHandler = new AttachingBallChainHandler(_pathCreator, _ballChainDto, _segments,
                _widgetBallChainProvider, _winBallChainHandler, _levelService, _levelLocalProgressService);
        }

        public void Update()
        {
            UpdateSegments();
            CheckMerges();
            CheckLoss();
            if (!_loseBallChainHandler.IsLose)
                _mouthChainHandler.TryUpdateMouthProgress((float)_ballChainDto.PercentToDetectionLose / 100);
        }

        public void StartBallSpawning(List<Color> colorItems)
        {
            if (_pathCreator == null) return;

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

            foreach (var seg in _segments)
                seg.Clear();
            _segments.Clear();

            _colorItems.Clear();
            _countItems = 0;
        }

        public void TryAttachBall(Ball newBall)
        {
            _attachingBallChainHandler.TryAttachBall(newBall);
        }

        public async UniTask MoveParticleAlongPathAsync(ParticleSystemHolder particle)
        {
            await _particleChainHandler.MoveParticleAlongPathAsync(particle);
        }

        private void UpdateSegments()
        {
            foreach (var seg in _segments)
                seg.Update(Time.deltaTime);
        }

        private void CheckMerges()
        {
            for (int i = 0; i < _segments.Count - 1; i++)
            {
                var front = _segments[i];
                var back = _segments[i + 1];

                // Back head has reached front tail position
                if (back.HeadLogicalDistance >= front.TailLogicalDistance - _ballChainDto.SpacingBalls)
                {
                    back.IsCatchingUp = false;
                    back.CurrentSpeed = back.BaseSpeed;

                    int junctionIdx = front.Count - 1;
                    front.MergeBack(back);
                    _segments.RemoveAt(i + 1);
                    front.ReIndexBalls();

                    _attachingBallChainHandler.CheckMatchAtJunction(front, junctionIdx);
                    i--;
                }
            }
        }

        private void CheckLoss()
        {
            if (_segments.Count == 0) return;

            var front = _segments[0];
            while (front.Count > 0 && front.HeadLogicalDistance >= _pathCreator.path.length)
            {
                _loseBallChainHandler.TryLose(front.GetBall(0).transform.position);
                front.GetBall(0).Deactivate();
                front.RemoveBallAt(0);
                front.SetHeadLogicalDistance(front.HeadLogicalDistance - _ballChainDto.SpacingBalls);
            }

            if (front.Count == 0)
                _segments.RemoveAt(0);
        }

        private async UniTaskVoid SpawnInitialBallsAsync(CancellationToken token)
        {
            var segment = new ChainSegment(_pathCreator, _ballChainDto.SpacingBalls,
                _ballChainDto.MoveSpeed, _ballChainDto.ChainSpringStrength, _ballChainDto.ChainGapSpringStrength);
            _segments.Add(segment);

            for (int i = 0; i < _countItems; i++)
            {
                if (token.IsCancellationRequested) return;

                Color color = _colorItems.FirstOrDefault();
                Ball newBall = _ballProvider.GetBall(Vector3.zero, Quaternion.identity);
                newBall.SetColor(color);
                _colorItems.Remove(color);

                float minHead = i * _ballChainDto.SpacingBalls;
                if (segment.HeadLogicalDistance < minHead)
                    segment.SetHeadLogicalDistance(minHead);

                float initDist = Mathf.Max(segment.HeadLogicalDistance - i * _ballChainDto.SpacingBalls, 0f);
                segment.AddBall(newBall, initDist);
                newBall.SetIndex(i);

                await UniTask.Delay((int)(_ballChainDto.DurationSpawnBall * 1000), cancellationToken: token);
            }
        }

        private async UniTaskVoid BoostSpeedAsync(CancellationToken token)
        {
            float elapsed = 0f;
            float startSpeed = _ballChainDto.MoveSpeed * _ballChainDto.InitialSpeedMultiplier;
            float endSpeed = _ballChainDto.MoveSpeed;

            while (elapsed < _ballChainDto.BoostDuration)
            {
                elapsed += Time.deltaTime;
                float speed = Mathf.Lerp(startSpeed, endSpeed, elapsed / _ballChainDto.BoostDuration);

                foreach (var seg in _segments)
                {
                    if (!seg.IsCatchingUp)
                    {
                        seg.BaseSpeed = endSpeed;
                        seg.CurrentSpeed = speed;
                    }
                }

                await UniTask.Yield(cancellationToken: token);
            }

            foreach (var seg in _segments)
            {
                if (!seg.IsCatchingUp)
                {
                    seg.BaseSpeed = endSpeed;
                    seg.CurrentSpeed = endSpeed;
                }
            }
        }
    }
}
