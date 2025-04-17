using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma.Balls;
using Code.Services.Levels;
using Code.Services.LocalProgress;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Services.BallController
{
    public class AttachingBallChainHandler
    {
        private readonly BallChainDTO _ballChainDto;
        private readonly ChainTracker _chainTracker;
        private readonly WidgetBallChainProvider _widgetBallChainProvider;
        private readonly WinBallChainHandler _winBallChainHandler;
        private readonly ILevelService _levelService;
        private readonly ILevelLocalProgressService _levelLocalProgressService;

        public AttachingBallChainHandler(
            BallChainDTO ballChainDto,
            ChainTracker chainTracker, 
            WidgetBallChainProvider widgetBallChainProvider, 
            WinBallChainHandler winBallChainHandler, 
            ILevelService levelService, 
            ILevelLocalProgressService levelLocalProgressService)
        {
            _ballChainDto = ballChainDto;
            _chainTracker = chainTracker;
            _widgetBallChainProvider = widgetBallChainProvider;
            _winBallChainHandler = winBallChainHandler;
            _levelService = levelService;
            _levelLocalProgressService = levelLocalProgressService;
        }

        public void TryAttachBall(Ball newBall)
        {
            List<(Ball ball, float distance, int index)> ballsCollision = new();

            for (int i = 0; i < _chainTracker.Balls.Count; i++)
            {
                Ball existingBall = _chainTracker.Balls[i];
                float distance = Vector3.Distance(existingBall.transform.position, newBall.transform.position);

                if (distance <= _ballChainDto.CollisionThreshold)
                {
                    ballsCollision.Add((existingBall, distance, i));
                }
            }

            if (ballsCollision.Count == 0)
                return;

            ballsCollision = ballsCollision.OrderBy(b => b.distance).ToList();

            Ball closestBall = ballsCollision.First().ball;
            int closestIndex = ballsCollision.First().index;

            if (closestIndex == 0 || _chainTracker.Balls[closestIndex - 1].transform.position.z > closestBall.transform.position.z)
            { 
                AttachBallToChain(newBall, closestIndex);
            }
            else
            {
                AttachBallToChain(newBall, closestIndex + 1);   
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
                    insertedBall.SetInteractive(false);
                    balls.Remove(ball);
                    ball.Deactivate();
                        
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
    }
}