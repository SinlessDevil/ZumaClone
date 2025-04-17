using Code.Services.Factories.UIFactory;
using Code.Window;
using UnityEngine;
using Zenject;

namespace Code.Services.Window
{
    public class WindowService : IWindowService
    {
        private IUIFactory _uiFactory;

        [Inject]
        public void Constructor(IUIFactory uiFactory)
        {
            _uiFactory = uiFactory;
        }

        public RectTransform Open(WindowTypeId windowTypeId)
        {
            return _uiFactory.CrateWindow(windowTypeId);
        }
    }
}