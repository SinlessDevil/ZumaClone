using Code.Infrastructure.Services.PersistenceProgress.Player;

namespace Code.Infrastructure.Services.SaveLoad
{
    public interface ISaveLoadFacade
    {
        void SaveProgress(SaveMethod method);
        void Save(SaveMethod method, PlayerData data);
        PlayerData Load(SaveMethod method);
    }
}