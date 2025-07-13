using Code.Services.PersistenceProgress.Player;

namespace Code.Services.SaveLoad
{
    public interface ISaveLoadService
    {
        void SaveProgress();
        void Save(PlayerData playerData);
        PlayerData Load();
    }
}