using Code.Services.PersistenceProgress;
using Code.Services.PersistenceProgress.Player;
using Code.Services.SaveLoad;

namespace Code.Infrastructure.StateMachine.Game.States
{
    public class LoadProgressState : IState, IGameState
    {
        private readonly IStateMachine<IGameState> _stateMachine;
        private readonly IPersistenceProgressService _progressService;
        private readonly ISaveLoadFacade _saveLoadFacade;

        public LoadProgressState(
            IStateMachine<IGameState> stateMachine, 
            IPersistenceProgressService progressService, 
            ISaveLoadFacade saveLoadFacade)
        {
            _stateMachine = stateMachine;
            _progressService = progressService;
            _saveLoadFacade = saveLoadFacade;
        }

        public void Enter()
        {
            LoadOrCreatePlayerData();
            
            _stateMachine.Enter<BootstrapAnalyticState>();
        }

        public void Exit()
        {
            
        }

        private PlayerData LoadOrCreatePlayerData()
        {
            var playerData = _progressService.PlayerData =
                _saveLoadFacade.Load(SaveMethodType.PlayerPrefs) != null ? 
                    _saveLoadFacade.Load(SaveMethodType.PlayerPrefs) : 
                    CreatePlayerData();
            return playerData;
        }
        
        private PlayerData CreatePlayerData()
        {
            PlayerData playerData = new PlayerData();

            return playerData;
        }
    }
}