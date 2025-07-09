using System.Collections;
using Code.Services.Factories.UIFactory;
using Code.Services.Providers.Widgets;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace Tests.PlayMode
{
    public class WidgetProviderPlayModeTests : ZenjectIntegrationTestFixture
    {
        private IWidgetProvider _widgetProvider;
        private IUIFactory _uiFactory;

        [SetUp]
        public void SetUp()
        {
            PreInstall();

            Container.Bind<IUIFactory>().To<UIFactory>().AsSingle();
            Container.Bind<IWidgetProvider>().To<WidgetProvider>().AsSingle();

            PostInstall();

            _uiFactory = Container.Resolve<IUIFactory>();
            _widgetProvider = Container.Resolve<IWidgetProvider>();
        }

        [UnityTest]
        public IEnumerator CreatePool_ShouldPreloadWidgets()
        {
            _widgetProvider.CreatePoolWidgets();
            yield return null;
            
            var widget = _widgetProvider.GetWidget(Vector3.zero, Quaternion.identity);
            yield return null;

            widget.Should().NotBeNull();
            widget.gameObject.activeSelf.Should().BeTrue();
        }

        [UnityTest]
        public IEnumerator GetWidget_ShouldActivateAndReuse()
        {
            _widgetProvider.CreatePoolWidgets();
            yield return null;

            var widget = _widgetProvider.GetWidget(Vector3.one, Quaternion.identity);
            yield return null;

            widget.Should().NotBeNull();
            widget.transform.position.Should().Be(Vector3.one);
            widget.gameObject.activeSelf.Should().BeTrue();
        }

        [UnityTest]
        public IEnumerator ReturnWidget_ShouldDeactivate()
        {
            _widgetProvider.CreatePoolWidgets();
            yield return null;

            var widget = _widgetProvider.GetWidget(Vector3.one, Quaternion.identity);
            yield return null;

            _widgetProvider.ReturnWidget(widget);
            yield return null;

            widget.gameObject.activeSelf.Should().BeFalse();
        }

        [UnityTest]
        public IEnumerator PlayAnimation_ShouldDeactivateAfterEnd()
        {
            _widgetProvider.CreatePoolWidgets();
            yield return null;

            var widget = _widgetProvider.GetWidget(Vector3.zero, Quaternion.identity);
            widget.PlayAnimation();

            yield return new WaitForSeconds(1.5f);

            widget.gameObject.activeSelf.Should().BeFalse();
        }

        [UnityTest]
        public IEnumerator SpawnMultipleWidgets_WithRandomPositions()
        {
            _widgetProvider.CreatePoolWidgets();
            yield return null;

            for (int i = 0; i < 15; i++)
            {
                Vector3 randomPos = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
                var widget = _widgetProvider.GetWidget(randomPos, Quaternion.identity);
                widget.SetText($"#{i}");
                widget.SetColor(Color.green);
                widget.PlayAnimation();
            }

            yield return new WaitForSeconds(2f);
            yield return null;
        }
    }
}
