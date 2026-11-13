using System;
using UnityEngine;

namespace Code.StaticData.Levels
{
    [CreateAssetMenu(fileName = "LevelStaticData", menuName = "StaticData/Level", order = 0)]
    public class LevelStaticData : ScriptableObject
    {
        public string LevelName;
        public int LevelId;
        public LevelTypeId LevelTypeId;
        public GameObject LevelHolder;
        public LevelConfig LevelConfig = new LevelConfig();
    }

    [Serializable]
    public enum LevelTypeId
    {
        Regular = 0, 
        Special = 1,
        Bonus = 2,
    }
    
    [Serializable]
    public class LevelConfig
    {
        public int CountItem;
        [Min(0), Tooltip("Score required to stop the feed. Zero uses a value based on CountItem.")]
        public int ZumaTargetScore;
        [Min(0f)] public float AceTime;
        [Min(0)] public int PowerupEvery = 12;
        [Min(0f)] public float PowerupLifetime = 6f;
        [Min(0f)] public float PowerupDuration = 4f;
        [Min(0f)] public float ExplosionRadius = 1.4f;
        [Min(0f)] public float CoinInterval = 8f;
        [Min(0f)] public float CoinLifetime = 4f;
        [Min(0f), Tooltip("World units per second. Zero uses the shared ball chain speed.")]
        public float BallMoveSpeedOverride;
        public LevelRandomConfigStaticData RandomConfig;
        public LevelScoreStaticData ScoreConfig;

        public int GetZumaTarget() => ZumaTargetScore > 0
            ? ZumaTargetScore
            : Mathf.Max(100, CountItem * ScoreConfig.ScorePerItem);
    }
}