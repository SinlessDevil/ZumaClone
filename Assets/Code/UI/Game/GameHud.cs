using System.Collections.Generic;
using Code.Services.StaticData;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Code.UI.Game
{
    public class GameHud : MonoBehaviour
    {
        [Space(10)] [Header("Displayer")]
        [SerializeField] private TypeLevelDisplayer _typeLevelDisplayer;
        [SerializeField] private LevelDisplayer _levelDisplayer;
        [SerializeField] private ScoreDisplayer _scoreDisplayer;
        [SerializeField] private TimeDisplayer _timeDisplayer;
        [Space(10)] [Header("Other")]
        [SerializeField] private InputZona _inputZona;
        [SerializeField] private List<GameObject> _debugObjects;
        
        private IStaticDataService _staticDataService; 
        
        [Inject]
        public void Constructor(IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
        }
        
        public InputZona InputZona => _inputZona;
        
        public void Initialize()
        {
            InitDebugObjects();

            _levelDisplayer.Initialize();
            _typeLevelDisplayer.Initialize();
            _scoreDisplayer.Initialize();
            _timeDisplayer.Initialize();
            
            TrySetUpEventSystem();
        }

        private void OnDestroy()
        {
            _scoreDisplayer.Dispose();
            _timeDisplayer.Dispose();
        }

        private void InitDebugObjects()
        {
            if (_staticDataService.GameConfig.DebugMode)
            {
                foreach (var debugObject in _debugObjects)
                {
                    debugObject.SetActive(true);
                }
            }
        }

        private static void TrySetUpEventSystem()
        {
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var gameObjectEventSystem = new GameObject("EventSystem");
                gameObjectEventSystem.AddComponent<EventSystem>();
                gameObjectEventSystem.AddComponent<StandaloneInputModule>();
            }
        }
    }
}