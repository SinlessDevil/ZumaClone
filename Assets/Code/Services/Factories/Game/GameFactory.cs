using Code.Logic.Zuma.Balls;
using Code.Logic.Zuma.Level;
using Code.Logic.Zuma.Players;
using Code.Services.Levels;
using Code.StaticData.Levels;
using UnityEngine;
using Zenject;

namespace Code.Services.Factories.Game
{
    public class GameFactory : Factory, IGameFactory
    {
        private readonly ILevelGeometryService _levelGeometry;

        public GameFactory(IInstantiator instantiator, ILevelGeometryService levelGeometry) : base(instantiator)
        {
            _levelGeometry = levelGeometry;
        }
        public Player Player { get; private set; }
        
        public Ball CreateBall(Vector3 position, Quaternion rotation)
        {
            var ball = Instantiate(ResourcePath.BallPath, position, rotation, null).GetComponent<Ball>();
            return ball;
        }

        public Player CreatePlayer(Vector3 position, Quaternion rotation)
        {
            var player = Instantiate(ResourcePath.PlayerPath, position, rotation, null).GetComponent<Player>();
            Player = player;
            return player;
        }
        
        public LevelHolder CreateLevelHolder(LevelStaticData levelStaticData, LevelThemeStaticData theme = null)
        {
            LevelHolder level = Instantiate(ResourcePath.LevelPath).GetComponent<LevelHolder>();
            try
            {
                _levelGeometry.Apply(level, levelStaticData, theme);
                return level;
            }
            catch
            {
                Object.Destroy(level.gameObject);
                throw;
            }
        }
    }
}