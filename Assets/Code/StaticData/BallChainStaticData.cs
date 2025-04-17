using UnityEngine;

namespace Code.StaticData
{
    [CreateAssetMenu(menuName = "StaticData/BallChainConfg", fileName = "BallChainConfig", order = 10)]
    public class BallChainStaticData : ScriptableObject
    {
        [Space(10)] [Header("Chain Settings")]
        public float DurationSpawnBall = 0.05f;
        public float MoveSpeed = 0.25f;
        public float SpacingBalls = 0.35f;
        public float DurationMovingOffset = 0.2f;
        [Space(10)] [Header("Attach Balls Settings")]
        public float CollisionThreshold = 1f;
        public int MatchingCount = 3;
        [Space(10)] [Header("Boost Settings")]
        public float InitialSpeedMultiplier = 2.5f;
        public float BoostDuration = 3f;
        [Space(10)] [Header("Particles Settings")]
        public float MinParticleSpeed = 35f;
        public float MaxParticleSpeed = 70f;
        public float DurationHideParticles = 0.4f;
        [Space(10)] [Header("Widgets Settings")]
        public Color BaseColorWidget = Color.yellow;
        public float SetToSpawnWidget = 0.5f;
        public int TimeToSpawnWidget = 100; // 200ms
        [Space(10)] [Header("Lose Settings")] 
        [Range(1,100)] public int PercentToDetectionLose = 20;
        public float BoostSpeedBallForLose = 4f;
    }
}