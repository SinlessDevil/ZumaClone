using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma.Balls;
using Code.Services.Factories.Game;
using Code.Services.StaticData;
using UnityEngine;

namespace Code.Services.Providers.Balls
{
    public class BallProvider : IBallProvider
    {
        private const int InitialCount = 10;
        
        private List<Ball> _pool;
        
        private readonly IGameFactory _gameFactory;
        private readonly IStaticDataService _staticData;

        public BallProvider(
            IGameFactory gameFactory, 
            IStaticDataService staticData)
        {
            _gameFactory = gameFactory;
            _staticData = staticData;
        }
        
        public void CreatePoolBall()
        {
            _pool = new List<Ball>();

            for (int i = 0; i < InitialCount; i++)
            {
                var ball = CreateObject(Vector3.zero, Quaternion.identity);
                ball.Deactivate();
            }
        }
        
        public void CleanupPool()
        {
            foreach (var ball in _pool.Where(ball => ball != null))
            {
                ball.Deactivate();
                Object.Destroy(ball.gameObject);
            }

            _pool.Clear();
        }

        public Ball GetBall(Vector3 position, Quaternion rotation)
        {
            foreach (var ball in _pool.Where(ball => !ball.gameObject.activeSelf))
            {
                ball.Activate(position, rotation);
                return ball;
            }

            Ball newBall = CreateObject(position, rotation);
            newBall.Activate(position, rotation);
            return newBall;
        }

        public void ReturnBall(Ball ball)
        {
            ball.Deactivate();
        }
        
        private Ball CreateObject(Vector3 position, Quaternion rotation)
        {
            Ball createdObject = _gameFactory.CreateBall(position, rotation);
            createdObject.Initialize(_staticData.BallConfig);
            _pool.Add(createdObject);
            return createdObject;
        }
    }
}