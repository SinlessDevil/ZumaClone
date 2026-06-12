using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Code.UI
{
    public class Widget : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _canvasGroup;

        private RectTransform _rectTransform;

        private float _moveUpDuration = 0.5f;
        private Vector2 _moveUpOffset = new(0, 80f);
        private float _fadeDuration = 0.5f;
        private float _scaleDuration = 0.3f;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void SetText(string text)
        {
            _text.text = text;
        }

        public void Activate(Vector3 worldPosition)
        {
            var screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            _rectTransform.position = screenPos;

            gameObject.SetActive(true);
        }

        public void SetColor(Color color)
        {
            _text.color = color;
        }

        public void Deactivate()
        {
            transform.localScale = Vector3.one;
            _canvasGroup.alpha = 1f;
            _text.text = string.Empty;

            gameObject.SetActive(false);
        }

        public void PlayAnimation()
        {
            Sequence sequence = DOTween.Sequence();

            transform.localScale = Vector3.zero;
            _canvasGroup.alpha = 1f;

            sequence.Append(transform.DOScale(Vector3.one, _scaleDuration).SetEase(Ease.OutBack));
            sequence.Append(_rectTransform.DOAnchorPos(_rectTransform.anchoredPosition + _moveUpOffset, _moveUpDuration).SetEase(Ease.OutQuad));
            sequence.Join(_canvasGroup.DOFade(0f, _fadeDuration).SetEase(Ease.InOutQuad));
            sequence.OnComplete(() =>
            {
                Deactivate();
            });
        }
    }
}