using Code.Services.Factories.UIFactory;
using Code.Services.Providers.Widgets;

namespace Code.Infrastructure.StateMachine.Game.States
{
    public class LoadLevelTestState : IPayloadedState<string>, IGameState
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly ILoadingCurtain _loadingCurtain;
        private readonly IUIFactory _uiFactory;
        private readonly IStateMachine<IGameState> _gameStateMachine;
        private readonly IWidgetProvider _widgetProvider;

        public LoadLevelTestState(
            IStateMachine<IGameState> gameStateMachine, 
            ISceneLoader sceneLoader,
            ILoadingCurtain loadingCurtain, 
            IUIFactory uiFactory,
            IWidgetProvider widgetProvider)
        {
            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
            _loadingCurtain = loadingCurtain;
            _uiFactory = uiFactory;
            _widgetProvider = widgetProvider;
        }

        public void Enter(string payload)
        {
            _loadingCurtain.Show();
            _sceneLoader.Load(payload, OnLevelLoad);
        }

        public void Exit()
        {
            _loadingCurtain.Hide();
        }

        protected virtual void OnLevelLoad()
        {
            InitGameWorld();
        }

        private void InitGameWorld()
        {
            _uiFactory.CreateUiRoot();
            
            InitHud();
            
            InitProviders();
        }
        
        private void InitProviders()
        {
            _widgetProvider.CreatePoolWidgets();
        }
        
        private void InitHud()
        {
            var gameHud = _uiFactory.CreateGameHud();
            gameHud.Initialize();
        }
    }
}