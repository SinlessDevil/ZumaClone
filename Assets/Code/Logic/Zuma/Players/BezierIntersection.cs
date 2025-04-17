using System.Collections.Generic;
using PathCreation;
using UnityEngine;

namespace Code.Logic.Zuma.Players
{
    public class BezierIntersection
    {
        private Vector3 _lineStart;
        private Vector3 _lineEnd;

        private List<Vector3> _intersectionPoints = new();
        
        private PathCreator _pathCreator;

        public BezierIntersection(PathCreator pathCreator)
        {
            _pathCreator = pathCreator;
        }

        public List<Vector3> GetIntersectionPoints(Vector3 lineStart, Vector3 lineEnd)
        {
            _intersectionPoints.Clear();
            FindIntersection(lineStart, lineEnd);
            return _intersectionPoints;
        }

        public void OnDrawIntersectionGizmos()
        {
            if (_pathCreator != null && _intersectionPoints.Count > 0)
            {
                int numSegments = _pathCreator.path.NumPoints - 1;
                Gizmos.color = Color.green;
                for (int i = 0; i < numSegments; i++)
                {
                    Vector3 p0 = _pathCreator.path.GetPoint(i);
                    Vector3 p1 = _pathCreator.path.GetPoint(i + 1);

                    Gizmos.DrawLine(p0, p1);
                }

                Gizmos.color = Color.red;
                Gizmos.DrawLine(_lineStart, _lineEnd);

                Gizmos.color = Color.blue;
                foreach (Vector3 intersection in _intersectionPoints)
                {
                    Gizmos.DrawSphere(intersection, 0.1f);
                }
            }
        }

        private void FindIntersection(Vector3 lineStart, Vector3 lineEnd)
        {
            _lineStart = lineStart;
            _lineEnd = lineEnd;

            int numSegments = _pathCreator.path.NumPoints - 1;

            for (int i = 0; i < numSegments; i++)
            {
                Vector3 p0 = _pathCreator.path.GetPoint(i);
                Vector3 p1 = _pathCreator.path.GetPoint(i + 1);

                Vector3? intersection = FindIntersectionWithLine(p0, p1, lineStart, lineEnd);

                if (intersection.HasValue)
                    _intersectionPoints.Add(intersection.Value);
            }
        }

        private Vector3? FindIntersectionWithLine(Vector3 p0, Vector3 p1, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 curveDirection = p1 - p0;
            Vector3 lineDirection = lineEnd - lineStart;

            float denominator = curveDirection.x * lineDirection.z - curveDirection.z * lineDirection.x;

            if (Mathf.Approximately(denominator, 0f))
                return null;

            float t1 = ((lineStart.x - p0.x) * lineDirection.z - (lineStart.z - p0.z) * lineDirection.x) / denominator;
            float t2 = ((lineStart.x - p0.x) * curveDirection.z - (lineStart.z - p0.z) * curveDirection.x) /
                       denominator;

            if (t1 >= 0f && t1 <= 1f && t2 >= 0f && t2 <= 1f)
            {
                Vector3 intersection = p0 + t1 * curveDirection;
                return intersection;
            }

            return null;
        }
    }
}