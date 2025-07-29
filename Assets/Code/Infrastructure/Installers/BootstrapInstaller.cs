using Code.Infrastructure.StateMachine;
using Code.Infrastructure.StateMachine.Game;
using Code.Infrastructure.StateMachine.Game.States;
using Code.Localization.Code.Services.LocalizeLanguageService;
using Code.Services.BallController;
using Code.Services.Factories.Game;
using Code.Services.Factories.UIFactory;
using Code.Services.Finish;
using Code.Services.Finish.Lose;
using Code.Services.Finish.Win;
using Code.Services.Input;
using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.Services.PersistenceProgress;
using Code.Services.Providers.Balls;
using Code.Services.Providers.Widgets;
using Code.Services.Random;
using Code.Services.SaveLoad;
using Code.Services.SFX.Music;
using Code.Services.SFX.Sound;
using Code.Services.SFX.StaticData;
using Code.Services.SFX.Vibration;
using Code.Services.StaticData;
using Code.Services.Timer;
using Code.Services.Window;
using UnityEngine;
using Zenject;
using Application = UnityEngine.Application;

namespace Code.Infrastructure.Installers
{
    public class BootstrapInstaller : MonoInstaller, IInitializable
    {
        [SerializeField] private CoroutineRunner _coroutineRunner;
        [SerializeField] private LoadingCurtain _curtain;
        [SerializeField] private TimeService _timeService;
        
        private RuntimePlatform Platform => Application.platform;

        public override void InstallBindings()
        {
            Debug.Log("Installer");

            BindMonoServices();
            BindServices();
            BindGameStateMachine();
            MakeInitializable();
        }
        
        public void Initialize() => BootstrapGame();

        private void BindMonoServices()
        {
            Container.Bind<ICoroutineRunner>().FromMethod(() => Container.InstantiatePrefabForComponent<ICoroutineRunner>(_coroutineRunner)).AsSingle();
            Container.Bind<ILoadingCurtain>().FromMethod(() => Container.InstantiatePrefabForComponent<ILoadingCurtain>(_curtain)).AsSingle();
            Container.Bind<ITimeService>().FromMethod(() => Container.InstantiatePrefabForComponent<ITimeService>(_timeService)).AsSingle();
            
            BindSceneLoader();
        }

        private void BindServices()
        {
            BindStaticDataService();
            BindFactory();
            
            Container.BindInterfacesTo<WindowService>().AsSingle();
            Container.BindInterfacesTo<InputService>().AsSingle();
            Container.BindInterfacesTo<PersistenceProgressService>().AsSingle();
            Container.BindInterfacesTo<RandomService>().AsSingle();
            Container.BindInterfacesTo<UnifiedSaveLoadFacade>().AsSingle();
            Container.BindInterfacesTo<BallProvider>().AsSingle();
            Container.BindInterfacesTo<WidgetProvider>().AsSingle();
            Container.BindInterfacesTo<LevelService>().AsSingle();
            Container.BindInterfacesTo<BallChainController>().AsSingle();
            Container.BindInterfacesTo<LevelLocalProgressService>().AsSingle();
            
            BindFinishServices();
            BindAudioVibration();
            BindLocalizeLanguage();
        }

        private void BindFactory()
        {
            Container.BindInterfacesTo<UIFactory>().AsSingle();
            Container.BindInterfacesTo<GameFactory>().AsSingle();
        }

        private void BindFinishServices()
        {
            Container.BindInterfacesTo<FinishService>().AsSingle();
            Container.BindInterfacesTo<WinService>().AsSingle();
            Container.BindInterfacesTo<LoseService>().AsSingle();
        }

        private void BindAudioVibration()
        {
            Container.Bind<ISoundService>().To<SoundService>().AsSingle();
            Container.Bind<IMusicService>().To<MusicService>().AsSingle();
            Container.Bind<IVibrationService>().To<VibrationService>().AsSingle();
        }

        private void BindLocalizeLanguage()
        {
            Container.BindInterfacesTo<LocalizeLanguageService>().AsSingle();
        }
        
        private void BindGameStateMachine()
        {
            Container.Bind<GameStateFactory>().AsSingle();
            Container.BindInterfacesTo<GameStateMachine>().AsSingle();
            
            BindGameStates();
        }

        private void MakeInitializable() => Container.Bind<IInitializable>().FromInstance(this);

        private void BindSceneLoader()
        {
            ISceneLoader sceneLoader = new SceneLoader(Container.Resolve<ICoroutineRunner>());
            Container.Bind<ISceneLoader>().FromInstance(sceneLoader).AsSingle();
        }

        private void BindStaticDataService()
        {
            IStaticDataService staticDataService = new StaticDataService();
            staticDataService.LoadData();
            Container.Bind<IStaticDataService>().FromInstance(staticDataService).AsSingle();
            
            Container.Bind<IAudioVibrationStaticDataService>().To<AudioVibrationStaticDataService>().AsSingle();
            Container.Resolve<IAudioVibrationStaticDataService>().LoadData();
        }
        
        private void BindGameStates()
        {
            Container.Bind<BootstrapState>().AsSingle();
            Container.Bind<LoadProgressState>().AsSingle();
            Container.Bind<BootstrapAnalyticState>().AsSingle();
            Container.Bind<PreLoadGameState>().AsSingle();
            Container.Bind<LoadMenuState>().AsSingle();
            Container.Bind<LoadLevelState>().AsSingle();
            Container.Bind<LoadLevelTestState>().AsSingle();
            Container.Bind<GameLoopState>().AsSingle();
        }

        private void BootstrapGame()
        {
            CacheDataAudios();

            Container.Resolve<IStateMachine<IGameState>>().Enter<BootstrapState>();
        }

        private void CacheDataAudios()
        {
            Container.Resolve<ISoundService>().Cache2DSounds();
            Container.Resolve<ISoundService>().CreateSoundsPool();

            Container.Resolve<IMusicService>().CacheMusic();
            Container.Resolve<IMusicService>().CreateMusicRoot();
        }
    }
}