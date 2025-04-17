using System;
using Code.Infrastructure.StateMachine;
using Code.Infrastructure.StateMachine.Game.States;
using Code.Services.PersistenceProgress;
using Code.Services.PersistenceProgress.Player;
using Code.Services.SaveLoad;
using Code.Services.SFX;
using Code.Services.StaticData;
using Code.Services.Timer;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Window.Setting
{
    public class SettingWindow : MonoBehaviour
    {
        [SerializeField] private ToggleContainer _toggleMusic;
        [SerializeField] private ToggleContainer _toggleSound;
        [SerializeField] private ToggleContainer _toggleVibrations;
        [Space(10)] 
        [SerializeField] private Button[] _buttons;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _quitToMenuButton;
        [SerializeField] private Button _restartLevelButton;
        [Space(10)]
        [SerializeField] private Color _enabledColor;
        [SerializeField] private Color _disabledColor;

        private const float AnimationDuration = 0.5f;
        
        private PlayerSettings _playerSettings;

        private ISaveLoadService _saveLoadService;
        private ISoundService _soundService;
        private ITimeService _timeService;
        private IStaticDataService _staticDataService;
        private IStateMachine<IGameState> _gameStateMachine;

        [Inject]
        public void Constructor(
            IPersistenceProgressService progressService, 
            ISaveLoadService saveLoadService,
            ISoundService soundService, 
            ITimeService timeService,
            IStaticDataService staticDataService,
            IStateMachine<IGameState> gameStateMachine)
        {
            _soundService = soundService;
            _saveLoadService = saveLoadService;
            _timeService = timeService;
            _staticDataService = staticDataService;
            _gameStateMachine = gameStateMachine;
            
            _playerSettings = progressService.PlayerData.PlayerSettings;
        }

        private void OnEnable()
        {
            _toggleMusic.Button.onClick.AddListener(() => UpdateSetting(ref _playerSettings.Music));
            _toggleSound.Button.onClick.AddListener(() => UpdateSetting(ref _playerSettings.Sound));
            _toggleVibrations.Button.onClick.AddListener(() => UpdateSetting(ref _playerSettings.Vibration));
            
            _continueButton.onClick.AddListener(OnHideWindow);
            _quitToMenuButton.onClick.AddListener(OnQuitToMenu);
            _restartLevelButton.onClick.AddListener(OnRestartLevel);
        }
        private void OnDisable()
        {
            _toggleMusic.Button.onClick.RemoveListener(() => UpdateSetting(ref _playerSettings.Music));
            _toggleSound.Button.onClick.RemoveListener(() => UpdateSetting(ref _playerSettings.Sound));
            _toggleVibrations.Button.onClick.RemoveListener(() => UpdateSetting(ref _playerSettings.Vibration));
            
            _continueButton.onClick.RemoveListener(OnHideWindow);
            _quitToMenuButton.onClick.RemoveListener(OnQuitToMenu);
            _restartLevelButton.onClick.RemoveListener(OnRestartLevel);
        }

        private void OnHideWindow()
        {
            _soundService.ButtonClick();
            _timeService.SimpleMode();
            
            Destroy(this.gameObject);
        }
        
        private void OnQuitToMenu()
        {
            _soundService.ButtonClick();
            _timeService.SimpleMode();
            
            _gameStateMachine.Enter<LoadMenuState, string>(_staticDataService.GameConfig.MenuScene);
        }
        
        private void OnRestartLevel()
        {
            _soundService.ButtonClick();
            _timeService.SimpleMode();
            
            _gameStateMachine.Enter<LoadLevelState, string>(_staticDataService.GameConfig.GameScene);
        }

        private void UpdateSetting(ref bool setting)
        {
            _soundService.ButtonClick();
            
            setting = !setting;
            UpdateWindow();
            
            _saveLoadService.SaveProgress();
        }

        public void UpdateWindow()
        {
            UpdateColor();
            UpdateRectTransform();
        }
        private void UpdateColor()
        {
            _toggleMusic.Image.DOColor(SelectColor(_playerSettings.Music), AnimationDuration)
                .SetUpdate(true);
            _toggleSound.Image.DOColor(SelectColor(_playerSettings.Sound), AnimationDuration)
                .SetUpdate(true);
            _toggleVibrations.Image.DOColor(SelectColor(_playerSettings.Vibration), AnimationDuration)
                .SetUpdate(true);
        }
        private void UpdateRectTransform()
        {
            _toggleMusic.Image.rectTransform.DOAnchorPosX(SelectPositionX(_playerSettings.Music), AnimationDuration)
                .SetUpdate(true);
            _toggleSound.Image.rectTransform.DOAnchorPosX(SelectPositionX(_playerSettings.Sound), AnimationDuration)
                .SetUpdate(true);
            _toggleVibrations.Image.rectTransform.DOAnchorPosX(SelectPositionX(_playerSettings.Vibration), AnimationDuration)
                .SetUpdate(true);
        }
        
        private Color SelectColor(bool value) => value ? _enabledColor : _disabledColor;
        private float SelectPositionX(bool value) => value ? 50f : -50f;

        public void ResetButtonScale()
        {
            foreach (var button in _buttons)
            {
                button.transform.localScale = Vector3.one;
            }
        }
    }

    [Serializable]
    public class ToggleContainer
    {
        public Button Button;
        public Image Image;
    }
}