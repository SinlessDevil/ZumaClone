using UnityEngine;

namespace Code.StaticData.Levels
{
    [CreateAssetMenu(fileName = "LevelTheme", menuName = "StaticData/Level Theme")]
    public sealed class LevelThemeStaticData : ScriptableObject
    {
        public string DisplayName;
        public Material Ground;
        public Material Track;
        [Min(0.1f)] public float TrackRepeatLength = 3f;
    }
}
