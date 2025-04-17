using Code.Services.PersistenceProgress.Player;

namespace Code.Services.SaveLoad
{
    public interface ISaveLoadService
    {
        PlayerData LoadProgress();
        void SaveProgress();
        void ResetProgress();
    }
}