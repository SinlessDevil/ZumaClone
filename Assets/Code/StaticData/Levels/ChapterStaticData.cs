using System.Collections.Generic;
using UnityEngine;

namespace Code.StaticData.Levels
{
    [CreateAssetMenu(fileName = "ChapterStaticData", menuName = "StaticData/Chapter", order = 0)]
    public class ChapterStaticData : ScriptableObject
    {
        public int ChapterId;
        public int TempleId = 1;
        public LevelThemeStaticData Theme;
        public List<LevelStaticData> Levels = new();
        [HideInInspector] public List<int> RetiredLevelIds = new();
    }
}