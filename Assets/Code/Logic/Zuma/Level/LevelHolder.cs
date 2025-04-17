using PathCreation;
using PathCreation.Examples;
using UnityEngine;

namespace Code.Logic.Zuma.Level
{
    public class LevelHolder : MonoBehaviour
    {
        [field: SerializeField] public Transform SpawnPositionPlayer { get; private set; }
        [field: SerializeField] public PathCreator PathCreator { get; private set; }
        [field: SerializeField] public RoadMeshCreator RoadMeshCreator { get; private set; }
        [field: SerializeField] public ParticleSystemHolder DefaultParticleSystemHolder { get; private set; }
        [field: SerializeField] public ParticleSystemHolder LoseParticleSystemHolder { get; private set; }
        [field: SerializeField] public LevelStart LevelStart { get; private set; }
        [field: SerializeField] public LevelEnd LevelEnd { get; private set; }
    }
}