using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Code.Extensions
{
    public static class DOTweenTMPExtensions
    {
        public static Tweener DOTextCounter(this TMP_Text target, int fromValue, int toValue, float duration, bool addThousandsSeparator = false)
        {
            int currentValue = fromValue;
            return DOTween.To(() => currentValue, x =>
            {
                currentValue = x;
                target.text = addThousandsSeparator
                    ? currentValue.ToString("N0")
                    : currentValue.ToString();
            }, toValue, duration);
        }
        
        public static Tweener DOTimeCounter(this TMP_Text target, float fromSeconds, float toSeconds, float duration)
        {
            float current = fromSeconds;
            return DOTween.To(() => current, x =>
            {
                current = x;
                int minutes = Mathf.FloorToInt(current / 60f);
                int seconds = Mathf.FloorToInt(current % 60f);
                target.text = $"{minutes:00}:{seconds:00}";
            }, toSeconds, duration);
        }
    }   
}