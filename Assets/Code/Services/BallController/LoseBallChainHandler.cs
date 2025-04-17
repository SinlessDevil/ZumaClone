using Code.Services.Factories.Game;
using Code.Services.Finish;
using Code.Services.Input;
using Code.Services.Levels;
using Code.Services.Timer;
using Cysharp.Threading.Tasks;
using PathCreation;
using UnityEngine;

namespace Code.Services.BallController
{
    public class LoseBallChainHandler
    {
        private readonly BallChainDTO _ballChainDto;
        private readonly PathCreator _pathCreator;
        private readonly ChainTracker _chainTracker;
        
        private readonly ITimeService _timeService;
        private readonly ILevelService _levelService;
        private readonly IInputService _inputService;
        private readonly IGameFactory _gameFactory;
        private readonly IFinishService _finishService;

        public LoseBallChainHandler(
            BallChainDTO ballChainDto, 
            PathCreator pathCreator, 
            ChainTracker chainTracker,
            ITimeService timeService, 
            ILevelService levelService, 
            IInputService inputService, 
            IGameFactory gameFactory,
            IFinishService finishService)
        {
            _ballChainDto = ballChainDto;
            _pathCreator = pathCreator;
            _chainTracker = chainTracker;
            _timeService = timeService;
            _levelService = levelService;
            _inputService = inputService;
            _gameFactory = gameFactory;
            _finishService = finishService;
        }

        public bool IsLose { get; private set; }
        
        public void TryLose(Vector3 positionLastBall)
        {
            if (IsLose) 
                return;

            IsLose = true;
            
            TriggerDefeatConditionAsync().Forget();
            
            float thresholdDistance = _pathCreator.path.length * 0.2f;
            float distanceTravelled = _pathCreator.path.GetClosestDistanceAlongPath(positionLastBall);
            float remainingDistance = _pathCreator.path.length - distanceTravelled;
            float normalizedValue = Mathf.Clamp01(1 - (remainingDistance / thresholdDistance));
            
            _levelService.GetLevelHolder().LevelEnd.SetMouthProgressionAnimation(normalizedValue);
        }
        
        private async UniTask TriggerDefeatConditionAsync()
        {
            _timeService.StopTimer();
            _inputService.Cleanup();
            
            _gameFactory.Player.PlayerAnimator.PlayLoopRotation();
            _ballChainDto.MoveSpeed = _ballChainDto.BoostSpeedBallForLose;
            await UniTask.WaitUntil(() => _chainTracker.Balls.Count == 0);
            _gameFactory.Player.PlayerAnimator.StopRotation();
            await UniTask.Delay(1000);
            
            _finishService.Lose();
        }
    }
}