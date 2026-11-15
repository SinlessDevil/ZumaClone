using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.StaticData.Levels
{
    [CreateAssetMenu(fileName = "LevelRandomConfigStaticData", menuName = "StaticData/LevelRandomConfig", order = 0)]
    public class LevelRandomConfigStaticData : ScriptableObject
    {
        public const float DefaultRepeatColorPercent = 41f;
        [Range(0f, 100f), Tooltip("Chance to repeat the previous color in the chain before choosing a different color.")]
        public float RepeatColorPercent = DefaultRepeatColorPercent;
        public List<ItemConfig> Items = new List<ItemConfig>();
        public List<ItemProbability> ItemProbabilities = new List<ItemProbability>();
    }
    
    [Serializable]
    public class ItemProbability
    {
        public ItemConfig Item;
        [Range(0, 100)] public float Probability;
    }
}