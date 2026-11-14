using Code.UI.Menu.ButtonsNavigation;
using Code.UI.Menu.Windows;
using UnityEngine;

namespace Code.UI.Menu
{
    public class MenuHud : MonoBehaviour
    {
        [SerializeField] private ButtonNavigationHolder _buttonNavigationHolder;
        [SerializeField] private WindowHolder _windowHolder;
        
        public void Initialize()
        {
            _buttonNavigationHolder.Initialize(TypeWindow.Map);
            _windowHolder.Initialize();
        }
    }
}