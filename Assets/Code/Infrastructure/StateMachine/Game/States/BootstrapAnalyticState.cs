namespace Code.Infrastructure.StateMachine.Game.States
{
    public class BootstrapAnalyticState : IState, IGameState
    {
        private readonly IStateMachine<IGameState> _stateMachine;

        public BootstrapAnalyticState(IStateMachine<IGameState> stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            _stateMachine.Enter<PreLoadGameState, TypeLoad>(TypeLoad.InitialLoading);
        }

        public void Exit()
        {

        }
    }
}