using System;
using System.Collections.Generic;
using Code.Infrastructure.StateMachine;
using Code.Infrastructure.StateMachine.Game.States;
using Code.Services.Factories.UIFactory;
using Code.Services.Levels;
using Code.Services.PersistenceProgress;
using Code.Services.SaveLoad;
using Code.Services.SFX;
using Code.Services.StaticData;
using Code.StaticData.Levels;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.UI.Menu.Windows.Map
{
    public class MapWindow : BaseWindow
    {
        [SerializeField] private Text _chapterName;
        [SerializeField] private GridLayoutGroup _gridLayoutGroup;
        [SerializeField] private List<ButtonSwipeChapter> _buttonsSwipeChapter;
        [SerializeField] private List<CanvasGroup> _canvasGroups;

        private int _currentChapterIndex;

        private List<ItemLevel> _levelPool = new();
        private List<ChapterStaticData> _chapters;

        private ILevelService _levelService;
        private IUIFactory _uiFactory;
        private ISaveLoadService _saveLoadService;
        private IStateMachine<IGameState> _stateMachine;
        private IStaticDataService _staticData;
        private ISoundService _soundService;
        
        [Inject]
        public void Constructor(
            ILevelService levelService,
            IUIFactory uiFactory,
            ISaveLoadService saveLoadService,
            IStateMachine<IGameState> stateMachine,
            IStaticDataService staticData,
            IPersistenceProgressService persistenceProgressService,
            ISoundService soundService)
        {
            _levelService = levelService;
            _uiFactory = uiFactory;
            _saveLoadService = saveLoadService;
            _stateMachine = stateMachine;
            _staticData = staticData;
            _soundService = soundService;
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        public override void Initialize()
        {
            foreach (var buttonSwipeChapter in _buttonsSwipeChapter)
            {
                buttonSwipeChapter.Initialize();
                buttonSwipeChapter.SwipedChapterEvent += OnSwipeChapter;
            }

            _chapters = _levelService.GetAllChapters();
            
            _currentChapterIndex = _levelService.GetCurrentChapterIndex();

            UpdateLevelUI();
        }

        private void UnsubscribeEvents()
        {
            foreach (var buttonSwipeChapter in _buttonsSwipeChapter)
            {
                buttonSwipeChapter.SwipedChapterEvent -= OnSwipeChapter;
            }

            foreach (var itemLevel in _levelPool)
            {
                itemLevel.LoadLevelEvent -= OnLoadLevel;
            }
        }

        private void OnSwipeChapter(TypeSwipe typeSwipe)
        {
            _soundService.ButtonClick();
            
            switch (typeSwipe)
            {
                case TypeSwipe.Left when _currentChapterIndex > 0:
                    _currentChapterIndex--;
                    break;
                case TypeSwipe.Right when _currentChapterIndex < _chapters.Count - 1:
                    _currentChapterIndex++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeSwipe), typeSwipe, null);
            }

            AnimationRefreshWindow(DisableButton, EnableButton, UpdateLevelUI);
        }

        private void AnimationRefreshWindow(Action disableButton, Action enableButton, Action refreshWindow)
        {
            foreach (var canvasGroup in _canvasGroups)
            {
                canvasGroup.DOFade(0f, 0.2f)
                    .OnStart(() =>
                    {
                        disableButton();
                    })
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        refreshWindow();
                        
                        canvasGroup.DOFade(1f, 0.2f)
                            .SetEase(Ease.Linear)
                            .OnComplete(() =>
                            {
                                enableButton();
                            });
                    });
            }
        }

        private void UpdateLevelUI()
        {
            SetNameChapter();

            ClearLevelPool();

            var currentChapter = _chapters[_currentChapterIndex];
            
            foreach (var level in currentChapter.Levels)
            {
                var levelIndex = currentChapter.Levels.IndexOf(level) + 1;
                var chapterIndex = _currentChapterIndex + 1;

                var levelItem = GetPooledItemLevel();
                levelItem.Initialize(levelIndex, chapterIndex);

                if (_levelService.IsLevelCurrent(chapterIndex, levelIndex))
                {
                    levelItem.SetCurrent();
                    continue;
                }

                if (_levelService.IsLastCompletedLevel(chapterIndex, levelIndex))
                {
                    levelItem.SetUnlockedNonCompleted();
                    continue;
                }

                if (_levelService.IsLevelCompleted(chapterIndex, levelIndex))
                {
                    levelItem.SetCompleted();
                    continue;
                }

                levelItem.SetLocked();
            }

            UpdateSwipeButtons();
        }

        private void SetNameChapter()
        {
            var currentChapter = _currentChapterIndex + 1;
            _chapterName.text = "Chapter " + currentChapter;
        }

        private ItemLevel GetPooledItemLevel()
        {
            foreach (var item in _levelPool)
            {
                if (!item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(true);
                    return item;
                }
            }

            var newItem = _uiFactory.CreateItemLevel(_gridLayoutGroup.transform);
            newItem.LoadLevelEvent += OnLoadLevel;
            _levelPool.Add(newItem);
            return newItem;
        }

        private void OnLoadLevel(int levelNumber, int chapterId)
        {
            _soundService.ButtonClick();
            
            _levelService.SetUpCurrentLevel(levelNumber, chapterId);
            _saveLoadService.SaveProgress();

            _stateMachine.Enter<LoadLevelState, string>(_staticData.GameConfig.GameScene);
        }

        private void ClearLevelPool()
        {
            foreach (var item in _levelPool)
            {
                item.gameObject.SetActive(false);
            }
        }

        private void UpdateSwipeButtons()
        {
            _buttonsSwipeChapter[0].gameObject.SetActive(_currentChapterIndex > 0);
            _buttonsSwipeChapter[1].gameObject.SetActive(_currentChapterIndex < _chapters.Count - 1);
        }

        private void EnableButton()
        {
            foreach (var buttonSwipeChapter in _buttonsSwipeChapter)
            {
                buttonSwipeChapter.Enable();
            }
        }

        private void DisableButton()
        {
            foreach (var buttonSwipeChapter in _buttonsSwipeChapter)
            {
                buttonSwipeChapter.Disable();
            }
        }
    }
}