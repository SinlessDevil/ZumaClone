using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Code.Services.PersistenceProgress.Player
{
    [Serializable]
    public class PlayerLevelData
    {
        public LevelContainer LastProgress = new LevelContainer();
        public LevelContainer CurrentProgress = new LevelContainer();
        public List<LevelContainer> LevelsComleted = new List<LevelContainer>();
    }
    
    [Serializable]
    public class LevelContainer
    {
        public int ChapterId = 1;
        public int LevelId = 1;
        [FormerlySerializedAs("Timer")] public float Time = 0f;
    }
}