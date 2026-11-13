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
        private sealed class Feed
        {
            public LevelTrack Track;
            public int Spawned;
            public float NextTime;
            public bool Closed;
            public System.Random Random;
        }

        private readonly List<ChainSegment> _segments = new();
        private readonly List<Feed> _feeds = new();
        private readonly List<Ball> _projectiles = new();
        private readonly IBallProvider _ballProvider;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ILevelService _levelService;
        private readonly ILevelLocalProgressService _progress;
        private readonly IFinishService _finishService;
        private readonly IInputService _inputService;
        private readonly IGameFactory _gameFactory;
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
        private float _startedAt;
        private float _slowUntil;
        private float _reverseUntil;
        private float _accuracyUntil;
        private LevelConfig _config;
        private Bounds _field;
        private WidgetBallChainProvider _widgets;
        private GameObject _coin;
        private Material _coinMaterial;
        private float _coinExpiresAt;
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
            ITimeService timeService)
        {
            _ballProvider = ballProvider;
            _widgetProvider = widgetProvider;
            _levelService = levelService;
            _progress = levelLocalProgressService;
            _finishService = finishService;
            _inputService = inputService;
            _gameFactory = gameFactory;
            _timeService = timeService;
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
                _inputService, _gameFactory, _finishService);
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

            _field = new Bounds(holder.SpawnPositionPlayer.position, Vector3.zero);
            foreach (Feed feed in _feeds)
                for (int i = 0; i < feed.Track.Path.path.NumPoints; i++)
                    _field.Encapsulate(feed.Track.Path.path.GetPoint(i));
            if (holder.CoinPositions != null)
                foreach (Transform position in holder.CoinPositions)
                    if (position != null)
                        _field.Encapsulate(position.position);
            _field.Expand(2f);
        }

        public bool IsOutsideField(Vector3 position)
        {
            Vector3 min = _field.min;
            Vector3 max = _field.max;
            return position.x < min.x || position.x > max.x || position.z < min.z || position.z > max.z;
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
            _startedAt = Time.time;
            System.Random seedSource = new(seed);
            foreach (Feed feed in _feeds)
            {
                feed.NextTime = _startedAt;
                feed.Random = new System.Random(seedSource.Next());
            }
            _nextCoinTime = _startedAt + _config.CoinInterval;
            _state.Start();
            _timeService.StartTimer();
            TrySpawn();
        }

        public void StopBallSpawning()
        {
            _state?.Cancel();
            if (_hits != null)
                _hits.PowerupTriggered -= ApplyPowerup;
            ClearCoin();
            CancelProjectiles();
            _segments.Clear();
            _feeds.Clear();
            _pathCreator = null;
        }

        public void Update()
        {
            if (_state == null || !_state.HasStarted || _state.HasWon ||
                AttemptToken.IsCancellationRequested || Time.deltaTime <= 0f)
                return;

            if (_state.HasLost)
            {
                MoveSegments();
                ConsumeAtSkull();
                return;
            }

            CloseFeedAtTarget();
            MoveSegments();
            MergeNeighbors();
            ConsumeAtSkull();
            
            if (!_state.CanPlay)
            {
                CancelProjectiles();
                return;
            }

            AdvanceProjectiles();

            CloseFeedAtTarget();
            TrySpawn();
            UpdateCoin();
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
            
            ClearCoin();
            CancelProjectiles();
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
                if (_projectiles[i].BallMover.Step(Time.deltaTime))
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
                _hits.TryAttachBall(newBall, newBall.transform.position, newBall.transform.position, stationaryTargets: true);
        }

        public bool TryAttachBall(Ball newBall, Vector3 start, Vector3 end)
        {
            if (_state == null || !_state.CanPlay || Time.timeScale <= 0f)
                return false;

            float coinTime = CoinContact(start, end);
            if (_hits.TryAttachBall(newBall, start, end, coinTime: coinTime))
                return true;

            if (float.IsPositiveInfinity(coinTime))
                return false;

            Vector3 position = _coin.transform.position;
            int score = Mathf.Max(_settings.Survival ? 200 : 500, _settings.ZumaTargetScore / 600 * 100);
            _progress.AddScore(score);
            _widgets.SetUpWidget(position, Color.yellow, $"Coin +{score}");
            _hits.ResolveShot(newBall.ShotId, false, position);
            ClearCoin();
            _ballProvider.ReturnBall(newBall);
            return true;
        }

        public UniTask MoveParticleAlongPathAsync(ParticleSystemHolder particle) =>
            _particles.MoveParticleAlongPathAsync(particle);

        public Vector3 AccuracyEnd(Vector3 start, Vector3 end) =>
            Vector3.Lerp(start, end, Mathf.Min(_hits.ContactFraction(start, end), CoinContact(start, end)));

        private float CoinContact(Vector3 start, Vector3 end)
        {
            if (_coin != null && AttachingBallChainHandler.TryContact(start - _coin.transform.position,
                    end - _coin.transform.position, _settings.CollisionThreshold * 0.85f, out float time))
                return time;

            return float.PositiveInfinity;
        }

        private void UpdateCoin()
        {
            if (_coin != null && Time.time >= _coinExpiresAt)
                ClearCoin();

            Transform[] positions = _levelService.GetLevelHolder().CoinPositions;
            if (_coin != null || positions == null || positions.Length == 0 || _config.CoinInterval <= 0f ||
                _config.CoinLifetime <= 0f || Time.time < _nextCoinTime)
                return;

            Transform marker = positions[_coinIndex++ % positions.Length];
            _nextCoinTime = Time.time + _config.CoinInterval;
            if (marker == null)
                return;

            _coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _coin.name = "Zuma Coin";
            _coin.transform.SetParent(marker, false);
            _coin.transform.localScale = new Vector3(_settings.CollisionThreshold, 0.04f, _settings.CollisionThreshold);
            _coin.GetComponent<Collider>().enabled = false;
            _coinMaterial = _coin.GetComponent<Renderer>().material;
            _coinMaterial.color = Color.yellow;
            _coinExpiresAt = Time.time + _config.CoinLifetime;
        }

        private void ClearCoin()
        {
            if (_coin != null)
                Object.Destroy(_coin);
            if (_coinMaterial != null)
                Object.Destroy(_coinMaterial);
            
            _coin = null;
            _coinMaterial = null;
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
            if (_state.FeedClosed)
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
            Vector3 point = feed.Track.Path.path.GetPointAtDistance(distance, EndOfPathInstruction.Stop);
            Ball ball = _ballProvider.GetBall(point, Quaternion.identity);
            ball.SetColor(starting && feed == _feeds[0] ? _openingColors[feed.Spawned] : RandomService.Sample(_palette, feed.Random));
            _hits.RegisterChainBall(ball);
            ball.SetTrackVisibility(tail.IsExposed(distance));
            if (!finiteFeed && _config.PowerupEvery > 0 && (feed.Spawned + 1) % _config.PowerupEvery == 0)
                ball.SetPowerup((BallPowerup)feed.Random.Next(1, 5), _config.PowerupLifetime);
            ball.SetIndex(tail.Count);
            
            tail.AddBall(ball, distance);
            
            feed.Spawned++;
            feed.NextTime = Time.time + _settings.DurationSpawnBall;
            if (finiteFeed && feed.Spawned == _openingColors.Count)
                feed.Closed = true;
        }

        private void MoveSegments()
        {
            float timeSinceStart = Time.time - _startedAt;
            float boost = _settings.BoostDuration > 0f
                ? Mathf.Lerp(_settings.InitialSpeedMultiplier, 1f, timeSinceStart / _settings.BoostDuration)
                : 1f;
            float survivalBoost = _settings.Survival ? 1f + Mathf.Min(3f, _progress.Score / 1500 * 0.12f) : 1f;
            float speed = _settings.MoveSpeed * boost * survivalBoost;
            if (Time.time < _slowUntil)
                speed *= 0.45f;
            bool reversing = Time.time < Mathf.Max(_state.MeterReverseUntil, _reverseUntil);
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                ChainSegment segment = _segments[i];
                bool hasBack = i + 1 < _segments.Count && _segments[i + 1].Track == segment.Track;
                float segmentSpeed = speed;
                if (_state.HasLost)
                    segmentSpeed = speed + _settings.BoostSpeedBallForLose;
                else if (reversing)
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
                foreach (Ball ball in segment.Balls)
                    ball.UpdatePowerup();
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
                    front.GetVisualDistance(front.Count - 1) - back.GetVisualDistance(0) > spacing * 1.25f)
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
                    ClearCoin();
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
