using System.Linq;
using Code.Services.Levels;
using Cysharp.Threading.Tasks;

namespace Code.Services.BallController
{
    public class MouthChainHandler
    {
        private readonly ParticleChainHandler _particleChainHandler;
        private readonly ChainTracker _chainTracker;
        private readonly ILevelService _levelService;

        public MouthChainHandler(
            ParticleChainHandler particleChainHandler,
            ChainTracker chainTracker,
            ILevelService levelService)
        {
            _particleChainHandler = particleChainHandler;
            _chainTracker = chainTracker;
            _levelService = levelService;
        }

        public void TryUpdateMouthProgress(float percentage)
        {
            float pathLength = _levelService.GetLevelHolder().PathCreator.path.length;
            float thresholdDistance = pathLength * percentage;
    
            float closestDistnceAlongPath = _levelService.GetLevelHolder().PathCreator.path.GetClosestDistanceAlongPath(_chainTracker.Balls.First().transform.position);
            float remainingDistance = pathLength - closestDistnceAlongPath;

            if (remainingDistance <= thresholdDistance) 
            {
                _levelService.GetLevelHolder().LevelEnd.UpdateMouthProgress(closestDistnceAlongPath, pathLength, thresholdDistance);

                if (_levelService.GetLevelHolder().LoseParticleSystemHolder.IsActive == false)
                {
                    _particleChainHandler.MoveParticleAlongPathAsync(_levelService.GetLevelHolder().LoseParticleSystemHolder).Forget();   
                }
            }
        }
    }
}