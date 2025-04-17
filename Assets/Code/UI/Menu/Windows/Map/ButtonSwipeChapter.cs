using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Menu.Windows.Map
{
    public class ButtonSwipeChapter : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TypeSwipe _typeSwipe;
        
        public event Action<TypeSwipe> SwipedChapterEvent;

        private void OnDestroy()
        { 
            UnsubscribeEvents();
        }

        public void Initialize()
        {
            UnsubscribeEvents();
            SubscribeEvents();
        }

        public void Enable()
        {
            _button.interactable = true;
        }
        
        public void Disable()
        {
            _button.interactable = false;
        }
        
        private void SubscribeEvents()
        {
            _button.onClick.AddListener(OnSwipeChapter);
        }
        
        private void UnsubscribeEvents()
        {
            _button.onClick.RemoveListener(OnSwipeChapter);
        }

        private void OnSwipeChapter()
        {
            SwipedChapterEvent?.Invoke(_typeSwipe);
        }
    }
}