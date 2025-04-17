using Code.Services.PersistenceProgress;
using Zenject;

namespace Code.Services.SFX
{
    public class VibrationService : IVibrationService
    {
        private bool _isVibration => _progressService.PlayerData.PlayerSettings.Vibration;
        private IPersistenceProgressService _progressService;

        [Inject]
        public void Constructor(IPersistenceProgressService progressService)
        {
            _progressService = progressService;
        }

        public void Light()
        {
            if (_isVibration)
                Vibration.LightImpact();
        }

        public void Medium()
        {
            if (_isVibration)
                Vibration.MediumImpact();
        }

        public void Heavy()
        {
            if (_isVibration)
                Vibration.HeavyImpact();
        }
    }
}