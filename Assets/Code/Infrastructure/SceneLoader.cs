using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Infrastructure
{
    public class SceneLoader : ISceneLoader
    {
        private readonly ICoroutineRunner _coroutineRunner;


        public SceneLoader(ICoroutineRunner coroutineRunner)
        {
            _coroutineRunner = coroutineRunner;
        }

        public void Load(string name, Action onLevelLoad)
        {
            _coroutineRunner.StartCoroutine(LoadLevel(name, onLevelLoad));
        }

        private IEnumerator LoadLevel(string name, Action onLevelLoad)
        {
            AsyncOperation waitNextScene = SceneManager.LoadSceneAsync(name);
            while (!waitNextScene.isDone)
                yield return null;

            onLevelLoad?.Invoke();
        }
    }
}