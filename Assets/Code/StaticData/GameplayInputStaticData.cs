using UnityEngine;

namespace Code.StaticData
{
    [CreateAssetMenu(menuName = "StaticData/Gameplay Input", fileName = "GameplayInputConfig")]
    public sealed class GameplayInputStaticData : ScriptableObject
    {
        [Tooltip("Gameplay input bounds from 0 to 1, measured from the bottom-left of the screen.")]
        public Rect GameplayInputArea = new(0f, 0f, 1f, 0.875f);

        public Rect GetGameplayInputArea(Rect screen, Rect safeArea)
        {
            Rect area = ClampGameplayInputArea(GameplayInputArea);
            float left = Mathf.Max(screen.xMin + area.xMin * screen.width, safeArea.xMin);
            float bottom = Mathf.Max(screen.yMin + area.yMin * screen.height, safeArea.yMin);
            float right = Mathf.Min(screen.xMin + area.xMax * screen.width, safeArea.xMax);
            float top = Mathf.Min(screen.yMin + area.yMax * screen.height, safeArea.yMax);
            return new Rect(left, bottom, Mathf.Max(0f, right - left), Mathf.Max(0f, top - bottom));
        }

        public static Rect ClampGameplayInputArea(Rect area)
        {
            float width = Mathf.Clamp01(area.width);
            float height = Mathf.Clamp01(area.height);
            return new Rect(Mathf.Clamp(area.x, 0f, 1f - width), Mathf.Clamp(area.y, 0f, 1f - height), width, height);
        }

        private void OnValidate() => GameplayInputArea = ClampGameplayInputArea(GameplayInputArea);
    }
}
