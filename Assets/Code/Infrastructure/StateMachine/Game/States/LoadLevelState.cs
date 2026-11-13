using System.Collections.Generic;
using System.Threading;
using Code.Services.BallController;
using Code.Services.Factories.Game;
using Code.Services.Factories.UIFactory;
using Code.Services.Input;
using Code.Services.Input.Device;
using Code.Services.Levels;
using Code.Services.Providers.Balls;
using Code.Services.Providers.Widgets;
using Code.Services.Random;
using Code.Services.SaveLoad;
using Code.Services.StaticData;
using Code.StaticData;
using Code.StaticData.Levels;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Infrastructure.StateMachine.Game.States
{
    public class LoadLevelState : IPayloadedState<string>, IGameState
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly ILoadingCurtain _loadingCurtain;
        private readonly IUIFactory _uiFactory;
        private readonly IStateMachine<IGameState> _gameStateMachine;
        private readonly IGameFactory _gameFactory;
        private readonly IInputService _inputService;
        private readonly ILevelService _levelService;
        private readonly IBallProvider _ballProvider;
        private readonly IWidgetProvider _widgetProvider;
        private readonly IBallChainController _ballChainController;
        private readonly IStaticDataService _staticDataService;
        private readonly IRandomService _randomService;
        private readonly ISaveLoadFacade _saveLoad;
        private int _loadVersion;

        public LoadLevelState(
            IStateMachine<IGameState> gameStateMachine, 
            ISceneLoader sceneLoader,
            ILoadingCurtain loadingCurtain, 
            IUIFactory uiFactory, 
            IGameFactory gameFactory,
            IInputService inputService,
            ILevelService levelService,
            IBallProvider ballProvider,
            IWidgetProvider widgetProvider,
            IBallChainController ballChainController,
            IStaticDataService staticDataService,
            IRandomService randomService,
            ISaveLoadFacade saveLoad)
        {
            _gameFactory = gameFactory;
            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
            _loadingCurtain = loadingCurtain;
            _uiFactory = uiFactory;
            _inputService = inputService;
            _levelService = levelService;
            _ballProvider = ballProvider;
            _widgetProvider = widgetProvider;
            _ballChainController = ballChainController;
            _staticDataService = staticDataService;
            _randomService = randomService;
            _saveLoad = saveLoad;
        }

        public void Enter(string payload)
        {
            _loadingCurtain.Show();
            int version = ++_loadVersion;
            _sceneLoader.Load(payload, () =>
            {
                if (version == _loadVersion)
                    OnLevelLoad();
            });
        }

        public void Exit()
        {
            ++_loadVersion;
            _loadingCurtain.Hide();
        }

        protected virtual void OnLevelLoad()
        {
            if (_levelService.Mode == ZumaMode.Adventure)
                _levelService.PrepareAdventureSelection();
            InitGameWorld();
            if (_levelService.VisitLevel())
                _saveLoad.SaveProgress(SaveMethodType.PlayerPrefs);

            _gameStateMachine.Enter<GameLoopState>();
        }

        private void InitGameWorld()
        {
            _uiFactory.CreateUiRoot();
            
            InitHud();
            
            InitLevelHolder();

            InitProviders();
            
            InitPlayer();

            InitBallChainController();
            
            PlayStartLevelAsync(_ballChainController.AttemptToken).Forget();
        }

        private void InitLevelHolder()
        {
            var levelStaticData = _levelService.GetCurrentLevelStaticData();
            var levelHolder = _gameFactory.CreateLevelHolder(levelStaticData);
            _levelService.SetLevelHolder(levelHolder);
            
            levelHolder.RoadMeshCreator.TriggerUpdate();
            levelHolder.RoadMeshCreator.MeshHolder.transform.SetParent(levelHolder.transform);
            levelHolder.RoadMeshCreator.MeshHolder.transform.position = new Vector3(0f, -0.11f, 0f);
            levelHolder.RoadMeshCreator.MeshHolder.transform.rotation = Quaternion.Euler(-0.6f, 0f, 0f);
        }

        private void InitProviders()
        {
            _ballProvider.CreatePoolBall();
            _widgetProvider.CreatePoolWidgets();
        }
        
        private void InitPlayer()
        {
            var levelHolder = _levelService.GetLevelHolder();
            var player = _gameFactory.CreatePlayer(levelHolder.SpawnPositionPlayer.position, Quaternion.identity);
            player.Initialize(levelHolder.PathCreator);
        }

        private void InitHud()
        {
            var gameHud = _uiFactory.CreateGameHud();
            gameHud.Initialize();
        }

        private void InitBallChainController()
        {
            var levelHolder = _levelService.GetLevelHolder();
            BallChainDTO ballChainDTO = _staticDataService.GetBallChainDTO();
            LevelConfig config = _levelService.GetCurrentLevelStaticData().LevelConfig;
            ballChainDTO.Survival = _levelService.Mode == ZumaMode.Survival;
            ballChainDTO.ZumaTargetScore = config.GetZumaTarget();
            ballChainDTO.AceTime = config.AceTime;
            ballChainDTO.CollisionThreshold = Mathf.Min(ballChainDTO.CollisionThreshold, ballChainDTO.SpacingBalls * 1.15f);
            float levelMoveSpeed = config.BallMoveSpeedOverride;
            if (levelMoveSpeed > 0f)
                ballChainDTO.MoveSpeed = levelMoveSpeed;

            _ballChainController.Initialize(levelHolder.PathCreator, ballChainDTO);
        }
        
        private async UniTask PlayStartLevelAsync(CancellationToken token)
        {
            await UniTask.WaitUntil(() => !_loadingCurtain.IsActive, cancellationToken: token);
            await UniTask.Delay(150, cancellationToken: token);
            
            var startLevelInfoDisplayer = _uiFactory.CreateStartLevelInfoDisplayer();
            var nameLevel = _levelService.GetCurrentLevelStaticData().LevelName;
            var numberLevel = _levelService.GetCurrentChapter() + "-" + _levelService.GetCurrentLevel();
            var level = nameLevel + " " + numberLevel;
            startLevelInfoDisplayer.Initialize(level);
            startLevelInfoDisplayer.Play();
            await UniTask.WaitUntil(() => !startLevelInfoDisplayer.IsActive, cancellationToken: token);
            
            await _ballChainController.MoveParticleAlongPathAsync(_levelService.GetLevelHolder().DefaultParticleSystemHolder);
            
            token.ThrowIfCancellationRequested();
            List<Color> colors = _randomService.GetColorsByLevelRandomConfig();
            
            _gameFactory.Player.PlayerShooting.Activate(colors);
            _ballChainController.StartBallSpawning(colors);

            TypeInput inputType = Application.isMobilePlatform ? TypeInput.Mobile : TypeInput.PC;
            switch (inputType)
            {
                case TypeInput.PC:
                    _inputService.SetInputDevice(new MouseInputDevice());
                    break;
                case TypeInput.Mobile:
                    _inputService.SetInputDevice(new TouchInputDevice());
                    break;
            }
        }
    }
}