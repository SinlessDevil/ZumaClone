using System.Collections.Generic;
using System.Threading;
using Code.Logic;
using Code.Logic.Zuma;
using Code.Logic.Zuma.Balls;
using Cysharp.Threading.Tasks;
using Code.PathCreation;
using UnityEngine;

namespace Code.Services.BallController
{
    public interface IBallChainController
    {
        public List<Item> ActiveItems { get; }
        public CancellationToken AttemptToken { get; }
        public bool HasAccuracy { get; }
        public bool IsOutsideField(Vector3 position);
        public Vector3 AccuracyEnd(Vector3 start, Vector3 end);
        public void Initialize(PathCreator pathCreator, BallChainDTO ballChainDTO);
        public void Update();
        public void StartBallSpawning(List<Color> colors);
        public void StopBallSpawning();
        public void RegisterShot(Ball ball);
        public void ResolveMiss(Ball ball);
        public void TryAttachBall(Ball newBall);
        public bool TryAttachBall(Ball newBall, Vector3 start, Vector3 end);
        public UniTask MoveParticleAlongPathAsync(ParticleSystemHolder particle);
    }
}