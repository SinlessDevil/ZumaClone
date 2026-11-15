using System;
using Code.Logic.Zuma.Level;
using Code.PathCreation;
using Code.StaticData.Levels;
using UnityEngine;

namespace Code.Services.Levels
{
    public sealed class LevelGeometryService : ILevelGeometryService
    {
        public void Apply(LevelHolder level, LevelStaticData data, LevelThemeStaticData theme = null)
        {
            LevelGeometry geometry = data.Geometry;
            if (geometry == null || !geometry.IsValidPath())
            {
                throw new InvalidOperationException($"Level {data.LevelId} ({data.LevelName}) has an invalid road.");
            }

            PathCreator path = level.PathCreator;
            path.InitializeEditorData(false);
            path.EditorData.vertexPathMaxAngleError = geometry.MaxAngleError;
            path.EditorData.vertexPathMinVertexSpacing = geometry.MinVertexSpacing;
            path.bezierPath = new BezierPath(geometry.Path);

            level.SpawnPositionPlayer.position = level.transform.TransformPoint(geometry.PlayerPosition);
            level.SetTrackData(geometry.Tunnels, geometry.CoinSpawns);
            SetGatePose(level.transform, level.LevelStart.transform, geometry.GetGatePose(true));
            SetGatePose(level.transform, level.LevelEnd.transform, geometry.GetGatePose(false));

            if (theme != null && theme.Ground != null)
            {
                level.FieldRenderer.sharedMaterial = theme.Ground;
            }

            if (geometry.FieldTexture != null)
            {
                RoadMeshCreator.ApplySurface(level.FieldRenderer, geometry.FieldTexture);
            }

            RoadMeshCreator road = level.RoadMeshCreator;
            road.roadWidth = geometry.RoadHalfWidth;
            road.thickness = geometry.RoadThickness;
            bool themedTrack = theme != null && theme.Track != null;
            if (themedTrack)
            {
                road.roadMaterial = theme.Track;
                if (theme.Ground != null)
                    road.undersideMaterial = theme.Ground;
            }
            road.textureTiling = themedTrack && geometry.RoadTexture == null
                ? path.path.length / Mathf.Max(0.1f, theme.TrackRepeatLength)
                : geometry.RoadTextureTiling;
            road.flattenSurface = geometry.FlattenRoad;
            road.SurfaceTexture = geometry.RoadTexture;
            road.TriggerUpdate();

            if (data.GroundDecorations != null)
            {
                GameObject decorations = UnityEngine.Object.Instantiate(data.GroundDecorations, level.transform, false);
                decorations.name = data.GroundDecorations.name;
            }
        }

        private static void SetGatePose(Transform root, Transform target, Pose pose)
        {
            target.SetPositionAndRotation(root.TransformPoint(pose.position), root.rotation * pose.rotation);
        }
    }
}
