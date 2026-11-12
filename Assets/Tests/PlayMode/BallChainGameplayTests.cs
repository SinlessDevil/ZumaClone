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
using Code.Services.LocalProgress;
using Code.Services.Providers.Balls;
using Code.Services.Providers.Widgets;
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

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(BallChainGameplayTests));
            LevelStaticData levelAsset = Resources.Load<LevelStaticData>("StaticData/Levels/Level_1");
            _level = Object.Instantiate(levelAsset.LevelHolder, _root.transform).GetComponent<LevelHolder>();
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
            Player player = Object.Instantiate(Resources.Load<Player>("Players/Player"), _root.transform);
            player.GetComponent<PlayerAiming>().Constructor(_input, null);
            player.PlayerShooting.Constructor(_input, balls, null, null, null);
            factory.Player.Returns(player);

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
        }

        [TearDown]
        public void TearDown()
        {
            _chain?.StopBallSpawning();
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
            _timer.Received(1).StartTimer();
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
        public IEnumerator MatchAtJunction_RemovesBothGroupsAndDoublesComboScore()
        {
            yield return Spawn(Color.red, Color.blue, Color.blue, Color.red, Color.red, Color.green);
            Ball[] before = ActiveBalls();
            List<int> scores = new();
            _progress.UpdateScoreEvent += scores.Add;
            Ball shot = InsertBehind(before[1], Color.blue);
            yield return AdvanceUntil(() => _chain.ActiveItems.Count == 1);

            Assert.That(ActiveBalls(), Is.EqualTo(new[] { before[5] }));
            Assert.That(before[5].Index, Is.Zero);
            Assert.That(shot.gameObject.activeSelf, Is.False);
            Assert.That(scores, Is.EqualTo(new[] { 3 * ScorePerBall, 9 * ScorePerBall }));
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
        public IEnumerator BallReachingTheGoal_IsConsumedAndLoses()
        {
            _settings.MoveSpeed = 30f;
            yield return Spawn(Color.blue);
            Ball ball = ActiveBalls()[0];
            yield return AdvanceUntil(() => _finish.ReceivedCalls().Any());

            Assert.That(_chain.ActiveItems, Is.Empty);
            Assert.That(ball.gameObject.activeSelf, Is.False);
            Assert.That(_progress.Score, Is.Zero);
            _finish.Received(1).Lose();
            _finish.DidNotReceive().Win();
            _input.Received(1).Cleanup();
            _timer.Received(1).StopTimer();
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
