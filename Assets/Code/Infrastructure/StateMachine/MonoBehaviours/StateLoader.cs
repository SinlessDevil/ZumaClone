using Code.Infrastructure.StateMachine.Game.States;
using UnityEngine;
using Zenject;

namespace Code.Infrastructure.StateMachine.MonoBehaviours
{
    public abstract class StateLoader<TBaseState> : MonoBehaviour where TBaseState : class
    {
        private IStateMachine<TBaseState> _stateMachine;

        [Inject]
        public void Constructor(IStateMachine<TBaseState> stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void LoadState<TState>() where TState : class, TBaseState, IState
        {
            _stateMachine.Enter<TState>();
        }

        public void LoadState<TState, TPayload>(TPayload payload)
            where TState : class, TBaseState, IPayloadedState<TPayload>
        {
            _stateMachine.Enter<TState, TPayload>(payload);
        }
    }
}