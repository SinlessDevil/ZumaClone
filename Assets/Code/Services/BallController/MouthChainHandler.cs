using Code.Services.Levels;
using Cysharp.Threading.Tasks;

namespace Code.Services.BallController
{
    public class MouthChainHandler
    {
        private readonly ParticleChainHandler _particleChainHandler;
        private readonly ChainState _chainState;
        private readonly ILevelService _levelService;

        public MouthChainHandler(
            ParticleChainHandler particleChainHandler,
            ChainState chainState,
            ILevelService levelService)
        {
            _particleChainHandler = particleChainHandler;
            _chainState = chainState;
            _levelService = levelService;
        }

        public void TryUpdateMouthProgress(float percentage)
        {
            var frontBall = _chainState.FrontBall;
            if (frontBall == null) return;

            float pathLength = _levelService.GetLevelHolder().PathCreator.path.length;
            float thresholdDistance = pathLength * percentage;

            float closestDistanceAlongPath = _levelService.GetLevelHolder().PathCreator.path
                .GetClosestDistanceAlongPath(frontBall.transform.position);
            float remainingDistance = pathLength - closestDistanceAlongPath;

            if (remainingDistance <= thresholdDistance)
            {
                _levelService.GetLevelHolder().LevelEnd
                    .UpdateMouthProgress(closestDistanceAlongPath, pathLength, thresholdDistance);

                if (_levelService.GetLevelHolder().LoseParticleSystemHolder.IsActive == false)
                    _particleChainHandler.MoveParticleAlongPathAsync(
                        _levelService.GetLevelHolder().LoseParticleSystemHolder).Forget();
            }
        }
    }
}
