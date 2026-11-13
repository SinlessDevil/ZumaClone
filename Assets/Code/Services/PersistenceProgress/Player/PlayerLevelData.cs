using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Code.Services.PersistenceProgress.Player
{
    [Serializable]
    public class PlayerLevelData
    {
        public int DeluxeProgressVersion;
        public bool AdventureComplete;
        public int AdventureLives = 3;
        public int AdventureScore;
        public LevelContainer StageCheckpoint = new LevelContainer();
        public List<LevelContainer> VisitedLevels = new();
        public List<GauntletRecord> GauntletRecords = new();
        public LevelContainer LastProgress = new LevelContainer();
        public LevelContainer CurrentProgress = new LevelContainer();
        public List<LevelContainer> LevelsComleted = new List<LevelContainer>();
    }
    
    [Serializable]
    public class GauntletRecord
    {
        public int ChapterId;
        public int LevelId;
        public int Mode;
        public int Score;
        public string Rank;
        public bool SunGem;
    }

    [Serializable]
    public class LevelContainer
    {
        public int ChapterId = 1;
        public int LevelId = 1;
        [FormerlySerializedAs("Timer")] public float Time = 0f;
    }
}