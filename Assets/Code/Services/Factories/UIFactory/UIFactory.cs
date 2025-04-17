using Code.Services.StaticData;
using Code.StaticData;
using Code.UI;
using Code.UI.Game;
using Code.UI.Menu;
using Code.UI.Menu.Windows.Map;
using Code.Window;
using UnityEngine;
using Zenject;

namespace Code.Services.Factories.UIFactory
{
    public class UIFactory : Factory, IUIFactory
    {
        private readonly IInstantiator _instantiator;
        private readonly IStaticDataService _staticData;

        private Transform _uiRoot;

        public UIFactory(
            IInstantiator instantiator,
            IStaticDataService staticDataService) : base(instantiator)
        {
            _instantiator = instantiator;
            _staticData = staticDataService;
        }

        public GameHud GameHud { get; private set; }
        public MenuHud MenuHud { get; private set; }

        public void CreateUiRoot()
        {
            _uiRoot = Instantiate(ResourcePath.UiRootPath).transform;
        }

        public RectTransform CrateWindow(WindowTypeId windowTypeId)
        {
            WindowConfig config = _staticData.ForWindow(windowTypeId);
            GameObject window = Instantiate(config.Prefab, _uiRoot);
            return window.GetComponent<RectTransform>();
        }

        public GameHud CreateGameHud()
        {
            return GameHud = Instantiate(ResourcePath.GameHudPath).GetComponent<GameHud>();
        }

        public MenuHud CreateMenuHud()
        {
            return MenuHud = Instantiate(ResourcePath.MenuHudPath).GetComponent<MenuHud>();
        }
        
        public Widget CreateWidget(Vector3 position, Quaternion rotation)
        {
            var widget = Instantiate(ResourcePath.WidgetPath, position, rotation, null)
                .GetComponent<Widget>();
            return widget;
        }

        public ItemLevel CreateItemLevel(Transform parent)
        {
            GameObject itemLevel = Instantiate(ResourcePath.ItemLevelPath, parent, true);
            return itemLevel.GetComponent<ItemLevel>();
        }

        public StartLevelInfoDisplayer CreateStartLevelInfoDisplayer()
        {
            GameObject window = Instantiate(ResourcePath.StarStartLevelInfoPath, _uiRoot, true);
            StartLevelInfoDisplayer startLevelInfoDisplayer = window.GetComponent<StartLevelInfoDisplayer>();
            return startLevelInfoDisplayer;
        }
    }
}