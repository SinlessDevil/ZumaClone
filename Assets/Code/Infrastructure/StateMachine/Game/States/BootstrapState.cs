using Code.Services.StaticData;
using UnityEngine;

namespace Code.Infrastructure.StateMachine.Game.States
{
    public class BootstrapState : IState, IGameState
    {
        private readonly IStateMachine<IGameState> _stateMachine;
        private readonly ISceneLoader _sceneLoader;
        private readonly IStaticDataService _staticData;
        private readonly ILoadingCurtain _curtain;

        public BootstrapState(
            IStateMachine<IGameState> stateMachine, 
            ISceneLoader sceneLoader, 
            IStaticDataService staticDataService, 
            ILoadingCurtain curtain)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
            _staticData = staticDataService;
            _curtain = curtain;
        }

        public void Enter()
        {
            Application.targetFrameRate = (int)_staticData.GameConfig.TargetFPS;
            
            _curtain.Show();
            _sceneLoader.Load(_staticData.GameConfig.InitialScene, OnLevelLoad);
        }

        public void Exit()
        {

        }

        private void OnLevelLoad() => _stateMachine.Enter<LoadProgressState>();
    }
}