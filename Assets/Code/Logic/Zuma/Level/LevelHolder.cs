using System;
using Code.PathCreation;
using UnityEngine;

namespace Code.Logic.Zuma.Level
{
    [Serializable]
    public sealed class LevelTrack
    {
        public PathCreator Path;
        public LevelEnd End;
        public Vector2[] Tunnels;

        public bool IsExposed(float distance)
        {
            if (Tunnels != null)
                foreach (Vector2 tunnel in Tunnels)
                    if (distance >= tunnel.x && distance <= tunnel.y)
                        return false;

            return true;
        }
    }

    public class LevelHolder : MonoBehaviour
    {
        [field: SerializeField] public Transform SpawnPositionPlayer { get; private set; }
        [field: SerializeField] public Transform[] CoinPositions { get; private set; }
        [field: SerializeField] public Vector2[] PrimaryTunnels { get; private set; }
        [field: SerializeField] public LevelTrack[] AdditionalTracks { get; private set; }
        [field: SerializeField] public PathCreator PathCreator { get; private set; }
        [field: SerializeField] public RoadMeshCreator RoadMeshCreator { get; private set; }
        [field: SerializeField] public ParticleSystemHolder DefaultParticleSystemHolder { get; private set; }
        [field: SerializeField] public ParticleSystemHolder LoseParticleSystemHolder { get; private set; }
        [field: SerializeField] public LevelStart LevelStart { get; private set; }
        [field: SerializeField] public LevelEnd LevelEnd { get; private set; }
    }
}