using Code.Services.Factories.Game;
using Code.Services.Finish;
using Code.Services.Input;
using Code.Services.Levels;
using Code.Services.Timer;
using Cysharp.Threading.Tasks;

namespace Code.Services.BallController
{
    public class LoseBallChainHandler
    {
        private readonly ChainState _chainState;

        private readonly ITimeService _timeService;
        private readonly ILevelService _levelService;
        private readonly IInputService _inputService;
        private readonly IGameFactory _gameFactory;
        private readonly IFinishService _finishService;

        public LoseBallChainHandler(
            ChainState chainState,
            ITimeService timeService,
            ILevelService levelService,
            IInputService inputService,
            IGameFactory gameFactory,
            IFinishService finishService)
        {
            _chainState = chainState;
            _timeService = timeService;
            _levelService = levelService;
            _inputService = inputService;
            _gameFactory = gameFactory;
            _finishService = finishService;
        }

        public bool IsLose => _chainState.HasLost;

        public void TryLose(ChainSegment segment)
        {
            if (!_chainState.TryFinish(won: false))
                return;
            TriggerDefeatConditionAsync().Forget();

            (segment.Track?.End ?? _levelService.GetLevelHolder().LevelEnd).SetMouthProgressionAnimation(1f);
        }

        private async UniTask TriggerDefeatConditionAsync()
        {
            _timeService.StopTimer();
            _inputService.Cleanup();

            _gameFactory.Player.PlayerAnimator.PlayLoopRotation();
            await UniTask.WaitUntil(() => _chainState.TotalBallCount == 0, cancellationToken: _chainState.Token);
            _gameFactory.Player.PlayerAnimator.StopRotation();
            await UniTask.Delay(1000, cancellationToken: _chainState.Token);

            _finishService.Lose();
        }
    }
}
