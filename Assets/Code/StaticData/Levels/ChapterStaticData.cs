using System.Collections.Generic;
using UnityEngine;

namespace Code.StaticData.Levels
{
    [CreateAssetMenu(fileName = "ChapterStaticData", menuName = "StaticData/Chapter", order = 0)]
    public class ChapterStaticData : ScriptableObject
    {
        public int ChapterId;
        public List<LevelStaticData> Levels = new();
    }
}