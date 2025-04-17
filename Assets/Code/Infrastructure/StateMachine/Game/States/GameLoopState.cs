using Code.Services.BallController;
using Code.Services.Input;
using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.Services.Providers.Balls;
using Code.Services.Providers.Widgets;
using Code.Services.Timer;

namespace Code.Infrastructure.StateMachine.Game.States
{
    public class GameLoopState : IState, IGameState, IUpdatable
    {
        private readonly IStateMachine<IGameState> _gameStateMachine;
        private readonly IInputService _inputService;
        private readonly IBallProvider _ballProvider;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ILevelService _levelService;
        private readonly IBallChainController _ballChainController;
        private readonly ILevelLocalProgressService _levelLocalProgressService;
        private readonly ITimeService _timeService;

        public GameLoopState(
            IStateMachine<IGameState> gameStateMachine, 
            IInputService inputService,
            IBallProvider ballProvider,
            IWidgetProvider widgetProvider,
            ILevelService levelService,
            IBallChainController ballChainController,
            ILevelLocalProgressService levelLocalProgressService,
            ITimeService timeService)
        {
            _gameStateMachine = gameStateMachine;
            _inputService = inputService;
            _ballProvider = ballProvider;
            _widgetProvider = widgetProvider;
            _levelService = levelService;
            _ballChainController = ballChainController;
            _levelLocalProgressService = levelLocalProgressService;
            _timeService = timeService;
        }
        
        public void Enter()
        {
            
        }

        public void Update()
        {
            if(_ballChainController != null)
                _ballChainController.Update();
        }

        public void Exit()
        {
            _ballChainController.StopBallSpawning();
            
            _inputService.Cleanup();
            _ballProvider.CleanupPool();
            _widgetProvider.CleanupPool();
            _levelService.Cleanup();
            _levelLocalProgressService.Cleanup();
            
            _timeService.ResetTimer();
        }
    }
}