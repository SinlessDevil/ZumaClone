using UnityEngine;

namespace Code.Logic.Zuma.Balls
{
    public class BallMover : MonoBehaviour
    {
        private BallIntersectionTracker _tracker;
        private Vector3 _velocity;

        public void StartMoveToDirection(Vector3 direction, float speed)
        {
            _tracker ??= GetComponent<BallIntersectionTracker>();
            _velocity = direction * speed;
        }

        public void StopMove() => _velocity = Vector3.zero;

        public bool Step(float deltaTime)
        {
            if (_velocity == Vector3.zero)
                return true;

            Vector3 start = transform.position;
            Vector3 end = start + _velocity * deltaTime;
            if (_tracker.ResolveStep(start, end))
                return true;

            transform.position = end;
            return false;
        }
    }
}
