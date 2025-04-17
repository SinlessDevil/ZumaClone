using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Code.Infrastructure
{
    public class GameRunner : MonoBehaviour
    {
        [SerializeField, NotNull] private SceneContext _defaultSceneContext;

        private void Awake()
        {
            if (!FindObjectOfType<ProjectContext>())
                Instantiate(_defaultSceneContext);
            
            Destroy(gameObject);
        }
    }
}