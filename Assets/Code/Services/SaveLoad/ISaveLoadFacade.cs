using Code.Services.PersistenceProgress.Player;

namespace Code.Services.SaveLoad
{
    public interface ISaveLoadFacade
    {
        void SaveProgress(SaveMethodType methodType);
        void Save(SaveMethodType methodType, PlayerData data);
        PlayerData Load(SaveMethodType methodType);
    }
}