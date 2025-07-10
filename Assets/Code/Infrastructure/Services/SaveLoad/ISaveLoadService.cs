using Code.Infrastructure.Services.PersistenceProgress.Player;

namespace Code.Infrastructure.Services.SaveLoad
{
    public interface ISaveLoadService
    {
        void SaveProgress();
        void Save(PlayerData playerData);
        PlayerData Load();
    }
}