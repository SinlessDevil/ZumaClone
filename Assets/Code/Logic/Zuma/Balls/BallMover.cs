using System.Collections;
using UnityEngine;

namespace Code.Logic.Zuma.Balls
{
    public class BallMover : MonoBehaviour
    {
        private Coroutine _moveCoroutine;

        public void StartMoveToDirection(Vector3 direction, float speed)
        {
            _moveCoroutine ??= StartCoroutine(MoveRoutine(direction, speed));
        }

        public void StopMove()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
        }

        private IEnumerator MoveRoutine(Vector3 direction, float speed)
        {
            while (true)
            {
                transform.position += direction * speed * Time.deltaTime;

                yield return null;
            }
        }
    }
}
