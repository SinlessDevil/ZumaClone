using System;
using Code.Services.Finish;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.UI.Buttons
{
    public class FinishButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TypeFinish _typeFinish;

        private IFinishService _finishService;
        
        [Inject]
        private void Constructor(IFinishService finishService)
        {
            _finishService = finishService;
        }
        
        private void OnValidate()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }

        private void Start() => _button.onClick.AddListener(OnFinishButtonClick);

        private void OnDestroy() => _button.onClick.RemoveListener(OnFinishButtonClick);

        private void OnFinishButtonClick()
        {
            switch (_typeFinish)
            {
                
                case TypeFinish.Win:
                    _finishService.Win();
                    break;
                case TypeFinish.Lose:
                    _finishService.Lose();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
    public enum TypeFinish
    {
        Win = 0,
        Lose = 1
    }
}