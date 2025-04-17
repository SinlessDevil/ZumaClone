namespace Code.Infrastructure.StateMachine.Game.States
{
    public interface IPayloadedState<TPayload> : IExitable
    {
        void Enter(TPayload payload);
    }
}