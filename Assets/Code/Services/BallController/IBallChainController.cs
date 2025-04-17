using System.Collections.Generic;
using Code.Logic;
using Code.Logic.Zuma;
using Code.Logic.Zuma.Balls;
using Cysharp.Threading.Tasks;
using PathCreation;

namespace Code.Services.BallController
{
    public interface IBallChainController
    {
        public List<Item> ActiveItems { get; }
        public void Initialize(PathCreator pathCreator, BallChainDTO ballChainDTO);
        public void Update();
        public void StartBallSpawning();
        public void StopBallSpawning();
        public void TryAttachBall(Ball newBall);
        public UniTask MoveParticleAlongPathAsync(ParticleSystemHolder particle);
    }
}