using System;

namespace Code.Services.PersistenceProgress.Player
{
    [Serializable]
    public class PlayerData
    {
        public PlayerLevelData PlayerLevelData = new();
        public PlayerSettings PlayerSettings = new();
        public PlayerTutorialData PlayerTutorialData = new();
    }
}