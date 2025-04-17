using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI
{
    public class Widget : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private CanvasGroup _canvasGroup;
        
        private float _moveUpDuration = 0.5f;
        private Vector3 _moveUpOffset = new(0, 0, 2);
        private float _fadeDuration = 0.5f;
        private float _scaleDuration = 0.3f;
        
        public void SetText(string text)
        {
            _text.text = text;
        }
        public void Activate(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            
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
            sequence.Append(transform.DOMove(transform.position + _moveUpOffset, _moveUpDuration).SetEase(Ease.OutQuad));
            sequence.Join(_canvasGroup.DOFade(0f, _fadeDuration).SetEase(Ease.InOutQuad));
            sequence.OnComplete(() =>
            {
                Deactivate();
            });
        }
    }
}