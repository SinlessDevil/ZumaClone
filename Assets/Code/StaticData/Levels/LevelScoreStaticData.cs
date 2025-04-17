using System;
using UnityEngine;

namespace Code.StaticData.Levels
{
    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "StaticData/ScoreConfig", order = 1)]
    public class LevelScoreStaticData : ScriptableObject
    {
        public int ScorePerItem = 10;
        public int ScorePerStepPath = 100;
        public int ScorePerCoin = 300;
        public MultiplierScore MultiplierScore;
    }

    [Serializable]
    public class MultiplierScore
    {
        public int Multiplier = 2;
        public string MultiplierName = "Combo x";
    }
}