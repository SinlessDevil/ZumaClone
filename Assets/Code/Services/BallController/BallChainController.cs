using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.Logic;
using Code.Logic.Zuma;
using Code.Logic.Zuma.Balls;
using Code.Logic.Zuma.Level;
using Code.PathCreation;
using Code.Services.Factories.Game;
using Code.Services.Finish;
using Code.Services.Input;
using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.Services.Providers.Balls;
using Code.Services.Providers.Widgets;
using Code.Services.Random;
using Code.Services.Timer;
using Code.StaticData.Levels;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Services.BallController
{
    public class BallChainController : IBallChainController
    {
        private const float JunctionSpacingTolerance = 1.1f;

        private sealed class Feed
        {
            public LevelTrack Track;
            public int Spawned;
            public float NextTime;
            public bool Closed;
            public System.Random Random;
            public Color? PreviousColor;
        }

        private readonly List<ChainSegment> _segments = new();
        private readonly List<Feed> _feeds = new();
        private readonly List<Ball> _projectiles = new();
        private readonly Plane[] _projectileViewPlanes = new Plane[6];
        private readonly IBallProvider _ballProvider;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ILevelService _levelService;
        private readonly ILevelLocalProgressService _progress;
        private readonly IFinishService _finishService;
        private readonly IInputService _inputService;
        private readonly IGameFactory _gameFactory;
        private readonly ICoinFactory _coinFactory;
        private readonly ITimeService _timeService;

        private PathCreator _pathCreator;
        private BallChainDTO _settings;
        private ChainState _state;
        private ParticleChainHandler _particles;
        private MouthChainHandler _mouth;
        private WinBallChainHandler _win;
        private LoseBallChainHandler _lose;
        private AttachingBallChainHandler _hits;
        private List<Color> _openingColors;
        private List<(Color color, float weight)> _palette;
        private float _slowUntil;
        private float _reverseUntil;
        private float _accuracyUntil;
        private LevelConfig _config;
        private Camera _projectileCamera;
        private int _projectileViewFrame = -1;
        private WidgetBallChainProvider _widgets;
        private ZumaCoin _coin;
        private float _nextCoinTime;
        private int _coinIndex;

        public List<Item> ActiveItems => _segments.SelectMany(segment => segment.Balls).Cast<Item>().ToList();
        public bool HasAccuracy => _state != null && _state.CanPlay && Time.time < _accuracyUntil;
        public CancellationToken AttemptToken => _state?.Token ?? new CancellationToken(true);

        public BallChainController(
            IBallProvider ballProvider, 
            IWidgetProvider widgetProvider,
            ILevelService levelService, 
            ILevelLocalProgressService levelLocalProgressService,
            IFinishService finishService, 
            IInputService inputService, 
            IGameFactory gameFactory, 
            ITimeService timeService,
            ICoinFactory coinFactory)
        {
            _ballProvider = ballProvider;
            _widgetProvider = widgetProvider;
            _levelService = levelService;
            _progress = levelLocalProgressService;
            _finishService = finishService;
            _inputService = inputService;
            _gameFactory = gameFactory;
            _timeService = timeService;
            _coinFactory = coinFactory;
        }

        public void Initialize(PathCreator pathCreator, BallChainDTO settings)
        {
            StopBallSpawning();
            _pathCreator = pathCreator;
            _settings = settings;
            _state = new ChainState(_segments);
            _particles = new ParticleChainHandler(settings, pathCreator, _state);
            _config = _levelService.GetCurrentLevelStaticData().LevelConfig;
            _slowUntil = _reverseUntil = _accuracyUntil = 0f;
            _widgets = new WidgetBallChainProvider(_widgetProvider, pathCreator);
            _coinIndex = 0;
            _mouth = new MouthChainHandler(_particles, _state, _levelService);
            _win = new WinBallChainHandler(settings, pathCreator, _particles, _widgets, _state, _inputService,
                _timeService, _finishService, _progress, _levelService);
            _lose = new LoseBallChainHandler(_state, _timeService, _levelService,
                _inputService, _finishService);
            _hits = new AttachingBallChainHandler(settings, _segments, _widgets, _levelService, _progress, _state);
            _hits.PowerupTriggered += ApplyPowerup;
            LevelHolder holder = _levelService.GetLevelHolder();
            _feeds.Add(new Feed { Track = new LevelTrack { Path = pathCreator, End = holder.LevelEnd, Tunnels = holder.PrimaryTunnels } });
            if (holder.AdditionalTracks != null)
                foreach (LevelTrack track in holder.AdditionalTracks)
                {
                    if (track?.Path == null || track.End == null || _feeds.Exists(feed => feed.Track.Path == track.Path))
                        throw new System.InvalidOperationException("Each additional route needs its own path and skull.");
                    _feeds.Add(new Feed { Track = track });
                }

            if (_config.CoinInterval > 0f && holder.CoinSpawns.Length > 0)
                _coin = _coinFactory.CreateCoin(holder.transform);
        }

        public void GetActiveColors(List<Color> colors)
        {
            colors.Clear();
            foreach (ChainSegment segment in _segments)
            {
                for (int i = 0; i < segment.Count; i++)
                {
                    Color color = segment.GetBall(i).Color;
                    if (!colors.Contains(color))
                        colors.Add(color);
                }
            }
        }

        public bool IsProjectileOffscreen(Bounds bounds)
        {
            if (_projectileCamera == null || !_projectileCamera.isActiveAndEnabled)
            {
                _projectileCamera = Camera.main;
                _projectileViewFrame = -1;
            }

            if (_projectileCamera == null)
                return false;

            if (_projectileViewFrame != Time.frameCount)
            {
                GeometryUtility.CalculateFrustumPlanes(_projectileCamera, _projectileViewPlanes);
                _projectileViewFrame = Time.frameCount;
            }

            return !GeometryUtility.TestPlanesAABB(_projectileViewPlanes, bounds);
        }

        public void StartBallSpawning(List<Color> colors) =>
            StartBallSpawning(colors, UnityEngine.Random.Range(0, int.MaxValue));

        public void StartBallSpawning(List<Color> colors, int seed)
        {
            if (_pathCreator == null || _state.HasStarted)
                return;

            if (colors.Count == 0 || colors.Any(color => color.a <= 0f))
                throw new System.ArgumentException("A level needs a valid ball palette.", nameof(colors));

            _openingColors = new List<Color>(colors);
            _palette = RandomService.BuildPalette(_config.RandomConfig);
            if (_palette.Count == 0)
                _palette = colors.Distinct().Select(color => (color, 1f)).ToList();
            float startedAt = Time.time;
            System.Random seedSource = new(seed);
            foreach (Feed feed in _feeds)
            {
                feed.NextTime = startedAt;
                feed.Random = new System.Random(seedSource.Next());
            }
            _nextCoinTime = startedAt + _config.CoinInterval;
            _state.Start();
            _timeService.StartTimer();
            TrySpawn();
        }

        public void StopBallSpawning()
        {
            _state?.Cancel();
            if (_hits != null)
                _hits.PowerupTriggered -= ApplyPowerup;
            if (_coin != null)
            {
                _coin.Hide();
                Object.Destroy(_coin.gameObject);
            }
            _coin = null;
            _projectileCamera = null;
            _projectileViewFrame = -1;
            CancelProjectiles();
            _segments.Clear();
            _feeds.Clear();
            _pathCreator = null;
        }

        public void Update()
        {
            if (_state == null || !_state.HasStarted ||
                AttemptToken.IsCancellationRequested || Time.deltaTime <= 0f)
                return;

            if (_state.HasWon)
            {
                AdvanceProjectiles();
                return;
            }

            if (_state.HasLost)
            {
                MoveSegments();
                ConsumeAtSkull();
                AdvanceProjectiles();
                return;
            }

            CloseFeedAtTarget();
            MoveSegments();
            _hits.ResolveSettledShots();
            MergeNeighbors();
            ConsumeAtSkull();
            
            AdvanceProjectiles();
            if (!_state.CanPlay)
                return;

            CloseFeedAtTarget();
            TrySpawn();
            SpawnNextCoin();
            float warning = _settings.PercentToDetectionLose / 100f;
            _mouth.TryUpdateMouthProgress(warning);
            
            foreach (Feed feed in _feeds.Skip(1))
            {
                ChainSegment front = _segments.Find(segment => segment.Track == feed.Track);
                float length = feed.Track.Path.path.length;
                float distance = front?.HeadLogicalDistance ?? 0f;
                feed.Track.End.UpdateMouthProgress(distance, length, length * warning);
            }

            if (!_state.FeedClosed || _state.TotalBallCount != 0) 
                return;
            
            _win.TryWin(0, _state.LastRemovalDistance);
        }

        public void RegisterShot(Ball ball)
        {
            _hits.RegisterShot(ball);
            _projectiles.Add(ball);
        }

        private void AdvanceProjectiles()
        {
            for (int i = 0; i < _projectiles.Count; i++)
                if (_projectiles[i].AdvanceProjectile(Time.deltaTime))
                    _projectiles.RemoveAt(i--);
        }

        private void CancelProjectiles()
        {
            foreach (Ball ball in _projectiles)
                _ballProvider.ReturnBall(ball);
            _projectiles.Clear();
        }

        public void ResolveMiss(Ball ball) => _hits.ResolveShot(ball.ShotId, false, ball.transform.position);

        public void TryAttachBall(Ball newBall)
        {
            if (_state != null && _state.CanPlay && Time.timeScale > 0f)
                _hits.TryAttachBall(newBall, newBall.transform.position, newBall.transform.position, out _, stationaryTargets: true);
        }

        public bool TryAttachBall(Ball newBall, Vector3 start, Vector3 end)
        {
            if (_state == null || !_state.CanPlay || Time.timeScale <= 0f)
                return false;

            bool attached = _hits.TryAttachBall(newBall, start, end, out float contactTime);
            TryCollectCoin(start, end, contactTime);
            return attached;
        }

        public UniTask MoveParticleAlongPathAsync(ParticleSystemHolder particle) =>
            _particles.MoveParticleAlongPathAsync(particle);

        public Vector3 AccuracyEnd(Vector3 start, Vector3 end) =>
            Vector3.Lerp(start, end, _hits.ContactFraction(start, end));

        private void TryCollectCoin(Vector3 start, Vector3 end, float chainContactTime)
        {
            if (_coin == null || !_coin.CanBeHit)
                return;

            Vector3 position = _coin.transform.position;
            if (!AttachingBallChainHandler.TryContact(start - position, end - position,
                    _settings.CollisionThreshold * 0.85f, out float time) || time >= chainContactTime)
                return;

            _coin.Hide();
            _nextCoinTime = Time.time + _config.CoinInterval;
            int score = Mathf.Max(_settings.Survival ? 200 : 500, _settings.ZumaTargetScore / 600 * 100);
            _progress.AddScore(score);
            _widgets.SetUpWidget(position, Color.yellow, $"Coin +{score}");
        }

        private void SpawnNextCoin()
        {
            if (_coin == null || _coin.gameObject.activeSelf || Time.time < _nextCoinTime)
                return;

            Pose[] positions = _levelService.GetLevelHolder().CoinSpawns;
            _coin.Show(positions[_coinIndex]);
            _coinIndex = (_coinIndex + 1) % positions.Length;
        }

        private void ApplyPowerup(BallPowerup powerup, Vector3 position, int shotId, int depth)
        {
            float until = Time.time + _config.PowerupDuration;
            switch (powerup)
            {
                case BallPowerup.Slowdown:
                    _slowUntil = until;
                    break;
                case BallPowerup.Reverse:
                    _reverseUntil = until;
                    break;
                case BallPowerup.Explosion:
                    _hits.Explode(position, _config.ExplosionRadius, shotId, depth);
                    break;
                case BallPowerup.Accuracy:
                    _accuracyUntil = until;
                    break;
            }
        }

        private void CloseFeedAtTarget()
        {
            if (_settings.Survival || 
                _state.FeedClosed || 
                _settings.ZumaTargetScore <= 0 || 
                _progress.Score < _settings.ZumaTargetScore)
                return;

            _state.FeedClosed = true;
            
            if (_segments.Any(segment => segment.HeadLogicalDistance > segment.Path.path.length * 0.5f))
                _state.MeterReverseUntil = Time.time + _settings.MeterReverseDuration;
        }

        private void TrySpawn()
        {
            if (_state.FeedClosed || Time.time < _reverseUntil)
                return;

            foreach (Feed feed in _feeds)
                TrySpawn(feed);
            
            if (_feeds.All(feed => feed.Closed))
                _state.FeedClosed = true;
        }

        private void TrySpawn(Feed feed)
        {
            if (feed.Closed || Time.time < feed.NextTime)
                return;

            ChainSegment tail = _segments.FindLast(segment => segment.Track == feed.Track);
            bool starting = feed.Spawned < _openingColors.Count;
            bool finiteFeed = _settings.ZumaTargetScore == 0 && !_settings.Survival;
            if (tail != null && tail.TailLogicalDistance < _settings.SpacingBalls && !(starting && finiteFeed))
                return;

            if (tail == null)
            {
                tail = new ChainSegment(feed.Track.Path, _settings.SpacingBalls, _settings.MoveSpeed,
                    _settings.ChainSpringStrength, _settings.ChainGapSpringStrength, feed.Track);
                int index = _feeds.IndexOf(feed);
                int nextRoute = _segments.FindIndex(segment => _feeds.FindIndex(other => other.Track == segment.Track) > index);
                _segments.Insert(nextRoute < 0 ? _segments.Count : nextRoute, tail);
            }

            float minimumHead = tail.Count * _settings.SpacingBalls;
            tail.SetHeadLogicalDistance(Mathf.Max(minimumHead, tail.HeadLogicalDistance));
            float distance = tail.HeadLogicalDistance - minimumHead;
            Pose pose = ChainSegment.GetTrackPose(feed.Track.Path.path, distance, Quaternion.identity);
            Ball ball = _ballProvider.GetBall(pose.position, pose.rotation);
            Color color = starting && feed == _feeds[0]
                ? _openingColors[feed.Spawned]
                : RandomService.Sample(_palette, feed.Random, feed.PreviousColor,
                    _config.RandomConfig?.RepeatColorPercent ?? LevelRandomConfigStaticData.DefaultRepeatColorPercent);
            ball.SetColor(color);
            feed.PreviousColor = color;
            _hits.RegisterChainBall(ball);
            ball.SetTrackVisibility(tail.IsExposed(distance));
            if (!finiteFeed && _config.PowerupEvery > 0 && (feed.Spawned + 1) % _config.PowerupEvery == 0)
                ball.SetPowerup((BallPowerup)feed.Random.Next(1, 5));
            ball.SetIndex(tail.Count);
            
            tail.AddBall(ball, distance);
            
            feed.Spawned++;
            feed.NextTime = Time.time + _settings.DurationSpawnBall;
            if (finiteFeed && feed.Spawned == _openingColors.Count)
                feed.Closed = true;
        }

        private void MoveSegments()
        {
            float speed = _settings.MoveSpeed;
            if (Time.time < _slowUntil)
                speed *= 0.45f;
            bool reversing = Time.time < _reverseUntil;
            bool meterReversing = Time.time < _state.MeterReverseUntil;
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                ChainSegment segment = _segments[i];
                bool hasBack = i + 1 < _segments.Count && _segments[i + 1].Track == segment.Track;
                float segmentSpeed = speed;
                if (_state.HasLost)
                    segmentSpeed = _settings.MoveSpeed * _settings.LoseSpeedMultiplier;
                else if (reversing)
                    segmentSpeed = -speed * 2f;
                else if (meterReversing)
                    segmentSpeed = -Mathf.Min(speed * 2f, Mathf.Max(0f, segment.TailLogicalDistance) / Time.deltaTime);
                else if (hasBack)
                    segmentSpeed = segment.GetBall(segment.Count - 1).Color == _segments[i + 1].GetBall(0).Color
                        ? -speed * Mathf.Max(1f, _settings.CatchupSpeedMultiplier - 1f)
                        : 0f;

                segment.Advance(Time.deltaTime, segmentSpeed);
                if (!_state.HasLost && hasBack)
                {
                    float contact = _segments[i + 1].HeadLogicalDistance + segment.Count * _settings.SpacingBalls;
                    segment.SetHeadLogicalDistance(Mathf.Max(contact, segment.HeadLogicalDistance));
                }

                segment.UpdatePositions(Time.deltaTime);
            }
        }

        private void MergeNeighbors()
        {
            int i = 0;
            while (i + 1 < _segments.Count)
            {
                ChainSegment front = _segments[i];
                ChainSegment back = _segments[i + 1];
                float spacing = _settings.SpacingBalls;
                if (front.Track != back.Track || back.HeadLogicalDistance < front.TailLogicalDistance - spacing - 0.0001f ||
                    front.GetVisualDistance(front.Count - 1) - back.GetVisualDistance(0) > spacing * JunctionSpacingTolerance ||
                    front.Balls.Any(ball => ball.IsEnteringChain) || back.Balls.Any(ball => ball.IsEnteringChain))
                {
                    i++;
                    continue;
                }

                int junction = front.Count - 1;
                front.MergeBack(back);
                _segments.RemoveAt(i + 1);
                front.ReIndexBalls();
                _hits.CheckMatchAtJunction(front, junction, back.JunctionShotId, Mathf.Max(1, back.JunctionComboDepth));
                i = Mathf.Max(0, i - 1);
            }
        }

        private void ConsumeAtSkull()
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                ChainSegment segment = _segments[i];
                float end = segment.Path.path.length;
                while (segment.Count > 0 && segment.HeadLogicalDistance >= end)
                {
                    Ball ball = segment.GetBall(0);
                    _lose.TryLose(segment);
                    _state.FeedClosed = true;
                    ball.Deactivate();
                    segment.RemoveBallAt(0);
                    segment.SetHeadLogicalDistance(segment.HeadLogicalDistance - _settings.SpacingBalls);
                }

                if (segment.Count == 0)
                    _segments.RemoveAt(i--);
                else
                    segment.ReIndexBalls();
            }
        }
    }
}
