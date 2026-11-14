using System;
using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma.Balls;
using Code.PathCreation;
using Code.Services.Levels;
using Code.Services.LocalProgress;
using UnityEngine;

namespace Code.Services.BallController
{
    public class AttachingBallChainHandler
    {
        private const float MatchSpacingTolerance = 0.1f;

        private readonly BallChainDTO _settings;
        private readonly List<ChainSegment> _segments;
        private readonly WidgetBallChainProvider _widgets;
        private readonly ILevelLocalProgressService _progress;
        private readonly ChainState _state;
        private readonly int _pointsPerBall;
        private readonly Dictionary<int, bool> _shotResults = new();
        private readonly List<(Ball ball, int chainId, int shotId, int gapBonus, Vector3 impact)> _settlingShots = new();
        private int _shotId;
        private int _ballId;
        private int _nextResult = 1;
        private int _streak;

        public AttachingBallChainHandler(
            BallChainDTO settings, 
            List<ChainSegment> segments,
            WidgetBallChainProvider widgets, 
            ILevelService levels, 
            ILevelLocalProgressService progress,
            ChainState state)
        {
            _settings = settings;
            _segments = segments;
            _widgets = widgets;
            _progress = progress;
            _state = state;
            _pointsPerBall = levels.GetCurrentLevelStaticData().LevelConfig.ScoreConfig.ScorePerItem;
        }

        public event Action<BallPowerup, Vector3, int, int> PowerupTriggered;

        public void RegisterShot(Ball ball) => ball.ShotId = ++_shotId;
        public void RegisterChainBall(Ball ball) => ball.ChainId = ++_ballId;

        public void ResolveShot(int shotId, bool matched, Vector3 position)
        {
            if (shotId <= 0 || !_state.CanPlay)
                return;

            _shotResults.Add(shotId, matched);
            while (_shotResults.Remove(_nextResult, out bool result))
            {
                _nextResult++;
                _streak = result ? _streak + 1 : 0;
                if (_streak < 5) 
                    continue;
                
                int bonus = 100 + (_streak - 5) * 10;
                _progress.AddScore(bonus);
                _widgets.SetUpWidget(position, Color.white, $"Chain +{bonus}");
            }
        }

        public bool TryAttachBall(Ball shot, Vector3 start, Vector3 end, bool stationaryTargets = false,
            float coinTime = float.PositiveInfinity)
        {
            (ChainSegment targetSegment, int targetIndex, float earliest) = FindContact(start, end, stationaryTargets);
            if (!stationaryTargets)
                TrackGaps(shot, start, Vector3.Lerp(start, end, Mathf.Min(earliest, coinTime)));

            if (targetSegment == null || coinTime < earliest)
                return false;

            Ball hit = targetSegment.GetBall(targetIndex);
            Vector3 impact = Vector3.Lerp(start, end, earliest);
            Vector3 hitPosition = stationaryTargets ? hit.transform.position :
                Vector3.Lerp(hit.PreviousChainPosition, hit.transform.position, earliest);
            Insert(shot, targetSegment, targetIndex, impact, impact - hitPosition);
            return true;
        }

        public float ContactFraction(Vector3 start, Vector3 end) => FindContact(start, end, stationaryTargets: true).time;

        private (ChainSegment segment, int index, float time) FindContact(Vector3 start, Vector3 end, bool stationaryTargets)
        {
            float earliest = float.PositiveInfinity;
            ChainSegment targetSegment = null;
            int targetIndex = -1;
            foreach (ChainSegment segment in _segments)
            {
                for (int i = 0; i < segment.Count; i++)
                {
                    if (!segment.IsExposed(segment.GetVisualDistance(i)))
                        continue;

                    Ball target = segment.GetBall(i);
                    Vector3 current = target.transform.position;
                    Vector3 previous = stationaryTargets ? current : target.PreviousChainPosition;
                    if (!TryContact(start - previous, end - current, _settings.CollisionThreshold,
                            out float time) || time >= earliest)
                        continue;

                    earliest = time;
                    targetSegment = segment;
                    targetIndex = i;
                }
            }

            return (targetSegment, targetIndex, earliest);
        }

        private void TrackGaps(Ball shot, Vector3 start, Vector3 end)
        {
            for (int i = 0; i + 1 < _segments.Count; i++)
            {
                ChainSegment front = _segments[i];
                ChainSegment back = _segments[i + 1];
                if (front.Track != back.Track)
                    continue;

                float near = back.GetVisualDistance(0);
                float far = front.GetVisualDistance(front.Count - 1);
                float diameter = _settings.CollisionThreshold;
                float width = far - near - diameter;
                if (width < diameter || !front.IsExposed(far) || !back.IsExposed(near))
                    continue;

                float from = near + diameter * 0.5f;
                float to = far - diameter * 0.5f;
                var path = front.Path.path;
                int steps = Mathf.Max(1, Mathf.CeilToInt((to - from) / _settings.SpacingBalls));
                Vector3 previous = path.GetPointAtDistance(from, EndOfPathInstruction.Stop);
                for (int j = 1; j <= steps; j++)
                {
                    float distance = Mathf.Lerp(from, to, (float)j / steps);
                    Vector3 next = path.GetPointAtDistance(distance, EndOfPathInstruction.Stop);
                    if (front.IsExposed(distance) && Crosses(start, end, previous, next))
                    {
                        int bonus = Mathf.RoundToInt(Mathf.Lerp(_settings.Survival ? 250f : 500f, 10f,
                            Mathf.InverseLerp(diameter, _settings.SpacingBalls * 8f, width)));
                        shot.RecordGap(front.GetBall(front.Count - 1).ChainId, back.GetBall(0).ChainId, bonus);
                        break;
                    }
                    previous = next;
                }
            }
        }

        private static bool Crosses(Vector3 start, Vector3 end, Vector3 a, Vector3 b)
        {
            Vector3 travel = end - start;
            Vector3 edge = b - a;
            Vector3 offset = a - start;
            float determinant = travel.x * edge.z - travel.z * edge.x;
            if (Mathf.Abs(determinant) < 0.00001f)
                return false;

            float t = (offset.x * edge.z - offset.z * edge.x) / determinant;
            float u = (offset.x * travel.z - offset.z * travel.x) / determinant;
            return t > 0f && t <= 1f && u >= 0f && u <= 1f;
        }

        public void Explode(Vector3 position, float radius, int shotId, int depth)
        {
            List<Ball> victims = new();
            foreach (ChainSegment segment in _segments)
            {
                victims.AddRange(from ball in segment.Balls where segment
                    .IsExposed(segment
                        .GetVisualDistance(ball.Index)) let offset = ball.transform.position - position where offset.x * offset.x + offset.z * offset.z <= radius * radius select ball);
            }

            int removed = 0;
            foreach (Ball victim in victims)
            {
                ChainSegment segment = _segments.Find(candidate =>
                    victim.Index < candidate.Count && candidate.GetBall(victim.Index) == victim);
                if (segment == null)
                    continue;

                _state.LastRemovalDistance = segment.GetVisualDistance(victim.Index);
                _state.LastRemovalPath = segment.Path;
                RemoveMatch(segment, victim.Index, 1, shotId, depth);
                removed++;
            }

            if (removed <= 0) 
                return;
            
            int score = removed * _pointsPerBall;
            _progress.AddScore(score);
            _widgets.SetUpWidget(position, Color.white, $"Explosion +{score}");
        }

        public static bool TryContact(Vector3 start, Vector3 end, float radius, out float time)
        {
            Vector2 origin = new(start.x, start.z);
            Vector2 travel = new(end.x - start.x, end.z - start.z);
            float c = origin.sqrMagnitude - radius * radius;
            time = 0f;
            if (c <= 0f)
                return true;

            float a = travel.sqrMagnitude;
            float b = Vector2.Dot(origin, travel);
            float discriminant = b * b - a * c;
            if (a == 0f || b >= 0f || discriminant < 0f)
                return false;

            time = c / (-b + Mathf.Sqrt(discriminant));
            return time <= 1f;
        }

        public void CheckMatchAtJunction(ChainSegment segment, int junction, int shotId, int depth)
        {
            if (segment.GetBall(junction).Color == segment.GetBall(junction + 1).Color)
                TryMatch(segment, junction, shotId, depth);
        }

        public void ResolveSettledShots()
        {
            for (int i = 0; i < _settlingShots.Count; i++)
            {
                (Ball ball, int chainId, int shotId, int gapBonus, Vector3 impact) = _settlingShots[i];
                ChainSegment segment = ball != null && ball.ChainId == chainId
                    ? _segments.Find(candidate => ball.Index < candidate.Count && candidate.GetBall(ball.Index) == ball)
                    : null;
                if (segment != null && ball.IsEnteringChain)
                    continue;

                bool? matched = segment == null ? false : TryMatch(segment, ball.Index, shotId, 1, waitForSeating: true);
                if (!matched.HasValue)
                    continue;

                _settlingShots.RemoveAt(i--);
                if (matched.Value && gapBonus > 0)
                {
                    _progress.AddScore(gapBonus);
                    _widgets.SetUpWidget(impact, Color.white, $"Gap +{gapBonus}");
                }
                ResolveShot(shotId, matched.Value, impact);
            }
        }

        private void Insert(Ball shot, ChainSegment segment, int hitIndex, Vector3 impact, Vector3 offset)
        {
            float hitDistance = segment.GetVisualDistance(hitIndex);
            Vector3 tangent = segment.Path.path.GetDirectionAtDistance(hitDistance, EndOfPathInstruction.Stop);
            float along = Vector3.Dot(new Vector3(offset.x, 0f, offset.z), tangent);
            int insertIndex = along > 0f ? hitIndex : hitIndex + 1;
            float distance = hitDistance + Mathf.Clamp(along, -_settings.SpacingBalls, _settings.SpacingBalls);

            shot.Dispose();
            shot.SetChainPosition(impact);
            shot.BeginChainEntry(_settings.DurationMovingOffset);
            shot.SetInteractive(false);
            RegisterChainBall(shot);
            segment.InsertBall(insertIndex, shot, distance);
            segment.SetHeadLogicalDistance(segment.HeadLogicalDistance + _settings.SpacingBalls);
            segment.ReIndexBalls();

            _settlingShots.Add((shot, shot.ChainId, shot.ShotId, shot.GapBonus, impact));
        }

        private bool? TryMatch(ChainSegment segment, int pivot, int shotId, int depth, bool waitForSeating = false)
        {
            Color color = segment.GetBall(pivot).Color;
            int first = pivot;
            int last = pivot;
            
            while (first > 0 && segment.GetBall(first - 1).Color == color)
                first--;
            while (last + 1 < segment.Count && segment.GetBall(last + 1).Color == color)
                last++;

            int count = last - first + 1;
            if (count < _settings.MatchingCount)
                return false;

            if (waitForSeating && !IsGroupSeated(segment, first, last))
                return null;

            Vector3 position = segment.GetBall(pivot).transform.position;
            _state.LastRemovalDistance = segment.GetVisualDistance(pivot);
            _state.LastRemovalPath = segment.Path;
            int score = count * _pointsPerBall + (depth - 1) * 100;
            _progress.AddScore(score);
            _widgets.SetUpWidget(position, color, depth > 1 ? $"Combo +{score}" : $"+{score}");
            RemoveMatch(segment, first, count, shotId, depth);
            return true;
        }

        private bool IsGroupSeated(ChainSegment segment, int first, int last)
        {
            float spacing = _settings.SpacingBalls;
            for (int i = first; i <= last; i++)
            {
                if (segment.GetBall(i).IsEnteringChain)
                    return false;
                if (i == first)
                    continue;

                float distance = segment.GetVisualDistance(i - 1) - segment.GetVisualDistance(i);
                if (Mathf.Abs(distance - spacing) > spacing * MatchSpacingTolerance)
                    return false;
            }
            return true;
        }

        private void RemoveMatch(ChainSegment segment, int first, int count, int shotId, int depth)
        {
            int segmentIndex = _segments.IndexOf(segment);
            List<(BallPowerup kind, Vector3 position)> powerups = null;
            for (int i = first + count - 1; i >= first; i--)
            {
                Ball ball = segment.GetBall(i);
                if (ball.Powerup != BallPowerup.None)
                {
                    powerups ??= new List<(BallPowerup, Vector3)>();
                    powerups.Add((ball.Powerup, ball.transform.position));
                    ball.SetPowerup(BallPowerup.None);
                }
                segment.RemoveBallAt(i);
                ball.PlayDestroyAnimation(ball.Deactivate);
            }

            if (segment.Count == 0)
            {
                _segments.RemoveAt(segmentIndex);
            }
            else if (first == 0)
            {
                segment.SetHeadLogicalDistance(segment.HeadLogicalDistance - count * _settings.SpacingBalls);
            }
            else if (first < segment.Count)
            {
                ChainSegment back = segment.SplitAt(first);
                back.SetHeadLogicalDistance(back.HeadLogicalDistance - count * _settings.SpacingBalls);
                back.JunctionShotId = shotId;
                back.JunctionComboDepth = depth + 1;
                _segments.Insert(segmentIndex + 1, back);
                back.ReIndexBalls();
            }

            segment.ReIndexBalls();
            if (powerups == null) 
                return;
            
            foreach ((BallPowerup kind, Vector3 position) in powerups)
                PowerupTriggered?.Invoke(kind, position, shotId, depth);
        }
    }
}
