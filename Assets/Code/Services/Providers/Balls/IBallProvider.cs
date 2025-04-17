using Code.Logic.Zuma.Balls;
using UnityEngine;

namespace Code.Services.Providers.Balls
{
    public interface IBallProvider
    {
        public void CreatePoolBall();
        public void CleanupPool();
        public Ball GetBall(Vector3 position, Quaternion rotation);
        public void ReturnBall(Ball ball);
    }   
}