using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Code.Infrastructure
{
    public class LoadingCurtain : MonoBehaviour, ILoadingCurtain
    {
        private const float Delay = 1.75f;
        private const float AnimationDuration = 0.65f;
        private const float TextUpdateInterval = 0.15f;

        [SerializeField] private RectTransform _right;
        [SerializeField] private RectTransform _left;
        [SerializeField] private TMP_Text _loadingText;

        private CancellationTokenSource _textAnimationCts;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public bool IsActive { get; private set; }

        public void Show()
        {
            IsActive = true;
            gameObject.SetActive(true);
            _left.anchoredPosition = Vector2.zero;
            _right.anchoredPosition = Vector2.zero;
        }

        public void Hide()
        {
            AnimationOpenAsync().Forget();
            StartLoadingTextAnimation();
        }

        private async UniTaskVoid AnimationOpenAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Delay), cancellationToken: this.GetCancellationTokenOnDestroy());

            float screenWidth = Screen.width;
            float elapsedTime = 0f;

            Vector2 leftStart = _left.anchoredPosition;
            Vector2 leftTarget = new Vector2(-screenWidth / 2f, leftStart.y);

            Vector2 rightStart = _right.anchoredPosition;
            Vector2 rightTarget = new Vector2(screenWidth / 2f, rightStart.y);

            while (elapsedTime < AnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / AnimationDuration);
                _left.anchoredPosition = Vector2.Lerp(leftStart, leftTarget, t);
                _right.anchoredPosition = Vector2.Lerp(rightStart, rightTarget, t);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            _left.anchoredPosition = leftTarget;
            _right.anchoredPosition = rightTarget;

            StopLoadingTextAnimation();
            gameObject.SetActive(false);
            IsActive = false;
        }

        private void StartLoadingTextAnimation()
        {
            StopLoadingTextAnimation();
            _textAnimationCts = new CancellationTokenSource();
            AnimateLoadingTextAsync(_textAnimationCts.Token).Forget();
        }

        private void StopLoadingTextAnimation()
        {
            if (_textAnimationCts != null && !_textAnimationCts.IsCancellationRequested)
                _textAnimationCts.Cancel();
            _textAnimationCts?.Dispose();
            _textAnimationCts = null;
        }

        private async UniTaskVoid AnimateLoadingTextAsync(CancellationToken token)
        {
            string baseText = "Loading";
            string[] dots = { "", ".", "..", "..." };
            int index = 0;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    _loadingText.text = baseText + dots[index];
                    index = (index + 1) % dots.Length;
                    await UniTask.Delay(TimeSpan.FromSeconds(TextUpdateInterval), cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
        }
    }
}
