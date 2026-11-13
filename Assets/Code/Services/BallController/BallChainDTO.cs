using UnityEngine;

namespace Code.Services.BallController
{
    public class BallChainDTO
    {
        public int ZumaTargetScore;
        public float MeterReverseDuration = 0.6f;
        public float AceTime;
        public bool Survival;
        public float DurationSpawnBall;
        public float MoveSpeed;
        public float SpacingBalls;
        public float DurationMovingOffset;
        public float ChainSpringStrength;
        public float ChainGapSpringStrength;
        public float CatchupSpeedMultiplier;

        public float CollisionThreshold;
        public int MatchingCount;

        public float InitialSpeedMultiplier;
        public float BoostDuration;
        
        public float MinParticleSpeed;
        public float MaxParticleSpeed;

        public Color BaseColorWidget;
        public float SetToSpawnWidget;
        public int TimeToSpawnWidget;
        
        public int PercentToDetectionLose;
        public float BoostSpeedBallForLose;
    }
}