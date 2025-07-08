using System.Collections.Generic;
using System.ComponentModel;
using Code.Services.BallController;
using Code.Services.Factories.UIFactory;
using Code.Services.StaticData;
using Code.StaticData;
using Code.StaticData.Levels;
using Code.UI;
using Code.UI.Game;
using Code.UI.Menu;
using Code.Window;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace Tests.PlayMode
{
    public class UIFactoryPlayModeTests : ZenjectIntegrationTestFixture
    {
        private IUIFactory _uiFactory;

        [SetUp]
        public void SetUp()
        {
            PreInstall();

            Container.Bind<IStaticDataService>().FromInstance(new MockStaticDataService());
            Container.Bind<IUIFactory>().To<UIFactory>().AsSingle();

            PostInstall();

            _uiFactory = Container.Resolve<IUIFactory>();
        }

        [UnityTest]
        public System.Collections.IEnumerator CreateGameHud_ShouldReturnHud()
        {
            var hud = _uiFactory.CreateGameHud();
            yield return null;
            hud.Should().NotBeNull();
            hud.Should().BeOfType<GameHud>();
        }

        [UnityTest]
        public System.Collections.IEnumerator CreateMenuHud_ShouldReturnHud()
        {
            var hud = _uiFactory.CreateMenuHud();
            yield return null;
            hud.Should().NotBeNull();
            hud.Should().BeOfType<MenuHud>();
        }

        [UnityTest]
        public System.Collections.IEnumerator CreateWidget_ShouldReturnWidget()
        {
            var widget = _uiFactory.CreateWidget(Vector3.zero, Quaternion.identity);
            yield return null;
            widget.Should().NotBeNull();
            widget.Should().BeOfType<Widget>();
        }

        [UnityTest]
        public System.Collections.IEnumerator CreateStartLevelInfoDisplayer_ShouldWork()
        {
            _uiFactory.CreateUiRoot();
            var displayer = _uiFactory.CreateStartLevelInfoDisplayer();
            yield return null;
            displayer.Should().NotBeNull();
        }

        [UnityTest]
        public System.Collections.IEnumerator CrateWindow_ReturnsRectTransform()
        {
            _uiFactory.CreateUiRoot();
            var rect = _uiFactory.CrateWindow(WindowTypeId.Setting);
            yield return null;
            rect.Should().NotBeNull();
            rect.Should().BeOfType<RectTransform>();
        }
    }

    public class MockStaticDataService : IStaticDataService
    {
        public GameStaticData GameConfig { get; }
        public BalanceStaticData Balance { get; }
        public List<ChapterStaticData> Chapters { get; }
        public BallChainStaticData BallChainConfig { get; }

        public void LoadData()
        {
            throw new System.NotImplementedException();
        }

        public WindowConfig ForWindow(WindowTypeId typeId)
        {
            var go = new GameObject("WindowMock");
            go.AddComponent<RectTransform>();
            return new WindowConfig { Prefab = go };
        }

        public ChapterStaticData ForChapter(int chapterId) => null;
        public BallChainDTO GetBallChainDTO()
        {
            throw new System.NotImplementedException();
        }

        public LevelStaticData ForLevel(int chapterId, int levelId) => null;
    }
}
