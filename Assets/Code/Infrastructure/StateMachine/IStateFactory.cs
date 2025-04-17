using Code.Infrastructure.StateMachine.Game.States;

namespace Code.Infrastructure.StateMachine
{
    public interface IStateFactory
    {
        T GetState<T>() where T : class, IExitable;
    }
}