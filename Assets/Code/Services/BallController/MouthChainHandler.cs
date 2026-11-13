using Code.Logic.Zuma.Level;
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
            LevelHolder level = _levelService.GetLevelHolder();
            float pathLength = level.PathCreator.path.length;
            float thresholdDistance = pathLength * percentage;
            float closestDistanceAlongPath = _chainState.GetHeadDistance(level.PathCreator);
            float remainingDistance = pathLength - closestDistanceAlongPath;

            level.LevelEnd.UpdateMouthProgress(closestDistanceAlongPath, pathLength, thresholdDistance);
            if (!(thresholdDistance > 0f) || !(remainingDistance <= thresholdDistance)) 
                return;
            
            if (_levelService.GetLevelHolder().LoseParticleSystemHolder.IsActive == false)
                _particleChainHandler.MoveParticleAlongPathAsync(
                    _levelService.GetLevelHolder().LoseParticleSystemHolder).Forget();
        }
    }
}
