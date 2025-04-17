using Code.UI.Menu.ButtonsNavigation;
using Code.UI.Menu.Windows;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.UI.Menu
{
    public class MenuHud : MonoBehaviour
    {
        [SerializeField] private ButtonNavigationHolder _buttonNavigationHolder;
        [SerializeField] private WindowHolder _windowHolder;
        
        public void Initialize()
        {
            InitEventSystem();
            
            _buttonNavigationHolder.Initialize(TypeWindow.Map);
            _windowHolder.Initialize();
        }

        private void InitEventSystem()
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