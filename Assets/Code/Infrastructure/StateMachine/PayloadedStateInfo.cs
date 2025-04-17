using System;
using Code.Infrastructure.StateMachine.Game.States;

namespace Code.Infrastructure.StateMachine
{
    public class PayloadedStateInfo<TState, TBaseState, TPayload> : IStateInfo where TState : class, IPayloadedState<TPayload>, TBaseState
    {
        private readonly StateMachine<TBaseState> _stateMachine;
        private readonly TState _state;
        private readonly TPayload _payload;

        public PayloadedStateInfo(StateMachine<TBaseState> stateMachine, TState state, TPayload payload)
        {
            _stateMachine = stateMachine;
            _state = state;
            _payload = payload;

            StateType = typeof(TState);
        }

        public Type StateType { get; }
        
        public void Enter() => 
            _stateMachine.Enter<TState, TPayload>(_payload);

        public void Update()
        {
            if(_state is IUpdatable updatableState)
                updatableState.Update();
        }

        public void Exit() => 
            _state.Exit();
    }
}