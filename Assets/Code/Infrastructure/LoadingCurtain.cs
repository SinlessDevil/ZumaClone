using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Infrastructure
{
    public class LoadingCurtain : MonoBehaviour, ILoadingCurtain
    {
        private const float Delay = 1.75f;
        private const float AnimationDuration = 0.65f;
        
        [SerializeField] private RectTransform _right;
        [SerializeField] private RectTransform _left;
        [SerializeField] private Text _loadingText;

        private Coroutine _loadingTextCoroutine;

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
            StartCoroutine(AnimationOpen());
            StartLoadingTextAnimation();
        }

        private IEnumerator AnimationOpen()
        {
            yield return new WaitForSeconds(Delay);
            
            float screenWidth = Screen.width;
            float elapsedTime = 0f;

            Vector2 leftStart = _left.anchoredPosition;
            Vector2 leftTarget = new Vector2(-screenWidth / 2, leftStart.y);
            
            Vector2 rightStart = _right.anchoredPosition;
            Vector2 rightTarget = new Vector2(screenWidth / 2, rightStart.y);

            while (elapsedTime < AnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / AnimationDuration;
                _left.anchoredPosition = Vector2.Lerp(leftStart, leftTarget, t);
                _right.anchoredPosition = Vector2.Lerp(rightStart, rightTarget, t);
                yield return null;
            }

            _left.anchoredPosition = leftTarget;
            _right.anchoredPosition = rightTarget;
            
            StopLoadingTextAnimation();
            gameObject.SetActive(false);
            IsActive = false;
        }

        private void StartLoadingTextAnimation()
        {
            _loadingTextCoroutine = StartCoroutine(LoadingTextEffect());
        }

        private void StopLoadingTextAnimation()
        {
            if (_loadingTextCoroutine != null)
                StopCoroutine(_loadingTextCoroutine);
        }

        private IEnumerator LoadingTextEffect()
        {
            string baseText = "Loading";
            while (true)
            {
                _loadingText.text = baseText + "";
                yield return new WaitForSeconds(0.15f);
                _loadingText.text = baseText + ".";
                yield return new WaitForSeconds(0.15f);
                _loadingText.text = baseText + "..";
                yield return new WaitForSeconds(0.15f);
                _loadingText.text = baseText + "...";
                yield return new WaitForSeconds(0.15f);
            }
        }
    }
}
