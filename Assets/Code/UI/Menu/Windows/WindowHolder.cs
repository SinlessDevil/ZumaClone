using System.Collections.Generic;
using Code.UI.Menu.ButtonsNavigation;
using UnityEngine;

namespace Code.UI.Menu.Windows
{
    public class WindowHolder : MonoBehaviour
    {
        [SerializeField] private List<BaseWindow> _windows = new();
        [SerializeField] private ButtonNavigationHolder _buttonNavigationHolder;
        
        private BaseWindow _currentWindow;
        private StateWindow _stateWindow;

        private void Update()
        {
            if(_stateWindow != null)
                _stateWindow.Update();
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        public void Initialize(TypeWindow typeWindow = TypeWindow.Map)
        {
            HideWindows();

            InitWindows();
            
            _stateWindow = new StateWindow(_windows);
            
            SubscribeEvents();
            OnSwipeWindow(typeWindow);
        }

        private void HideWindows()
        {
            _windows.ForEach(window => window.gameObject.SetActive(false));
        }

        private void InitWindows()
        {
            _windows.ForEach(window => window.Initialize());
        }
        
        private void SubscribeEvents()
        {
            _buttonNavigationHolder.OpenedWindowEvent += OnSwipeWindow;
        }
        
        private void UnsubscribeEvents()
        {
            _buttonNavigationHolder.OpenedWindowEvent -= OnSwipeWindow;
        }

        private void OnSwipeWindow(TypeWindow typeWindow)
        {
            switch (typeWindow)
            {
                case TypeWindow.Shop:
                    _stateWindow.EnterShopWindow();
                    break;
                case TypeWindow.Map:
                    _stateWindow.EnterMapWindow();
                    break;
                case TypeWindow.Meta:
                    _stateWindow.EnterMetaWindow();
                    break;
            }
        }
    }   
}