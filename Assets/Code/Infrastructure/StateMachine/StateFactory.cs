using System;
using System.Collections.Generic;
using Code.Infrastructure.StateMachine.Game.States;
using Zenject;

namespace Code.Infrastructure.StateMachine
{
    public abstract class StateFactory : IFactory<Type, IExitable>, IStateFactory
    {
        private readonly Dictionary<Type, Func<IExitable>> _states;

        protected abstract Dictionary<Type, Func<IExitable>> BuildStatesRegister(DiContainer container);

        protected StateFactory(DiContainer container)
        {
            _states = BuildStatesRegister(container);
        }

        public IExitable Create(Type type)
        {
            if (!_states.TryGetValue(type, out Func<IExitable> state))
                throw new Exception($"State for {type.Name} can't be created");

            return state();
        }

        public T GetState<T>() where T : class, IExitable =>
            Create(typeof(T)) as T;
    }
}