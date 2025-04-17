using Code.Services.Finish;
using Code.Services.Input;
using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.Services.Timer;
using Cysharp.Threading.Tasks;
using PathCreation;
using UnityEngine;

namespace Code.Services.BallController
{
    public class WinBallChainHandler
    {
        private readonly BallChainDTO _ballChainDto;
        private readonly PathCreator _pathCreator;
        private readonly ChainTracker _chainTracker;
        
        private readonly ParticleChainHandler _particleChainHandler;
        private readonly WidgetBallChainProvider _widgetBallChainProvider;
        
        private readonly ITimeService _timeService;
        private readonly IInputService _inputService;
        private readonly IFinishService _finishService;
        private readonly ILevelLocalProgressService _levelLocalProgressService;
        private readonly ILevelService _levelService;

        public WinBallChainHandler(
            BallChainDTO ballChainDto, 
            PathCreator pathCreator,
            ParticleChainHandler particleChainHandler,
            WidgetBallChainProvider widgetBallChainProvider,
            ChainTracker chainTracker,
            IInputService inputService, 
            ITimeService timeService, 
            IFinishService finishService, 
            ILevelLocalProgressService levelLocalProgressService, 
            ILevelService levelService)
        {
            _ballChainDto = ballChainDto;
            _inputService = inputService;
            _timeService = timeService;
            _finishService = finishService;
            _levelLocalProgressService = levelLocalProgressService;
            _levelService = levelService;
            _chainTracker = chainTracker;
            _widgetBallChainProvider = widgetBallChainProvider;
            _particleChainHandler = particleChainHandler;
            _pathCreator = pathCreator;
        }

        public bool IsWin { get; private set; }
    
        public void TryWin(int countBall)
        {
            if (countBall <= 0 && !IsWin)
            {
                IsWin = true;
                TriggerWinConditionAsync().Forget();
            }
        }
        
        private async UniTask TriggerWinConditionAsync()
        {
            _timeService.StopTimer();
            _inputService.Cleanup();
            
            _particleChainHandler.MoveParticleAlongPathAsync(
                _levelService.GetLevelHolder().DefaultParticleSystemHolder)
                .Forget();
            
            float step = _ballChainDto.SetToSpawnWidget;
            float currentDistance = _chainTracker.DistanceTravelled;
            float pathEnd = _pathCreator.path.length;
            Color widgetColor = _ballChainDto.BaseColorWidget;
            string widgetText =  "+" + _levelService.GetCurrentLevelStaticData().LevelConfig.ScoreConfig.ScorePerStepPath;
            int index = 1;
            
            while (currentDistance < pathEnd)
            {
                Vector3 position = _pathCreator.path.GetPointAtDistance(currentDistance);
                _widgetBallChainProvider.SetUpWidget(position, widgetColor, widgetText);
                
                index++;
                currentDistance += step;
                
                _levelLocalProgressService.AddScore(_levelService.GetCurrentLevelStaticData().LevelConfig.ScoreConfig.ScorePerStepPath);
                
                await UniTask.Delay(_ballChainDto.TimeToSpawnWidget);
            }
            
            await UniTask.Delay(1000);
            
            _finishService.Win();
        }
    }
}