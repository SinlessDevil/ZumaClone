using System;
using System.Collections.Generic;
using Code.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Window.Finish
{
    public abstract class FinishWindow : MonoBehaviour
    {
        [SerializeField] protected TMP_Text _textTime;
        [SerializeField] protected TMP_Text _textScore;
        [SerializeField] protected Button _buttonLoadLevel;
        [SerializeField] protected Button _buttonExitToMenu;
        [Space(10)] [Header("Addition Components")]
        [SerializeField] protected RectTransform _mainContaienr;
        [SerializeField] protected CanvasGroup _canvasGroupBG;
        [SerializeField] protected RectTransform _headerContainer;
        [SerializeField] protected List<ButtonScaler> _buttonScalers;

        private string _time;
        private string _score;
        
        public abstract void SetTime(string time);

        public abstract void SetScore(string score);

        protected virtual void SubscribeEvents()
        {
            _buttonLoadLevel.onClick.AddListener(OnLoadLevelButtonClick);
            _buttonExitToMenu.onClick.AddListener(OnExitToMenuButtonClick);
        }
        
        protected virtual void UnsubscribeEvents()
        {
            _buttonLoadLevel.onClick.RemoveListener(OnLoadLevelButtonClick);
            _buttonExitToMenu.onClick.RemoveListener(OnExitToMenuButtonClick);
        }

        public virtual void ResetWindow()
        {
            _mainContaienr.localScale = Vector3.zero;
            _canvasGroupBG.alpha = 0;
            _headerContainer.localScale = new Vector3(0, 1, 1);
            
            _score = _textScore.text;
            _time = _textTime.text;
            
            _textTime.text = string.Empty;
            _textScore.text = string.Empty;
            
            _buttonLoadLevel.transform.localScale = Vector3.zero;
            _buttonExitToMenu.transform.localScale = Vector3.zero;
        }
        
        public virtual void OpenWindow(Action onFinished)
        {
            Sequence sequence = DOTween.Sequence();

            sequence.Append(_canvasGroupBG.DOFade(1f, 0.5f).SetEase(Ease.Linear));
            sequence.Join(_mainContaienr.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBounce));
            sequence.AppendInterval(0.2f);
            sequence.Append(_headerContainer.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBounce));
            sequence.AppendInterval(0.2f);
            
            int parsedScore = int.TryParse(_score, out var s) ? s : 0;
            int currentScore = 0;
            sequence.Append(DOTween.To(() => currentScore, x =>
            {
                currentScore = x;
                _textScore.text = currentScore.ToString();
            }, parsedScore, 0.25f).SetEase(Ease.Linear));
            
            float parsedTime = float.TryParse(_time, out var t) ? t : 0f;
            float currentTime = 0;
            sequence.Append(DOTween.To(() => currentTime, x =>
            {
                currentTime = x;
                _textTime.text = currentTime.ToString("F1");
            }, parsedTime, 0.25f).SetEase(Ease.Linear));

            sequence.AppendInterval(0.2f);
            sequence.Append(_buttonLoadLevel.transform.DOScale(1, 0.5f).SetEase(Ease.OutBounce));
            sequence.Join(_buttonExitToMenu.transform.DOScale(1, 0.5f).SetEase(Ease.OutBounce));

            sequence.OnComplete(() =>
            {
                _buttonScalers.ForEach(x => x.SetupScaleOrigin());
                onFinished?.Invoke();
            });
        }

        
        protected abstract void OnLoadLevelButtonClick();
        protected abstract void OnExitToMenuButtonClick();
    }   
}