using Code.Services.Factories.UIFactory;
using Code.Services.Levels;

namespace Code.Infrastructure.StateMachine.Game.States
{
    public class LoadMenuState : IPayloadedState<string>, IGameState
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly ILoadingCurtain _loadingCurtain;
        private readonly IUIFactory _uiFactory;
        private readonly ILevelService _levelService;
        private readonly IStateMachine<IGameState> _gameStateMachine;
        
        public LoadMenuState(
            IStateMachine<IGameState> gameStateMachine,
            ISceneLoader sceneLoader,
            ILoadingCurtain loadingCurtain,
            IUIFactory uiFactory,
            ILevelService levelService)
        {
            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
            _loadingCurtain = loadingCurtain;
            _uiFactory = uiFactory;
            _levelService = levelService;
        }
        
        public void Enter(string payload)
        {
            _levelService.ReturnToAdventure();
            _loadingCurtain.Show();
            _loadingCurtain.Hide();
            
            _sceneLoader.Load(payload, InitMenuWorld);
        }

        public void Exit()
        {
            
        }

        private void InitMenuWorld()
        {
            _uiFactory.CreateUiRoot();

            var menuHud = _uiFactory.CreateMenuHud();
            menuHud.Initialize();
        }
    }
}