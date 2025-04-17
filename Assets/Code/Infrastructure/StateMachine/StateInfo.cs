using System;
using Code.Infrastructure.StateMachine.Game.States;

namespace Code.Infrastructure.StateMachine
{
    public class StateInfo<TState, TBaseState> : IStateInfo where TState : class, IState, TBaseState
    {
        private readonly StateMachine<TBaseState> _stateMachine;
        private readonly TState _state;
        private readonly IUpdatable _updatable;

        public StateInfo(StateMachine<TBaseState> stateMachine, TState state)
        {
            _stateMachine = stateMachine;
            _state = state;
            _updatable = state as IUpdatable;
            StateType = typeof(TState);
        }

        public Type StateType { get; }

        public virtual void Enter() =>
            _stateMachine.Enter<TState>();

        public void Update() =>
            _updatable?.Update();

        public void Exit() =>
            _state.Exit();
    }
}