using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.PathCreation;

namespace Code.Services.BallController
{
    public class ChainState
    {
        private readonly List<ChainSegment> _segments;
        private readonly CancellationTokenSource _lifetime = new();

        public CancellationToken Token { get; }
        public bool FeedClosed { get; set; }
        public float MeterReverseUntil { get; set; }
        public float LastRemovalDistance { get; set; }
        public PathCreator LastRemovalPath { get; set; }
        public bool HasStarted { get; private set; }
        public bool HasWon { get; private set; }
        public bool HasLost { get; private set; }
        public bool CanPlay => HasStarted && !HasWon && !HasLost && !Token.IsCancellationRequested;

        public ChainState(List<ChainSegment> segments)
        {
            _segments = segments;
            Token = _lifetime.Token;
        }

        public void Start() => HasStarted = true;

        public bool TryFinish(bool won)
        {
            if (!CanPlay)
                return false;

            HasWon = won;
            HasLost = !won;
            return true;
        }

        public void Cancel()
        {
            if (Token.IsCancellationRequested)
                return;

            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        public float HeadDistance => _segments.Count > 0 ? _segments[0].HeadLogicalDistance : 0f;
        public float GetHeadDistance(PathCreator path) =>
            _segments.Find(segment => segment.Path == path)?.HeadLogicalDistance ?? 0f;
        public int TotalBallCount => _segments.Sum(s => s.Count);
    }
}
