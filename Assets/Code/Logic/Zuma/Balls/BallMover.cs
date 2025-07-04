using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Code.Logic.Zuma.Balls
{
    public class BallMover : MonoBehaviour
    {
        private CancellationTokenSource _cts;

        public void StartMoveToDirection(Vector3 direction, float speed)
        {
            if (_cts != null) 
                return;

            _cts = new CancellationTokenSource();
            MoveAsync(direction, speed, _cts.Token).Forget();
        }

        public void StopMove()
        {
            if (_cts == null) 
                return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private async UniTaskVoid MoveAsync(Vector3 direction, float speed, 
            CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                transform.position += direction * speed * Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }
}