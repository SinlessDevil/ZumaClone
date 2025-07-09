using System.Collections;
using Code.Infrastructure.StateMachine;
using Code.Infrastructure.StateMachine.Game.States;
using Code.Services.Providers.Widgets;
using Code.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;

namespace Tests.PlayMode
{
    public class WidgetProviderPlayModeTest
    {
        private IWidgetProvider _provider;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LoadInitialScene();
            yield return new WaitForSeconds(1f);
            
            DiContainer container = ProjectContext.Instance.Container;
            var stateMachine = container.Resolve<IStateMachine<IGameState>>();
            yield return stateMachine.Enter<LoadLevelTestState, string>("Game");
            
            _provider = container.Resolve<IWidgetProvider>();
            Assert.IsNotNull(_provider, "WidgetProvider should not be null");
            yield return null;
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var obj in Object.FindObjectsOfType<GameObject>())
            {
                if (obj.scene.name == null)
                {
                    Object.Destroy(obj);
                }
            }

            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Should_Reuse_Play_Animation_Widget()
        {
            yield return new WaitForSeconds(5f);
            
            Widget first = _provider.GetWidget(Vector3.zero, Quaternion.identity);
            first.SetText("Test");
            first.SetColor(Color.red);
            first.PlayAnimation();
            _provider.ReturnWidget(first);

            yield return new WaitForSeconds(1f);

            Widget reused = _provider.GetWidget(Vector3.right, Quaternion.identity);
            reused.SetText("Test_1");
            reused.SetColor(Color.gray);
            reused.PlayAnimation();

            yield return new WaitForSeconds(1f);

            Assert.IsNotNull(first, "First widget is null. Possibly CreateWidget returned null.");
            Assert.IsNotNull(reused, "Reused widget is null. It might have been destroyed or not created properly.");
            Assert.AreSame(first, reused, "Expected the widget to be reused, but a different instance was returned.");
            yield return null;
        }
        
        private IEnumerator LoadInitialScene()
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Initial");
            yield return new WaitUntil(() => asyncLoad.isDone);
            yield return null;
        }
    }
}