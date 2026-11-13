using Code.Services.BallController;
using Code.Services.Providers.Balls;
using UnityEngine;
using Zenject;

namespace Code.Logic.Zuma.Balls
{
    public class BallIntersectionTracker : MonoBehaviour
    {
        private Ball _ball;
        private IBallProvider _ballProvider;
        private IBallChainController _ballChainController;

        [Inject]
        private void Constructor(IBallProvider ballProvider, IBallChainController ballChainController)
        {
            _ballProvider = ballProvider;
            _ballChainController = ballChainController;
        }

        public void Initialize(Ball ball) => _ball = ball;

        public bool ResolveStep(Vector3 start, Vector3 end)
        {
            if (!_ballChainController.AttemptToken.IsCancellationRequested &&
                _ballChainController.TryAttachBall(_ball, start, end))
                return true;

            if (!_ballChainController.AttemptToken.IsCancellationRequested &&
                !_ballChainController.IsOutsideField(end)) 
                return false;
            
            _ballChainController.ResolveMiss(_ball);
            _ballProvider.ReturnBall(_ball);
            
            return true;

        }
    }
}
