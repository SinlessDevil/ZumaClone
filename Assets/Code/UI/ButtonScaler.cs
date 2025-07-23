using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace Code.UI
{
    public class ButtonScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image _image;
        
        [Header("Setup")]
        [SerializeField] private float ScaleAmount = 0.8f;
        [SerializeField] private float ScaleDuration = 0.2f;

        private Vector3 _originalScale;
        private CancellationTokenSource _scaleCts;

        private void Start() => SetupScaleOrigin();

        public void SetupScaleOrigin()
        {
            _originalScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_image.gameObject.activeInHierarchy)
                return;

            _scaleCts?.Cancel();
            _scaleCts = new CancellationTokenSource();
            _ = ScaleButtonAsync(_originalScale * ScaleAmount, ScaleDuration, _scaleCts.Token);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_image.gameObject.activeInHierarchy)
                return;

            _scaleCts?.Cancel();
            _scaleCts = new CancellationTokenSource();
            _ = ScaleButtonAsync(_originalScale, ScaleDuration, _scaleCts.Token);
        }

        private async UniTaskVoid ScaleButtonAsync(Vector3 targetScale, float duration, CancellationToken token)
        {
            float time = 0f;
            Vector3 startScale = _image.transform.localScale;

            while (time < duration)
            {
                if (token.IsCancellationRequested) 
                    return;

                float t = time / duration;
                _image.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                time += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _image.transform.localScale = targetScale;
        }
    }
}
