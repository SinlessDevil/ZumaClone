using Code.Services.PersistenceProgress.Analytic;
using Code.Services.PersistenceProgress.Player;

namespace Code.Services.PersistenceProgress
{
    public interface IPersistenceProgressService
    {
        PlayerData PlayerData { get; set; }
        AnalyticsData AnalyticsData { get; set; }
    }
}