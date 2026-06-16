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

        // Counter instead of bool: handles user-shot combos overlapping with chain reactions
        private int _activeComboCount;
        public bool IsProcessingCombo => _activeComboCount > 0;

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

        // Called by BallChainController.CheckMerges only when no combo chain is active
        public void CheckMatchAtJunction(ChainSegment segment, int junctionIdx)
        {
            if (segment.Count == 0) return;

            junctionIdx = Mathf.Clamp(junctionIdx, 0, segment.Count - 1);
            var balls = segment.Balls.ToList();
            Ball anchor = balls[junctionIdx];
            var matches = FindMatchesAroundBall(anchor, balls);

            if (matches.Count < _ballChainDto.MatchingCount) return;

            // Initial match — no combo multiplier yet (1x)
            ApplyMatchScore(matches, anchor, comboMultiplier: 1);

            RunComboChain(matches, segment).Forget();
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
                // Initial match from a player shot — no combo multiplier yet (1x)
                ApplyMatchScore(matches, pivotBall, comboMultiplier: 1);

                await RunComboChain(matches, segment);
            }
            else
            {
                pivotBall.SetInteractive(false);
            }
        }

        // Entry point for all combo chains — tracks nesting so IsProcessingCombo stays true
        // for the full duration of a chain reaction, even across multiple sequential steps
        private async UniTask RunComboChain(List<Ball> matchBalls, ChainSegment segment)
        {
            _activeComboCount++;
            try
            {
                // First chained follow-up match starts the multiplier at 2x
                await ComboStep(matchBalls, segment, comboMultiplier: 2);
            }
            finally
            {
                _activeComboCount--;
            }
        }

        // One step of a combo: destroy → punch back → split → wait for merge → check next match.
        // comboMultiplier is applied to the next match found at the end of this step and grows by 1
        // for every further link in the chain (2x → 3x → 4x …).
        private async UniTask ComboStep(List<Ball> matchBalls, ChainSegment segment, int comboMultiplier)
        {
            // 1. Wait for all destroy animations to finish
            await UniTask.WhenAll(matchBalls.Select(WaitForDestroyAnimation));

            int lowestIndex = matchBalls.Min(b => b.Index);
            float punchBack = matchBalls.Count * _ballChainDto.SpacingBalls;

            // 2. Remove matched balls from high to low to keep indices valid
            foreach (var ball in matchBalls.OrderByDescending(b => b.Index))
            {
                segment.RemoveBallAt(ball.Index);
                ball.Deactivate();
            }

            if (lowestIndex >= segment.Count)
            {
                // Match was at the tail — skull-side balls get the punch back and chain stays put
                segment.SetHeadLogicalDistance(segment.HeadLogicalDistance - punchBack);
                segment.ReIndexBalls();
                TryCleanupEmptySegment(segment);
                return;
            }

            // 3. Split: balls [lowestIndex..] become the new back segment
            ChainSegment back = segment.SplitAt(lowestIndex);
            segment.ReIndexBalls();
            back.ReIndexBalls();

            int segIdx = _segments.IndexOf(segment);

            if (segment.Count == 0)
            {
                // Front wiped (match at head) — back keeps its original logical position,
                // NOT the skull position that SplitAt(0) assigned.
                // Without this fix the remaining balls lurch forward toward the skull.
                back.SetHeadLogicalDistance(
                    back.HeadLogicalDistance - matchBalls.Count * _ballChainDto.SpacingBalls);
                _segments[segIdx] = back;
                TryCleanupEmptySegment(segment);
                return;
            }

            // 4. Check junction colors to decide movement direction.
            // Same color  → punch back: front retreats toward back (both magnetize, combo incoming)
            // Diff color  → front holds position: back catches up and pushes front (normal merge)
            Ball frontTail = segment.GetBall(segment.Count - 1);
            Ball backHead  = back.GetBall(0);
            bool junctionMatch = frontTail.Color == backHead.Color;

            if (junctionMatch)
                segment.SetHeadLogicalDistance(segment.HeadLogicalDistance - punchBack);

            // Index of front's last ball — this becomes the junction after merge
            int junctionIdx = segment.Count - 1;

            // 5. Launch back segment catch-up; CheckMerges will do the physical merge
            back.IsCatchingUp = true;
            back.CurrentSpeed = back.BaseSpeed * _ballChainDto.CatchupSpeedMultiplier;
            _segments.Insert(segIdx + 1, back);

            // 6. Wait until CheckMerges has logically merged back into front
            await UniTask.WaitUntil(() => !_segments.Contains(back));

            // 7. Wait for the visual gap at the junction to actually close.
            // Logical merge fires early (distance math), but SmoothDamp needs more time.
            // junctionIdx     = last ball of original front
            // junctionIdx + 1 = first ball of original back (now part of segment after merge)
            int nextIdx = junctionIdx + 1;
            if (nextIdx < segment.Count)
            {
                await UniTask.WaitUntil(() =>
                    nextIdx < segment.Count &&
                    segment.GetVisualDistance(junctionIdx) - segment.GetVisualDistance(nextIdx)
                        <= _ballChainDto.SpacingBalls * 1.5f
                );
            }

            // 8. Chain is visually closed — check for a new match at the junction
            if (junctionIdx < 0 || junctionIdx >= segment.Count) return;

            var mergedBalls = segment.Balls.ToList();
            Ball anchor = mergedBalls[junctionIdx];
            var nextMatches = FindMatchesAroundBall(anchor, mergedBalls);

            if (nextMatches.Count < _ballChainDto.MatchingCount) return;

            ApplyMatchScore(nextMatches, anchor, comboMultiplier);

            // 9. Continue combo chain sequentially — same _activeComboCount scope,
            //    multiplier grows by 1 for the next link (2x → 3x → 4x …)
            await ComboStep(nextMatches, segment, comboMultiplier + 1);
        }

        // Scores a match: base = matchCount * ScorePerItem, multiplied by the combo multiplier.
        // The widget shows "Nx +score" for combos (multiplier > 1) and just "+score" otherwise.
        private void ApplyMatchScore(List<Ball> matches, Ball anchor, int comboMultiplier)
        {
            int baseScore = matches.Count *
                _levelService.GetCurrentLevelStaticData().LevelConfig.ScoreConfig.ScorePerItem;
            int score = baseScore * comboMultiplier;

            _levelLocalProgressService.AddScore(score);
            _widgetBallChainProvider.SetUpWidget(matches, anchor, score, comboMultiplier);
            _winBallChainHandler.TryWin(_segments.Sum(s => s.Count) - matches.Count);
        }

        private void TryCleanupEmptySegment(ChainSegment segment)
        {
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
