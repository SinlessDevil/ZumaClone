using System.Collections.Generic;
using Code.Logic.Zuma.Balls;
using Code.PathCreation;
using UnityEngine;

namespace Code.Services.BallController
{
    public class ChainSegment
    {
        private readonly PathCreator _pathCreator;
        private readonly float _spacing;
        private readonly float _springStrength;
        private readonly float _gapSpringStrength;

        private readonly List<Ball> _balls = new();
        private readonly List<float> _visualDistances = new();
        private readonly List<float> _velocities = new();

        public float HeadLogicalDistance { get; private set; }
        public float TailLogicalDistance => HeadLogicalDistance - (_balls.Count - 1) * _spacing;
        public float BaseSpeed { get; set; }
        public float CurrentSpeed { get; set; }
        public bool IsCatchingUp { get; set; }
        public IReadOnlyList<Ball> Balls => _balls;
        public int Count => _balls.Count;

        public ChainSegment(PathCreator pathCreator, float spacing, float speed,
            float springStrength, float gapSpringStrength)
        {
            _pathCreator = pathCreator;
            _spacing = spacing;
            BaseSpeed = speed;
            CurrentSpeed = speed;
            _springStrength = springStrength;
            _gapSpringStrength = gapSpringStrength;
        }

        public void Update(float deltaTime)
        {
            if (_balls.Count == 0) return;

            HeadLogicalDistance += CurrentSpeed * deltaTime;

            var path = _pathCreator.path;

            for (int i = 0; i < _balls.Count; i++)
            {
                float logicalDist = HeadLogicalDistance - i * _spacing;
                float visualDist = _visualDistances[i];
                float vel = _velocities[i];

                float gap = Mathf.Abs(logicalDist - visualDist);
                float smoothTime = gap > _spacing * 1.5f
                    ? 1f / _gapSpringStrength
                    : 1f / _springStrength;

                float newVisual = Mathf.SmoothDamp(visualDist, logicalDist, ref vel, smoothTime);
                _visualDistances[i] = newVisual;
                _velocities[i] = vel;

                _balls[i].transform.position = path.GetPointAtDistance(
                    Mathf.Max(newVisual, 0f), EndOfPathInstruction.Stop);
            }
        }

        public Ball GetBall(int index) => _balls[index];
        public float GetVisualDistance(int index) => _visualDistances[index];

        public void SetHeadLogicalDistance(float dist) => HeadLogicalDistance = dist;

        public void AddBall(Ball ball, float initialDist)
        {
            _balls.Add(ball);
            _visualDistances.Add(initialDist);
            _velocities.Add(0f);
        }

        public void InsertBall(int index, Ball ball, float initialDist)
        {
            _balls.Insert(index, ball);
            _visualDistances.Insert(index, initialDist);
            _velocities.Insert(index, 0f);
        }

        public void RemoveBallAt(int index)
        {
            _balls.RemoveAt(index);
            _visualDistances.RemoveAt(index);
            _velocities.RemoveAt(index);
        }

        // Splits: balls [splitIndex..] become a new back segment, removed from this segment
        public ChainSegment SplitAt(int splitIndex)
        {
            var back = new ChainSegment(_pathCreator, _spacing, BaseSpeed, _springStrength, _gapSpringStrength);
            back.SetHeadLogicalDistance(HeadLogicalDistance - splitIndex * _spacing);

            for (int i = splitIndex; i < _balls.Count; i++)
                back.AddBall(_balls[i], _visualDistances[i]);

            for (int i = _balls.Count - 1; i >= splitIndex; i--)
            {
                _balls.RemoveAt(i);
                _visualDistances.RemoveAt(i);
                _velocities.RemoveAt(i);
            }

            return back;
        }

        // Absorbs back segment's balls (call before removing back from segments list)
        public void MergeBack(ChainSegment back)
        {
            back.SetHeadLogicalDistance(TailLogicalDistance - _spacing);

            for (int i = 0; i < back.Count; i++)
            {
                _balls.Add(back.GetBall(i));
                _visualDistances.Add(back.GetVisualDistance(i));
                _velocities.Add(0f);
            }
        }

        public void ReIndexBalls()
        {
            for (int i = 0; i < _balls.Count; i++)
                _balls[i].SetIndex(i);
        }

        public void Clear()
        {
            _balls.Clear();
            _visualDistances.Clear();
            _velocities.Clear();
            HeadLogicalDistance = 0f;
        }
    }
}
