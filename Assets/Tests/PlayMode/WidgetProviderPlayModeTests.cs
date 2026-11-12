using System.Collections;
using Code.Services.Factories.UIFactory;
using Code.Services.Providers.Widgets;
using Code.UI;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class WidgetProviderPlayModeTest
    {
        private GameObject _root;
        private WidgetProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(WidgetProviderPlayModeTest));
            if (Camera.main == null)
            {
                GameObject cameraObject = new("Widget test camera") { tag = "MainCamera" };
                cameraObject.transform.SetParent(_root.transform);
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                cameraObject.AddComponent<Camera>();
            }

            Widget prefab = Resources.Load<Widget>("UI/Widget");
            IUIFactory factory = Substitute.For<IUIFactory>();
            factory.CreateWidget().Returns(_ => Object.Instantiate(prefab, _root.transform));
            _provider = new WidgetProvider(factory);
            _provider.CreatePoolWidgets();
        }

        [TearDown]
        public void TearDown()
        {
            _provider.CleanupPool();
            Object.Destroy(_root);
        }

        [UnityTest]
        public IEnumerator Should_Reuse_Play_Animation_Widget()
        {
            Widget first = _provider.GetWidget(Vector3.zero);
            first.SetText("Test");
            first.SetColor(Color.red);
            first.PlayAnimation();
            yield return WaitForAnimation(first);
            _provider.ReturnWidget(first);

            Widget reused = _provider.GetWidget(Vector3.right);
            Assert.That(reused, Is.SameAs(first));
            reused.SetText("Test_1");
            reused.SetColor(Color.gray);
            reused.PlayAnimation();
            yield return WaitForAnimation(reused);
        }

        private static IEnumerator WaitForAnimation(Widget widget)
        {
            Assert.That(widget.gameObject.activeSelf, Is.True);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (widget.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(widget.gameObject.activeSelf, Is.False, "The widget did not deactivate after the animation.");
        }
    }
}
