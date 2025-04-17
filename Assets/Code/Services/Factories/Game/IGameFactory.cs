using Code.Logic.Zuma.Balls;
using Code.Logic.Zuma.Level;
using Code.Logic.Zuma.Players;
using Code.StaticData.Levels;
using UnityEngine;

namespace Code.Services.Factories.Game
{
    public interface IGameFactory
    {
        Player Player { get; }
        
        Ball CreateBall(Vector3 position, Quaternion rotation);
        Player CreatePlayer(Vector3 position, Quaternion rotation);
        public LevelHolder CreateLevelHolder(LevelStaticData levelStaticData);
    }
}