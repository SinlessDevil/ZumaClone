using System.Collections.Generic;
using Code.Logic.Zuma.Balls;

namespace Code.Services.BallController
{
    public class ChainTracker
    {
        private float _distanceTravelled = 0;
        private List<Ball> _balls = new();
        private List<float> _pathDistances = new();
        private List<float> _velocities = new();

        public float DistanceTravelled => _distanceTravelled;
        public List<Ball> Balls => _balls;

        public void AddDistanceTravelled(float distance) => _distanceTravelled += distance;
        public void SubtractDistanceTravelled(float distance) => _distanceTravelled -= distance;
        public void ResetDistanceTravelled() => _distanceTravelled = 0;
        public void SetDistanceTravelled(float distance) => _distanceTravelled = distance;

        public float GetPathDistance(int i) => _pathDistances[i];
        public void SetPathDistance(int i, float dist) => _pathDistances[i] = dist;

        public float GetVelocity(int i) => _velocities[i];
        public void SetVelocity(int i, float vel) => _velocities[i] = vel;

        public void AddBall(Ball ball)
        {
            _balls.Add(ball);
            _pathDistances.Add(0f);
            _velocities.Add(0f);
        }

        public void InsertBall(int index, Ball ball, float initialPathDist = 0f)
        {
            _balls.Insert(index, ball);
            _pathDistances.Insert(index, initialPathDist);
            _velocities.Insert(index, 0f);
        }

        public void RemoveBall(Ball ball)
        {
            int i = _balls.IndexOf(ball);
            if (i < 0) return;
            _balls.RemoveAt(i);
            _pathDistances.RemoveAt(i);
            _velocities.RemoveAt(i);
        }

        public void ClearBalls()
        {
            _balls.Clear();
            _pathDistances.Clear();
            _velocities.Clear();
        }
    }
}
