using System;
using Code.PathCreation;
using UnityEngine;

namespace Code.StaticData.Levels
{
    [Serializable]
    public sealed class LevelGeometry
    {
        public BezierPath Path = new BezierPath(Vector3.zero, false, PathSpace.xz);
        public Vector3 PlayerPosition = new Vector3(0f, 0f, 2f);
        public GatePlacement Start = new GatePlacement(30f);
        public GatePlacement End = new GatePlacement(90f);
        public Pose[] CoinSpawns = Array.Empty<Pose>();
        public Vector2[] Tunnels = Array.Empty<Vector2>();
        public Texture2D FieldTexture;
        public Texture2D RoadTexture;
        [Min(0.02f)] public float RoadHalfWidth = 0.3f;
        [Min(0f)] public float RoadThickness = 0.15f;
        [Min(0.01f)] public float RoadTextureTiling = 1f;
        public bool FlattenRoad;
        public float MaxAngleError = 0.3f;
        public float MinVertexSpacing = 0.01f;

        public Pose GetGatePose(bool isStart)
        {
            Pose frame = GetGateFrame(isStart);
            GatePlacement gate = isStart ? Start : End;
            return new Pose(frame.position + frame.rotation * gate.Offset, frame.rotation * gate.Rotation);
        }

        public Pose GetGateFrame(bool isStart)
        {
            int anchorIndex = isStart ? 0 : Path.NumPoints - 1;
            int step = isStart ? 1 : -1;
            Vector3 anchor = Path[anchorIndex];
            for (int offset = 1; offset <= 3; offset++)
            {
                Vector3 heading = step * (Path[anchorIndex + offset * step] - anchor);
                heading.y = 0f;
                if (heading.sqrMagnitude > 0.000001f)
                {
                    return new Pose(anchor, Quaternion.LookRotation(heading));
                }
            }

            return new Pose(anchor, Quaternion.identity);
        }

        public bool IsValidPath()
        {
            if (Path == null || Path.IsClosed || Path.NumPoints < 4)
            {
                return false;
            }

            for (int index = 0; index < Path.NumPoints; index++)
            {
                Vector3 point = Path[index];
                if (!float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z))
                {
                    return false;
                }

                if (index > 0 && index % 3 == 0 && (point - Path[index - 3]).sqrMagnitude < 0.000001f)
                {
                    return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public struct GatePlacement
    {
        public Vector3 Offset;
        public Quaternion Rotation;

        public GatePlacement(float yaw)
        {
            Offset = Vector3.zero;
            Rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
