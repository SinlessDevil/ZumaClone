using System.Collections.Generic;
using Code.Logic.Zuma.Balls;

namespace Code.Services.BallController
{
    public class ChainTracker
    {
        private float _distanceTravelled = 0;
        private List<Ball> _balls = new();
        
        public float DistanceTravelled => _distanceTravelled;

        public List<Ball> Balls => _balls;
        
        public void AddDistanceTravelled(float distance) => _distanceTravelled += distance;
        
        public void SubtractDistanceTravelled(float distance) =>_distanceTravelled -= distance;
        
        public void ResetDistanceTravelled() => _distanceTravelled = 0;

        public void AddBall(Ball ball) => _balls.Add(ball);
        
        public void RemoveBall(Ball ball) => _balls.Remove(ball);
        
        public void ClearBalls() => _balls.Clear();
        
        public void InsertBall(int index, Ball ball) => _balls.Insert(index, ball);
    }
}