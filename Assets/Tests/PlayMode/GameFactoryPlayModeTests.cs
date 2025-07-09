using Code.Logic.Zuma.Balls;
using Code.Logic.Zuma.Level;
using Code.Logic.Zuma.Players;
using Code.Services.Factories.Game;
using Code.StaticData.Levels;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace Tests.PlayMode
{
    public class GameFactoryPlayModeTests : ZenjectIntegrationTestFixture
    {
        private IGameFactory _gameFactory;

        [SetUp]
        public void SetUp()
        {
            PreInstall();
            Container.Bind<IGameFactory>().To<GameFactory>().AsSingle();
            PostInstall();

            _gameFactory = Container.Resolve<IGameFactory>();
        }

        [UnityTest]
        public System.Collections.IEnumerator CreatePlayer_ShouldReturnPlayer()
        {
            var player = _gameFactory.CreatePlayer(Vector3.zero, Quaternion.identity);
            yield return null;

            player.Should().NotBeNull();
            player.Should().BeOfType<Player>();
        }

        [UnityTest]
        public System.Collections.IEnumerator CreateBall_ShouldReturnBall()
        {
            var ball = _gameFactory.CreateBall(Vector3.zero, Quaternion.identity);
            yield return null;

            ball.Should().NotBeNull();
            ball.Should().BeOfType<Ball>();
        }

        [UnityTest]
        public System.Collections.IEnumerator CreateLevelHolder_ShouldReturnLevelHolder()
        {
            var dummyLevelHolder = new GameObject("DummyLevelHolder");
            dummyLevelHolder.AddComponent<LevelHolder>();

            var levelData = ScriptableObject.CreateInstance<LevelStaticData>();
            levelData.LevelHolder = dummyLevelHolder;

            var holder = _gameFactory.CreateLevelHolder(levelData);
            yield return null;

            holder.Should().NotBeNull();
            holder.Should().BeOfType<LevelHolder>();
        }
    }
}