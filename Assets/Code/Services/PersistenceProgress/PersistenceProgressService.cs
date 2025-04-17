using Code.Services.PersistenceProgress.Analytic;
using Code.Services.PersistenceProgress.Player;

namespace Code.Services.PersistenceProgress
{
    public class PersistenceProgressService : IPersistenceProgressService
    {
        public PlayerData PlayerData { get; set; }
        public AnalyticsData AnalyticsData { get; set; }
    }
}