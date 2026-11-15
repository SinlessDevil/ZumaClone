using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma.Balls;
using Code.Logic.Zuma.Level;
using Code.Logic.Zuma.Players;
using Code.PathCreation;
using Code.Services.BallController;
using Code.Services.Factories.Game;
using Code.Services.Finish;
using Code.Services.Input;
using Code.Services.Levels;
using Code.Services.Factories;
using Code.Services.LocalProgress;
using Code.Services.Providers.Balls;
using Code.Services.Providers.Widgets;
using Code.Services.Random;
using Code.Services.Timer;
using Code.StaticData.Levels;
using Code.UI;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class BallChainGameplayTests
    {
        private const float TimeoutSeconds = 8f;
        private const int ScorePerBall = 10;
        private const int PathBonus = 100;

        private GameObject _root;
        private LevelHolder _level;
        private LevelStaticData _levelData;
        private LevelScoreStaticData _scoreData;
        private BallChainDTO _settings;
        private BallChainController _chain;
        private LevelLocalProgressService _progress;
        private IFinishService _finish;
        private IInputService _input;
        private ITimeService _timer;
        private Player _player;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(BallChainGameplayTests));
            LevelStaticData levelAsset = Resources.Load<LevelStaticData>("StaticData/Levels/Level_1");
            _level = Object.Instantiate(Resources.Load<GameObject>(ResourcePath.LevelPath), _root.transform)
                .GetComponent<LevelHolder>();
            new LevelGeometryService().Apply(_level, levelAsset);
            _level.PathCreator.bezierPath = new BezierPath(new[] { Vector3.zero, Vector3.right * 30f }, false, PathSpace.xz);

            _scoreData = ScriptableObject.CreateInstance<LevelScoreStaticData>();
            _scoreData.ScorePerItem = ScorePerBall;
            _scoreData.ScorePerStepPath = PathBonus;
            _levelData = ScriptableObject.CreateInstance<LevelStaticData>();
            _levelData.LevelConfig.ScoreConfig = _scoreData;

            ILevelService levels = Substitute.For<ILevelService>();
            levels.GetLevelHolder().Returns(_level);
            levels.GetCurrentLevelStaticData().Returns(_levelData);

            IBallProvider balls = Substitute.For<IBallProvider>();
            balls.GetBall(Arg.Any<Vector3>(), Arg.Any<Quaternion>())
                .Returns(call => CreateBall(call.Arg<Vector3>()));

            IWidgetProvider widgets = Substitute.For<IWidgetProvider>();
            Widget widgetPrefab = Resources.Load<Widget>("UI/Widget");
            widgets.GetWidget(Arg.Any<Vector3>()).Returns(call =>
                Object.Instantiate(widgetPrefab, call.Arg<Vector3>(), Quaternion.identity, _root.transform));

            _finish = Substitute.For<IFinishService>();
            _timer = Substitute.For<ITimeService>();
            _input = Substitute.For<IInputService>();
            _progress = new LevelLocalProgressService();

            IGameFactory factory = Substitute.For<IGameFactory>();
            _player = Object.Instantiate(Resources.Load<Player>("Players/Player"), _root.transform);
            _player.GetComponent<PlayerAiming>().Constructor(_input, null);
            factory.Player.Returns(_player);

            _settings = new BallChainDTO
            {
                DurationSpawnBall = 0.01f,
                MoveSpeed = 2f,
                SpacingBalls = 1f,
                DurationMovingOffset = 0.02f,
                ChainSpringStrength = 25f,
                ChainGapSpringStrength = 25f,
                CatchupSpeedMultiplier = 4f,
                CollisionThreshold = 0.4f,
                MatchingCount = 3,
                MinParticleSpeed = 300f,
                MaxParticleSpeed = 300f,
                SetToSpawnWidget = 100f,
                TimeToSpawnWidget = 1
            };

            _chain = new BallChainController(balls, widgets, levels, _progress, _finish, _input, factory, _timer);
            _chain.Initialize(_level.PathCreator, _settings);
            _player.PlayerShooting.Constructor(_input, balls, new RandomService(levels), _chain, null);
        }

        [TearDown]
        public void TearDown()
        {
            _chain?.StopBallSpawning();
            foreach (Ball ammo in _player.GetComponentsInChildren<Ball>())
                ammo.Dispose();
            Object.Destroy(_root);
            Object.Destroy(_levelData);
            Object.Destroy(_scoreData);
        }

        [UnityTest]
        public IEnumerator SpawnedChain_MovesTowardTheGoalWithColorsInOrder()
        {
            yield return Spawn(Color.red, Color.green, Color.blue);
            Ball[] balls = ActiveBalls();
            Vector3 headStart = balls[0].transform.position;
            yield return AdvanceUntil(() => balls[0].transform.position.x > headStart.x + 0.5f);

            Assert.That(balls.Select(ball => ball.Color), Is.EqualTo(new[] { Color.red, Color.green, Color.blue }));
            Assert.That(balls.Select(ball => ball.Index), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(balls[0].transform.position.x, Is.GreaterThan(balls[1].transform.position.x));
            Assert.That(balls[1].transform.position.x, Is.GreaterThan(balls[2].transform.position.x));
            Assert.That(Vector3.Angle(balls[0].transform.forward, Vector3.back), Is.LessThan(0.1f));
            _timer.Received(1).StartTimer();
        }

        [Test]
        public void ChainBall_SpinsAroundZByDistanceAndResetsOnReuse()
        {
            Ball ball = CreateBall(Vector3.zero);
            Transform graphics = ball.transform.Find("Graphics");
            Quaternion rest = graphics.localRotation;
            Quaternion heading = Quaternion.Euler(0f, 135f, 0f);

            ball.MoveAlongChain(new Pose(Vector3.right, heading), 0.1f);
            Quaternion firstSpin = rest * Quaternion.Euler(0f, 0f, 90f);
            Assert.That(Quaternion.Angle(graphics.localRotation, firstSpin), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(ball.transform.rotation, heading), Is.LessThan(0.01f));

            ball.MoveAlongChain(new Pose(Vector3.right, heading), 0.5f);
            Assert.That(Quaternion.Angle(graphics.localRotation, firstSpin), Is.LessThan(0.01f));

            ball.MoveAlongChain(new Pose(Vector3.right * 0.5f, heading), 0.3f);
            Quaternion reverseSpin = rest * Quaternion.Euler(0f, 0f, 135f);
            Assert.That(Quaternion.Angle(graphics.localRotation, reverseSpin), Is.LessThan(0.01f));

            ball.Deactivate();
            ball.Activate(Vector3.left * 8f, Quaternion.identity);
            Assert.That(Quaternion.Angle(graphics.localRotation, rest), Is.LessThan(0.01f));
            Object.Destroy(ball.gameObject);
        }

        [UnityTest]
        public IEnumerator ShotOutsideCollisionRange_LeavesChainUntouched()
        {
            yield return Spawn(Color.red, Color.blue);
            Ball[] before = ActiveBalls();
            Ball shot = CreateBall(before[0].transform.position + Vector3.forward * 3f, Color.red);
            shot.SetInteractive(true);

            _chain.TryAttachBall(shot);

            Assert.That(ActiveBalls(), Is.EqualTo(before));
            Assert.That(shot.IsInteractive, Is.True);
            Assert.That(_progress.Score, Is.Zero);
        }

        [UnityTest]
        public IEnumerator NonContiguousColors_InsertWithoutScoring()
        {
            yield return Spawn(Color.red, Color.blue, Color.red);
            Ball[] before = ActiveBalls();
            Ball shot = InsertBehind(before[0], Color.red);
            yield return AdvanceUntil(() => !shot.IsInteractive);

            Assert.That(ActiveBalls(), Is.EqualTo(new[] { before[0], shot, before[1], before[2] }));
            Assert.That(ActiveBalls().Select(ball => ball.Index), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(_progress.Score, Is.Zero);
            _finish.DidNotReceive().Win();
        }

        [UnityTest]
        public IEnumerator DirectMatch_WaitsForShotToReachItsSlot()
        {
            _settings.DurationMovingOffset = 0.2f;
            yield return Spawn(Color.red, Color.red, Color.green);
            Ball[] before = ActiveBalls();
            Ball shot = CreateBall(before[0].transform.position + new Vector3(-0.2f, 0f, 0.25f), Color.red);
            shot.SetInteractive(true);
            _chain.TryAttachBall(shot);
            float impactZ = shot.transform.position.z;

            Assert.That(ActiveBalls(), Does.Contain(shot));
            Assert.That(shot.IsEnteringChain, Is.True);
            Assert.That(_progress.Score, Is.Zero);

            yield return AdvanceUntil(() => shot.transform.position.z < impactZ || _progress.Score > 0);
            Assert.That(shot.transform.position.z, Is.InRange(0.001f, impactZ));
            Assert.That(_progress.Score, Is.Zero);

            yield return AdvanceUntil(() => _progress.Score > 0);

            Assert.That(shot.IsEnteringChain, Is.False);
            Assert.That(shot.transform.position.z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(_progress.Score, Is.EqualTo(3 * ScorePerBall));
            Assert.That(ActiveBalls(), Is.EqualTo(new[] { before[2] }));
        }

        [UnityTest]
        public IEnumerator MatchAtJunction_RemovesBothGroupsAndAddsComboBonus()
        {
            yield return Spawn(Color.red, Color.blue, Color.blue, Color.red, Color.red, Color.green);
            Ball[] before = ActiveBalls();
            List<int> scores = new();
            float junctionSpacing = float.PositiveInfinity;
            _progress.UpdateScoreEvent += score =>
            {
                scores.Add(score);
                if (scores.Count == 2)
                    junctionSpacing = Vector3.Distance(before[0].transform.position, before[3].transform.position);
            };
            Ball shot = InsertBehind(before[1], Color.blue);
            yield return AdvanceUntil(() => _chain.ActiveItems.Count == 1 && !shot.gameObject.activeSelf);

            Assert.That(ActiveBalls(), Is.EqualTo(new[] { before[5] }));
            Assert.That(before[5].Index, Is.Zero);
            Assert.That(shot.gameObject.activeSelf, Is.False);
            Assert.That(scores, Is.EqualTo(new[] { 3 * ScorePerBall, 6 * ScorePerBall + 100 }));
            Assert.That(junctionSpacing, Is.EqualTo(_settings.SpacingBalls).Within(_settings.SpacingBalls * 0.1f));
            _finish.DidNotReceive().Win();
            _finish.DidNotReceive().Lose();
        }

        [UnityTest]
        public IEnumerator ClearingTheLastGroup_AwardsPathBonusAndWins()
        {
            yield return Spawn(Color.red, Color.red);
            List<int> scores = new();
            _progress.UpdateScoreEvent += scores.Add;
            Ball shot = InsertBehind(ActiveBalls()[0], Color.red);
            yield return AdvanceUntil(() => _chain.ActiveItems.Count == 0 && _finish.ReceivedCalls().Any());

            Assert.That(shot.gameObject.activeSelf, Is.False);
            Assert.That(scores, Is.EqualTo(new[] { 3 * ScorePerBall, 3 * ScorePerBall + PathBonus }));
            _finish.Received(1).Win();
            _finish.DidNotReceive().Lose();
            _input.Received(1).Cleanup();
            _timer.Received(1).StopTimer();
        }

        [UnityTest]
        public IEnumerator BallReachingTheGoal_SpeedsUpTheRemainingChainAndLoses()
        {
            _settings.MoveSpeed = 5f;
            _settings.LoseSpeedMultiplier = 4f;
            yield return Spawn(Color.blue, Color.red, Color.green, Color.blue, Color.red, Color.green, Color.blue, Color.red, Color.green);
            Ball[] balls = ActiveBalls();
            yield return AdvanceUntil(() => !balls[0].gameObject.activeSelf);

            Ball tail = balls[^1];
            Vector3 start = tail.transform.position;
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                _chain.Update();
                elapsed += Time.deltaTime;
                if (elapsed < 0.3f)
                    yield return null;
            }

            float distance = Vector3.Distance(start, tail.transform.position);
            Assert.That(tail.gameObject.activeSelf, Is.True);
            Assert.That(distance / elapsed, Is.EqualTo(20f).Within(2f));
            yield return AdvanceUntil(() => _finish.ReceivedCalls().Any());

            Assert.That(_chain.ActiveItems, Is.Empty);
            Assert.That(tail.gameObject.activeSelf, Is.False);
            Assert.That(_progress.Score, Is.Zero);
            _finish.Received(1).Lose();
            _finish.DidNotReceive().Win();
            _input.Received(1).Cleanup();
            _timer.Received(1).StopTimer();
        }

        [Test]
        public void ChainColorSampling_AtZeroPercentSwitchesUnlessThereIsOnlyOneColor()
        {
            System.Random random = new(11);
            (Color, float)[] palette = { (Color.blue, 25f), (Color.red, 25f) };
            Assert.That(RandomService.Sample(palette, random, Color.blue, 0f), Is.EqualTo(Color.red));
            Assert.That(RandomService.Sample(palette, random, Color.red, 100f), Is.EqualTo(Color.red));
            Assert.That(RandomService.Sample(new[] { (Color.blue, 25f) }, random, Color.blue, 0f),
                Is.EqualTo(Color.blue));
        }

        [UnityTest]
        public IEnumerator ContinuousFeed_CanRepeatLastColorFromOpeningSequence()
        {
            LevelRandomConfigStaticData config = Object.Instantiate(
                Resources.Load<LevelRandomConfigStaticData>("StaticData/Random/LevelRandomConfig_Base"));
            try
            {
                config.RepeatColorPercent = 100f;
                _levelData.LevelConfig.RandomConfig = config;
                _settings.ZumaTargetScore = 1000;
                Color first = config.Items[0].ItemColor;
                Color second = config.Items[1].ItemColor;
                _chain.StartBallSpawning(new List<Color> { first, second }, 27);

                yield return AdvanceUntil(() => _chain.ActiveItems.Count == 4);

                Assert.That(ActiveBalls().Select(ball => ball.Color),
                    Is.EqualTo(new[] { first, second, second, second }));
            }
            finally
            {
                Object.Destroy(config);
            }
        }

        [UnityTest]
        public IEnumerator LastBallOfColorMatched_RefreshesBothLoadedShotsInPlace()
        {
            yield return Spawn(Color.red, Color.red, Color.green);
            _player.Initialize();
            _player.PlayerShooting.Activate(new List<Color> { Color.red });
            Ball[] ammo = _player.GetComponentsInChildren<Ball>();
            Assert.That(ammo.Length, Is.EqualTo(2));
            Assert.That(ammo.Select(ball => ball.Color), Is.All.EqualTo(Color.red));

            InsertBehind(ActiveBalls()[0], Color.red);
            yield return AdvanceUntil(() => ammo.All(ball => ball.Color == Color.green));

            Assert.That(_player.GetComponentsInChildren<Ball>(), Is.EqualTo(ammo));
            Assert.That(ActiveBalls().Select(ball => ball.Color), Is.EqualTo(new[] { Color.green }));
        }

        [UnityTest]
        public IEnumerator AccuracyMatch_ShowsBeamToFirstHitUntilEffectExpires()
        {
            _levelData.LevelConfig.PowerupDuration = 1f;
            _player.Initialize();
            _player.PlayerShooting.Activate(new List<Color> { Color.green, Color.red });
            LineRenderer beam = _player.GetComponent<LineRenderer>();
            Assert.That(beam.enabled, Is.False);

            yield return MatchPowerup(BallPowerup.Accuracy);
            Assert.That(_chain.HasAccuracy, Is.True);
            Ball target = ActiveBalls()[^1];
            _player.transform.SetPositionAndRotation(target.transform.position - Vector3.right * 3f,
                Quaternion.LookRotation(Vector3.right, Vector3.up));
            yield return AdvanceUntil(() => beam.enabled);

            Vector3 muzzle = _player.transform.Find("SpawnPointBall").position;
            Vector3 end = beam.GetPosition(1);
            Assert.That(Vector3.Distance(beam.GetPosition(0), muzzle), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(end, muzzle), Is.GreaterThan(1f));
            Assert.That(Vector3.Distance(end, target.transform.position), Is.LessThan(_settings.CollisionThreshold + 0.15f));
            yield return AdvanceUntil(() => !_chain.HasAccuracy && !beam.enabled);
        }

        [UnityTest]
        public IEnumerator SlowdownMatch_SlowsWithoutStoppingAndRestoresBaseSpeed()
        {
            _levelData.LevelConfig.PowerupDuration = 1.3f;
            yield return MatchPowerup(BallPowerup.Slowdown);
            Ball survivor = ActiveBalls()[0];

            float settleAt = Time.time + 0.15f;
            yield return AdvanceUntil(() => Time.time >= settleAt);
            float started = Time.time;
            float position = survivor.transform.position.x;
            yield return AdvanceUntil(() => Time.time >= started + 0.3f);
            float slowed = (survivor.transform.position.x - position) / (Time.time - started);
            Assert.That(slowed, Is.EqualTo(_settings.MoveSpeed * 0.45f).Within(0.2f));

            float restoredAt = Time.time + 1.4f;
            yield return AdvanceUntil(() => Time.time >= restoredAt);
            started = Time.time;
            position = survivor.transform.position.x;
            yield return AdvanceUntil(() => Time.time >= started + 0.3f);
            float restored = (survivor.transform.position.x - position) / (Time.time - started);
            Assert.That(restored, Is.EqualTo(_settings.MoveSpeed).Within(0.2f));
        }

        [UnityTest]
        public IEnumerator ReverseMatch_RetreatsPastTheEntranceAndResumesForward()
        {
            _levelData.LevelConfig.PowerupDuration = 0.9f;
            yield return MatchPowerup(BallPowerup.Reverse);
            Ball[] remaining = ActiveBalls();
            Ball front = remaining[0];
            Ball rear = remaining[1];
            float start = front.transform.position.x;
            MeshRenderer rearRenderer = rear.transform.Find("Graphics/Sphere").GetComponent<MeshRenderer>();
            Assert.That(rearRenderer.enabled, Is.True);

            yield return AdvanceUntil(() => !rearRenderer.enabled && front.transform.position.x < start - 0.25f);
            Assert.That(_chain.ActiveItems, Does.Contain(rear));
            yield return AdvanceUntil(() => rearRenderer.enabled && rear.transform.position.x > 0.15f);
        }

        [UnityTest]
        public IEnumerator ExplosionMatch_RemovesNearbyDifferentColorAndSparesDistantBalls()
        {
            _levelData.LevelConfig.ExplosionRadius = 1.6f;
            yield return Spawn(Color.blue, Color.green, Color.red, Color.red, Color.yellow);
            Ball[] balls = ActiveBalls();
            balls[2].SetPowerup(BallPowerup.Explosion);
            InsertBehind(balls[2], Color.red);

            yield return AdvanceUntil(() => _progress.Score >= 4 * ScorePerBall);
            Assert.That(ActiveBalls(), Is.EqualTo(new[] { balls[0], balls[4] }));
            Assert.That(_progress.Score, Is.EqualTo(4 * ScorePerBall));
        }

        [UnityTest]
        public IEnumerator UnmatchedPowerup_KeepsPulsingPastSixSecondsAndActivatesOnMatch()
        {
            yield return Spawn(Color.green, Color.green, Color.blue);
            Ball ball = ActiveBalls()[0];
            BallPowerupPulse pulse = ball.GetComponent<BallPowerupPulse>();
            MeshRenderer renderer = ball.transform.Find("Graphics/Sphere").GetComponent<MeshRenderer>();
            MaterialPropertyBlock block = new();
            renderer.GetPropertyBlock(block);
            Color normalEmission = block.GetColor("_EmissionColor");
            Assert.That(pulse.enabled, Is.False);

            ball.SetPowerup(BallPowerup.Accuracy);
            float previousExpiry = Time.time + 6.2f;
            renderer.GetPropertyBlock(block);
            float peakRed = block.GetColor("_EmissionColor").r;
            Assert.That(pulse.enabled, Is.True);
            Assert.That(peakRed, Is.GreaterThan(0.8f));

            yield return new WaitForSeconds(0.25f);
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor("_EmissionColor").r, Is.LessThan(peakRed - 0.2f));
            Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(Color.green));

            yield return AdvanceUntil(() => Time.time >= previousExpiry);
            Assert.That(ball.Powerup, Is.EqualTo(BallPowerup.Accuracy));
            Assert.That(pulse.enabled, Is.True);

            InsertBehind(ball, Color.green);
            yield return AdvanceUntil(() => _chain.HasAccuracy);
            Assert.That(ball.Powerup, Is.EqualTo(BallPowerup.None));
            renderer.GetPropertyBlock(block);
            Assert.That(pulse.enabled, Is.False);
            Assert.That(block.GetColor("_EmissionColor"), Is.EqualTo(normalEmission));
        }

        private IEnumerator MatchPowerup(BallPowerup powerup)
        {
            yield return Spawn(Color.red, Color.red, Color.green, Color.blue);
            Ball target = ActiveBalls()[0];
            target.SetPowerup(powerup);
            InsertBehind(target, Color.red);
            yield return AdvanceUntil(() => _progress.Score >= 3 * ScorePerBall);
            Assert.That(ActiveBalls().Length, Is.EqualTo(2));
        }

        private IEnumerator Spawn(params Color[] colors)
        {
            _levelData.LevelConfig.CountItem = colors.Length;
            _chain.StartBallSpawning(colors.ToList());
            yield return AdvanceUntil(() => _chain.ActiveItems.Count == colors.Length);
            // Give the last ball room to enter the path before placing a shot behind a target.
            yield return AdvanceUntil(() => ActiveBalls()[^1].transform.position.x > 0.25f);
        }

        private Ball[] ActiveBalls() => _chain.ActiveItems.Cast<Ball>().ToArray();

        private Ball InsertBehind(Ball target, Color color)
        {
            Ball shot = CreateBall(target.transform.position - Vector3.right * 0.2f, color);
            shot.SetInteractive(true);
            _chain.TryAttachBall(shot);
            return shot;
        }

        private Ball CreateBall(Vector3 position) => CreateBall(position, Color.white);

        private Ball CreateBall(Vector3 position, Color color)
        {
            Ball ball = Object.Instantiate(Resources.Load<Ball>("Balls/Ball"), position, Quaternion.identity, _root.transform);
            ball.Activate(position, Quaternion.identity);
            ball.SetColor(color);
            return ball;
        }

        private IEnumerator AdvanceUntil(Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                _chain.Update();
                yield return null;
            }

            Assert.That(condition(), Is.True, "The chain did not reach the expected gameplay state.");
        }
    }
}
