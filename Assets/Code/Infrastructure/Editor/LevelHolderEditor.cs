using Code.Logic.Zuma.Level;
using Code.PathCreation;
using UnityEditor;
using UnityEngine;

namespace Code.Infrastructure.Editor
{
    [CustomEditor(typeof(LevelHolder))]
    public class LevelHolderEditor : UnityEditor.Editor
    {
        private const float HeadingSampleDistance = 0.15f;
        private static readonly Quaternion StartYawOffset = Quaternion.Euler(0f, 30f, 0f);
        private static readonly Quaternion EndYawOffset = Quaternion.Euler(0f, 90f, 0f);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Update Start & End Positions"))
            {
                UpdateStartAndEndPositions(true);
            }
        }

        private void UpdateStartAndEndPositions(bool updatePositions)
        {
            LevelHolder level = (LevelHolder)target;
            if (level.PathCreator == null)
            {
                Debug.LogWarning("PathCreator is not assigned!", level);
                return;
            }

            VertexPath path = level.PathCreator.path;
            if (level.LevelStart != null)
                AlignGate(level.LevelStart.transform, path, true, updatePositions);

            if (level.LevelEnd != null)
                AlignGate(level.LevelEnd.transform, path, false, updatePositions);

            if (level.AdditionalTracks == null)
                return;

            foreach (LevelTrack track in level.AdditionalTracks)
            {
                if (track?.Path != null && track.End != null)
                    AlignGate(track.End.transform, track.Path.path, false, updatePositions);
            }
        }

        private static void AlignGate(Transform gate, VertexPath path, bool isStart, bool updatePosition)
        {
            if (path == null || path.length <= Mathf.Epsilon)
                return;

            float sampleDistance = Mathf.Min(HeadingSampleDistance, path.length);
            float sampleFromStart = isStart ? sampleDistance : path.length - sampleDistance;
            Vector3 endpoint = path.GetPoint(isStart ? 0 : path.NumPoints - 1);
            Vector3 sample = path.GetPointAtDistance(sampleFromStart, EndOfPathInstruction.Stop);
            Vector3 heading = Vector3.ProjectOnPlane(isStart ? sample - endpoint : endpoint - sample, Vector3.up);

            Undo.RecordObject(gate, "Align Track Gate");
            if (updatePosition)
                gate.position = endpoint;

            // Apply each gate model's yaw offset relative to the track.
            if (heading.sqrMagnitude > 0.000001f)
            {
                Quaternion headingRotation = Quaternion.LookRotation(heading);
                gate.rotation = headingRotation * (isStart ? StartYawOffset : EndYawOffset);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(gate);
        }
    }
}
