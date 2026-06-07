using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma.Balls;
using Code.Services.Levels;
using Code.Services.LocalProgress;
using Cysharp.Threading.Tasks;
using Code.PathCreation;
using UnityEngine;

namespace Code.Services.BallController
{
    public class AttachingBallChainHandler
    {
        private readonly PathCreator _pathCreator;
        private readonly BallChainDTO _ballChainDto;
        private readonly ChainTracker _chainTracker;
        private readonly WidgetBallChainProvider _widgetBallChainProvider;
        private readonly WinBallChainHandler _winBallChainHandler;
        private readonly ILevelService _levelService;
        private readonly ILevelLocalProgressService _levelLocalProgressService;

        public AttachingBallChainHandler(
            PathCreator pathCreator,
            BallChainDTO ballChainDto,
            ChainTracker chainTracker,
            WidgetBallChainProvider widgetBallChainProvider,
            WinBallChainHandler winBallChainHandler,
            ILevelService levelService,
            ILevelLocalProgressService levelLocalProgressService)
        {
            _pathCreator = pathCreator;
            _ballChainDto = ballChainDto;
            _chainTracker = chainTracker;
            _widgetBallChainProvider = widgetBallChainProvider;
            _winBallChainHandler = winBallChainHandler;
            _levelService = levelService;
            _levelLocalProgressService = levelLocalProgressService;
        }

        public void TryAttachBall(Ball newBall)
        {
            var collision = GetClosestCollision(newBall);
            if (collision == null)
                return;

            InsertBallToChainByDistance(newBall, collision);
        }

        private CollisionData GetClosestCollision(Ball newBall)
        {
            float minDistance = float.MaxValue;
            CollisionData closest = null;

            for (int i = 0; i < _chainTracker.Balls.Count; i++)
            {
                Ball existingBall = _chainTracker.Balls[i];
                float distance = Vector3.Distance(existingBall.transform.position, newBall.transform.position);

                if (distance <= _ballChainDto.CollisionThreshold && distance < minDistance)
                {
                    closest = new CollisionData { Ball = existingBall, Distance = distance, Index = i };
                    minDistance = distance;
                }
            }

            return closest;
        }

        private void InsertBallToChainByDistance(Ball newBall, CollisionData collision)
        {
            var path = _pathCreator.path;

            float newBallDist = path.GetClosestDistanceAlongPath(newBall.transform.position);
            float closestBallDist = path.GetClosestDistanceAlongPath(collision.Ball.transform.position);

            if (collision.Index == _chainTracker.Balls.Count - 1 && newBallDist < closestBallDist)
                AttachBallToChain(newBall, _chainTracker.Balls.Count);
            else if (collision.Index == 0 && newBallDist > closestBallDist)
                AttachBallToChain(newBall, 0);
            else
            {
                int insertIndex = (newBallDist > closestBallDist) ? collision.Index : collision.Index + 1;
                AttachBallToChain(newBall, insertIndex);
            }
        }

        private void AttachBallToChain(Ball newBall, int index)
        {
            newBall.Dispose();

            _chainTracker.InsertBall(index, newBall);
            _chainTracker.AddDistanceTravelled(_ballChainDto.SpacingBalls);

            ReIndexBalls();

            WaitToCheckAndDestroyMatches(newBall).Forget();
        }

        private async UniTask WaitToCheckAndDestroyMatches(Ball insertedBall)
        {
            await UniTask.Delay((int)(_ballChainDto.DurationMovingOffset * 1000));
            await CheckAndDestroyMatches(insertedBall, _chainTracker.Balls);
        }

        private async UniTask CheckAndDestroyMatches(Ball pivotBall, List<Ball> balls)
        {
            var matchingBalls = FindMatchesAroundBall(pivotBall, balls);

            if (matchingBalls.Count >= _ballChainDto.MatchingCount)
            {
                int score = matchingBalls.Count * _levelService.GetCurrentLevelStaticData().LevelConfig.ScoreConfig.ScorePerItem;
                _levelLocalProgressService.AddScore(score);
                _widgetBallChainProvider.SetUpWidget(matchingBalls, pivotBall, score);
                _winBallChainHandler.TryWin(balls.Count - matchingBalls.Count);

                int junctionIndex = matchingBalls.Min(b => b.Index) - 1;

                await DestroyBalls(matchingBalls, balls);
                await CheckChainReaction(junctionIndex, balls);
            }
            else
            {
                pivotBall.SetInteractive(false);
            }
        }

        // Recursively checks for new matches after a group is destroyed.
        // junctionIndex points to the ball just before the destroyed group.
        private async UniTask CheckChainReaction(int junctionIndex, List<Ball> balls)
        {
            if (balls.Count == 0 || _winBallChainHandler.IsWin)
                return;

            await UniTask.Delay((int)(_ballChainDto.DurationMovingOffset * 1000));

            junctionIndex = Mathf.Clamp(junctionIndex, 0, balls.Count - 1);
            Ball anchor = balls[junctionIndex];
            var matchingBalls = FindMatchesAroundBall(anchor, balls);

            if (matchingBalls.Count >= _ballChainDto.MatchingCount)
            {
                int score = matchingBalls.Count * _levelService.GetCurrentLevelStaticData().LevelConfig.ScoreConfig.ScorePerItem;
                _levelLocalProgressService.AddScore(score);
                _widgetBallChainProvider.SetUpWidget(matchingBalls, anchor, score);
                _winBallChainHandler.TryWin(balls.Count - matchingBalls.Count);

                int nextJunctionIndex = matchingBalls.Min(b => b.Index) - 1;

                await DestroyBalls(matchingBalls, balls);
                await CheckChainReaction(nextJunctionIndex, balls);
            }
        }

        // Waits for ALL destroy animations to finish, then removes balls and contracts the chain at once.
        private async UniTask DestroyBalls(List<Ball> matchingBalls, List<Ball> allBalls)
        {
            await UniTask.WhenAll(matchingBalls.Select(WaitForDestroyAnimation));

            foreach (var ball in matchingBalls)
            {
                allBalls.Remove(ball);
                ball.Deactivate();
            }

            _chainTracker.SubtractDistanceTravelled(matchingBalls.Count * _ballChainDto.SpacingBalls);
            ReIndexBalls();
        }

        private UniTask WaitForDestroyAnimation(Ball ball)
        {
            var tcs = new UniTaskCompletionSource();
            ball.PlayDestroyAnimation(() => tcs.TrySetResult());
            return tcs.Task;
        }

        private List<Ball> FindMatchesAroundBall(Ball pivotBall, List<Ball> balls)
        {
            var matching = new List<Ball> { pivotBall };
            Color color = pivotBall.Color;

            for (int i = pivotBall.Index - 1; i >= 0; i--)
            {
                if (balls[i].Color == color) matching.Add(balls[i]);
                else break;
            }

            for (int i = pivotBall.Index + 1; i < balls.Count; i++)
            {
                if (balls[i].Color == color) matching.Add(balls[i]);
                else break;
            }

            return matching;
        }

        private void ReIndexBalls()
        {
            for (int i = 0; i < _chainTracker.Balls.Count; i++)
                _chainTracker.Balls[i].SetIndex(i);
        }

        private class CollisionData
        {
            public Ball Ball;
            public float Distance;
            public int Index;
        }
    }
}
