using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.Services.BallController;
using Code.Services.Providers.Balls;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Code.Logic.Zuma.Balls
{
    public class BallIntersectionTracker : MonoBehaviour
    {
        [SerializeField] private float thresholdToPoint = 0.35f;
        [SerializeField] private float bufferOutOfScreen = 0.1f;

        private Ball _ball;
        private CancellationTokenSource _cts;
        
        private IBallProvider _ballProvider;
        private IBallChainController _ballChainController;

        [Inject]
        private void Constructor(
            IBallProvider ballProvider,
            IBallChainController ballChainController)
        {
            _ballProvider = ballProvider;
            _ballChainController = ballChainController;
        }

        public void Initialize(Ball ball)
        {
            _ball = ball;
        }

        public void StartTracker(List<Vector3> intersectionPoints)
        {
            StopTracker();
            _cts = new CancellationTokenSource();
            TrackIntersectionAsync(intersectionPoints, _cts.Token).Forget();
        }

        public void StopTracker()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTaskVoid TrackIntersectionAsync(List<Vector3> points, CancellationToken token)
        {
            if (points == null || points.Count == 0)
            {
                Debug.LogError("No intersection points found.");

                await WaitUntilOutOfScreen(token);
                _ballProvider.ReturnBall(_ball);
                return;
            }

            var sortedPoints = GetSortedPoints(points);

            foreach (var targetPoint in sortedPoints)
            {
                await UniTask.WaitUntil(() =>
                    Vector3.Distance(transform.position, targetPoint) <= thresholdToPoint ||
                    IsOutOfScreen(), cancellationToken: token);

                if (IsOutOfScreen())
                {
                    HandleOutOfScreen();
                    return;
                }

                _ballChainController.TryAttachBall(_ball);
            }
            
            await WaitUntilOutOfScreen(token);
            HandleOutOfScreen();
        }

        private List<Vector3> GetSortedPoints(List<Vector3> points)
        {
            return points
                .OrderBy(p => Vector3.Distance(transform.position, p))
                .ToList();
        }

        private async UniTask WaitUntilOutOfScreen(CancellationToken token)
        {
            await UniTask.WaitUntil(IsOutOfScreen, cancellationToken: token);
        }

        private void HandleOutOfScreen()
        {
            _ball.SetInteractive(false);
            _ballProvider.ReturnBall(_ball);
        }

        private bool IsOutOfScreen()
        {
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
            return screenPoint.x < -bufferOutOfScreen || screenPoint.x > 1 + bufferOutOfScreen ||
                   screenPoint.y < -bufferOutOfScreen || screenPoint.y > 1 + bufferOutOfScreen;
        }
    }
}
