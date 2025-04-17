using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Menu.ButtonsNavigation
{
    public class ButtonNavigation : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TypeWindow _typeWindow;

        public event Action<TypeWindow> OpenedWindowEvent;
        
        private void OnValidate()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        public void Initialize()
        {
            UnsubscribeEvents();
            SubscribeEvents();
        }
        
        private void SubscribeEvents()
        {
            _button.onClick.AddListener(OnOpenedWindowButtonClick);
        }
        
        private void UnsubscribeEvents()
        {
            _button.onClick.RemoveListener(OnOpenedWindowButtonClick);
        }

        private void OnOpenedWindowButtonClick()
        {
            OpenedWindowEvent?.Invoke(_typeWindow);
        }
    }
}