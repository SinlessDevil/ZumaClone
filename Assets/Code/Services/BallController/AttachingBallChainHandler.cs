using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma.Balls;
using Code.Services.Levels;
using Code.Services.LocalProgress;
using Cysharp.Threading.Tasks;
using PathCreation;
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
            var path = _pathCreator.path;
            float minDistance = float.MaxValue;
            CollisionData closest = null;

            for (int i = 0; i < _chainTracker.Balls.Count; i++)
            {
                Ball existingBall = _chainTracker.Balls[i];
                float distance = Vector3.Distance(existingBall.transform.position, newBall.transform.position);

                if (distance <= _ballChainDto.CollisionThreshold && distance < minDistance)
                {
                    closest = new CollisionData
                    {
                        Ball = existingBall,
                        Distance = distance,
                        Index = i
                    };
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
            {
                AttachBallToChain(newBall, _chainTracker.Balls.Count);
            }
            else if (collision.Index == 0 && newBallDist > closestBallDist)
            {
                AttachBallToChain(newBall, 0);
            }
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
            CheckAndDestroyMatches(insertedBall, _chainTracker.Balls);
        }
        
        private void CheckAndDestroyMatches(Ball insertedBall, List<Ball> balls)
        {
            List<Ball> matchingBalls = new List<Ball> { insertedBall };
            Color matchColor = insertedBall.Color;

            for (int i = insertedBall.Index - 1; i >= 0; i--)
            {
                if (balls[i].Color == matchColor)
                    matchingBalls.Add(balls[i]);
                else
                    break;
            }

            for (int i = insertedBall.Index + 1; i < balls.Count; i++)
            {
                if (balls[i].Color == matchColor)
                    matchingBalls.Add(balls[i]);
                else
                    break;
            }

            if (matchingBalls.Count >= _ballChainDto.MatchingCount)
            {
                int count = matchingBalls.Count * _levelService.GetCurrentLevelStaticData().LevelConfig.ScoreConfig.ScorePerItem;
                _levelLocalProgressService.AddScore(count);
                
                _widgetBallChainProvider.SetUpWidget(matchingBalls, insertedBall, count);
                _winBallChainHandler.TryWin(balls.Count - matchingBalls.Count);
                
                PlayDestroyMatchingBalls(insertedBall, balls, matchingBalls);
                
                ReIndexBalls();
            }
            else
            {
                insertedBall.SetInteractive(false);
            }
        }

        private void PlayDestroyMatchingBalls(Ball insertedBall, List<Ball> balls, List<Ball> matchingBalls)
        {
            foreach (var ball in matchingBalls)
            {
                ball.PlayDestroyAnimation(() =>
                {
                    balls.Remove(ball);
                    ball.Deactivate();
                 
                    insertedBall.SetInteractive(false);
                    
                    _chainTracker.SubtractDistanceTravelled(_ballChainDto.SpacingBalls);
                });
            }
        }

        private void ReIndexBalls()
        {
            for (int i = 0; i < _chainTracker.Balls.Count; i++)
            {
                _chainTracker.Balls[i].SetIndex(i);
            }
        }
        
        private class CollisionData
        {
            public Ball Ball;
            public float Distance;
            public int Index;
        }
    }
}