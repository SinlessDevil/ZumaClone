using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma.Balls;

namespace Code.Services.BallController
{
    public class ChainState
    {
        private readonly List<ChainSegment> _segments;

        public ChainState(List<ChainSegment> segments) => _segments = segments;

        public float HeadDistance => _segments.Count > 0 ? _segments[0].HeadLogicalDistance : 0f;
        public Ball FrontBall => _segments.Count > 0 && _segments[0].Count > 0 ? _segments[0].GetBall(0) : null;
        public int TotalBallCount => _segments.Sum(s => s.Count);
    }
}
