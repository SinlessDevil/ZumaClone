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
        private readonly List<ChainSegment> _segments;
        private readonly WidgetBallChainProvider _widgetBallChainProvider;
        private readonly WinBallChainHandler _winBallChainHandler;
        private readonly ILevelService _levelService;
        private readonly ILevelLocalProgressService _levelLocalProgressService;

        public AttachingBallChainHandler(
            PathCreator pathCreator,
            BallChainDTO ballChainDto,
            List<ChainSegment> segments,
            WidgetBallChainProvider widgetBallChainProvider,
            WinBallChainHandler winBallChainHandler,
            ILevelService levelService,
            ILevelLocalProgressService levelLocalProgressService)
        {
            _pathCreator = pathCreator;
            _ballChainDto = ballChainDto;
            _segments = segments;
            _widgetBallChainProvider = widgetBallChainProvider;
            _winBallChainHandler = winBallChainHandler;
            _levelService = levelService;
            _levelLocalProgressService = levelLocalProgressService;
        }

        public void TryAttachBall(Ball newBall)
        {
            var (segment, index) = FindClosestCollision(newBall);
            if (segment == null) return;

            InsertBallToSegment(newBall, segment, index);
        }

        // Called by BallChainController after two segments merge
        public void CheckMatchAtJunction(ChainSegment segment, int junctionIdx)
        {
            if (segment.Count == 0) return;

            junctionIdx = Mathf.Clamp(junctionIdx, 0, segment.Count - 1);
            var balls = segment.Balls.ToList();
            Ball anchor = balls[junctionIdx];
            var matches = FindMatchesAroundBall(anchor, balls);

            if (matches.Count >= _ballChainDto.MatchingCount)
            {
                int score = matches.Count *
                    _levelService.GetCurrentLevelStaticData().LevelConfig.ScoreConfig.ScorePerItem;
                _levelLocalProgressService.AddScore(score);
                _widgetBallChainProvider.SetUpWidget(matches, anchor, score);
                _winBallChainHandler.TryWin(_segments.Sum(s => s.Count) - matches.Count);

                DestroyAndSplit(matches, segment).Forget();
            }
        }

        private (ChainSegment segment, int index) FindClosestCollision(Ball newBall)
        {
            float minDist = float.MaxValue;
            ChainSegment closest = null;
            int closestIndex = -1;

            foreach (var segment in _segments)
            {
                for (int i = 0; i < segment.Count; i++)
                {
                    float dist = Vector3.Distance(segment.GetBall(i).transform.position,
                        newBall.transform.position);

                    if (dist <= _ballChainDto.CollisionThreshold && dist < minDist)
                    {
                        minDist = dist;
                        closest = segment;
                        closestIndex = i;
                    }
                }
            }

            return (closest, closestIndex);
        }

        private void InsertBallToSegment(Ball newBall, ChainSegment segment, int collisionIndex)
        {
            var path = _pathCreator.path;
            float newBallDist = path.GetClosestDistanceAlongPath(newBall.transform.position);
            float collidedDist = path.GetClosestDistanceAlongPath(
                segment.GetBall(collisionIndex).transform.position);

            int insertIndex;
            if (collisionIndex == segment.Count - 1 && newBallDist < collidedDist)
                insertIndex = segment.Count;
            else if (collisionIndex == 0 && newBallDist > collidedDist)
                insertIndex = 0;
            else
                insertIndex = newBallDist > collidedDist ? collisionIndex : collisionIndex + 1;

            newBall.Dispose();

            float initialPathDist = path.GetClosestDistanceAlongPath(newBall.transform.position);
            segment.InsertBall(insertIndex, newBall, initialPathDist);
            segment.SetHeadLogicalDistance(segment.HeadLogicalDistance + _ballChainDto.SpacingBalls);
            segment.ReIndexBalls();

            SlideInAndCheckMatches(newBall).Forget();
        }

        private async UniTaskVoid SlideInAndCheckMatches(Ball ball)
        {
            await UniTask.Delay((int)(_ballChainDto.DurationMovingOffset * 1000));
            await CheckAndDestroyMatches(ball);
        }

        private async UniTask CheckAndDestroyMatches(Ball pivotBall)
        {
            ChainSegment segment = FindSegmentOf(pivotBall);
            if (segment == null) return;

            var balls = segment.Balls.ToList();
            var matches = FindMatchesAroundBall(pivotBall, balls);

            if (matches.Count >= _ballChainDto.MatchingCount)
            {
                int score = matches.Count *
                    _levelService.GetCurrentLevelStaticData().LevelConfig.ScoreConfig.ScorePerItem;
                _levelLocalProgressService.AddScore(score);
                _widgetBallChainProvider.SetUpWidget(matches, pivotBall, score);
                _winBallChainHandler.TryWin(_segments.Sum(s => s.Count) - matches.Count);

                await DestroyAndSplit(matches, segment);
            }
            else
            {
                pivotBall.SetInteractive(false);
            }
        }

        private async UniTask DestroyAndSplit(List<Ball> matchBalls, ChainSegment segment)
        {
            await UniTask.WhenAll(matchBalls.Select(WaitForDestroyAnimation));

            int lowestIndex = matchBalls.Min(b => b.Index);

            // Remove from high to low to preserve indices during removal
            foreach (var ball in matchBalls.OrderByDescending(b => b.Index))
            {
                segment.RemoveBallAt(ball.Index);
                ball.Deactivate();
            }

            // Split remaining balls after match into back segment
            if (lowestIndex < segment.Count)
            {
                ChainSegment back = segment.SplitAt(lowestIndex);
                segment.ReIndexBalls();
                back.ReIndexBalls();

                int segIdx = _segments.IndexOf(segment);

                if (segment.Count > 0)
                {
                    // Front has balls — back catches up
                    back.IsCatchingUp = true;
                    back.CurrentSpeed = back.BaseSpeed * _ballChainDto.CatchupSpeedMultiplier;
                    _segments.Insert(segIdx + 1, back);
                }
                else
                {
                    // Front is empty — back becomes the lead, no one to catch
                    _segments[segIdx] = back;
                }
            }
            else
            {
                segment.ReIndexBalls();
            }

            // Clean up empty front segment
            if (segment.Count == 0 && _segments.Contains(segment))
                _segments.Remove(segment);
        }

        private ChainSegment FindSegmentOf(Ball ball) =>
            _segments.FirstOrDefault(s => s.Balls.Contains(ball));

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
    }
}
