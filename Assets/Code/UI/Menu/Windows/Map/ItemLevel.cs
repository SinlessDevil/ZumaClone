using System;
using System.Collections.Generic;
using Code.Services.SFX;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.UI.Menu.Windows.Map
{
    public class ItemLevel : MonoBehaviour
    {
        [SerializeField] private Image _mainImage;
        [SerializeField] private List<Text> _texts;
        [SerializeField] private Button _button;
        [Space(10)]
        [SerializeField] private GameObject _panelCompleted;
        [SerializeField] private GameObject _panelLocked;
        [SerializeField] private GameObject _panelCurrent;
        [SerializeField] private GameObject _panelDefault;
        
        private int _currentLevel;
        private int _currentChapter;
        
        private ISoundService _soundService;
        
        [Inject]
        private void Constructor(ISoundService soundService)
        {
            _soundService = soundService;
        }
        
        public event Action<int, int> LoadLevelEvent; 
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        
        public void Initialize(int CurrentLevel, int CurrentChapter)
        {
            _currentLevel = CurrentLevel;
            _currentChapter = CurrentChapter;
         
            SetText(_currentLevel.ToString());
            
            UnsubscribeEvents();
            SubscribeEvents();
        }
        
        public void SetCurrent()
        {
            DisableAllPanels();
            
            _panelDefault.SetActive(true);
            _panelCurrent.SetActive(true);
            
            _button.interactable = true;
        }
        
        public void SetCompleted()
        {
            DisableAllPanels();
            
            _panelCompleted.SetActive(true);
            
            _button.interactable = true;
        }

        public void SetUnlockedNonCompleted()
        {
            DisableAllPanels();
            
            _panelDefault.SetActive(true);
            
            _button.interactable = true;
        }
        
        public void SetLocked()
        {
            DisableAllPanels();
            
            _panelLocked.SetActive(true);
            
            _button.interactable = false;
        }
        
        private void DisableAllPanels()
        {
            _panelCompleted.SetActive(false);
            _panelLocked.SetActive(false);
            _panelCurrent.SetActive(false);
            _panelDefault.SetActive(false);
        }
        
        private void SetText(string text)
        {
            _texts.ForEach(x => x.text = text);
        }
        
        private void SubscribeEvents()
        {
            _button.onClick.AddListener(OnLoadLevel);
        }
        
        private void UnsubscribeEvents()
        {
            _button.onClick.RemoveListener(OnLoadLevel);
        }
        
        private void OnLoadLevel()
        {
            _soundService.ButtonClick();
            
            LoadLevelEvent?.Invoke(_currentLevel, _currentChapter);
        }
    }
}