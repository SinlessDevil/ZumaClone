using System;
using System.Collections.Generic;
using Code.Infrastructure.StateMachine.Game.States;
using Zenject;

namespace Code.Infrastructure.StateMachine.Game
{
    public class GameStateFactory : StateFactory
    {
        public GameStateFactory(DiContainer container) : base(container)
        {
        }

        protected override Dictionary<Type, Func<IExitable>> BuildStatesRegister(DiContainer container)
        {
            return new Dictionary<Type, Func<IExitable>>()
            {
                [typeof(BootstrapState)] = container.Resolve<BootstrapState>,
                [typeof(LoadProgressState)] = container.Resolve<LoadProgressState>,
                [typeof(BootstrapAnalyticState)] = container.Resolve<BootstrapAnalyticState>,
                [typeof(PreLoadGameState)] = container.Resolve<PreLoadGameState>,
                [typeof(LoadMenuState)] = container.Resolve<LoadMenuState>,
                [typeof(LoadLevelState)] = container.Resolve<LoadLevelState>,
                [typeof(GameLoopState)] = container.Resolve<GameLoopState>
            };
        }
    }
}