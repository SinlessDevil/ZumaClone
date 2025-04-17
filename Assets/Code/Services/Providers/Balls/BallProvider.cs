using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma.Balls;
using Code.Services.Factories.Game;
using UnityEngine;

namespace Code.Services.Providers.Balls
{
    public class BallProvider : IBallProvider
    {
        private const int InitialCount = 10;
        
        private List<Ball> _pool;
        
        private readonly IGameFactory _gameFactory;

        public BallProvider(IGameFactory gameFactory)
        {
            _gameFactory = gameFactory;
        }
        
        public void CreatePoolBall()
        {
            _pool = new List<Ball>();

            for (int i = 0; i < InitialCount; i++)
            {
                var ball = CreateObject(Vector3.zero, Quaternion.identity);
                ball.Initialize();
                ball.Deactivate();
            }
        }
        
        public void CleanupPool()
        {
            foreach (var ball in _pool.Where(ball => ball != null))
            {
                Object.Destroy(ball.gameObject);
            }

            _pool.Clear();
        }

        public Ball GetBall(Vector3 position, Quaternion rotation)
        {
            foreach (var ball in _pool)
            {
                if (!ball.gameObject.activeInHierarchy)
                {
                    ball.Activate(position, rotation);
                    return ball;
                }
            }

            Ball newBall = CreateObject(position, rotation);
            newBall.Initialize();
            newBall.Activate(position, rotation);
            return newBall;
        }

        public void ReturnBall(Ball ball)
        {
            ball.Deactivate();
        }
        
        private Ball CreateObject(Vector3 position, Quaternion rotation)
        {
            var createdObject = _gameFactory.CreateBall(position, rotation);
            _pool.Add(createdObject);
            return createdObject;
        }
    }
}