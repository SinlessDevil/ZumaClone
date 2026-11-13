using System;
using Code.Services.Finish.Lose;
using Code.Services.Finish.Win;
using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.Services.SaveLoad;
using Code.StaticData.Levels;

namespace Code.Services.Finish
{
    public class FinishService : IFinishService
    {
        private readonly IWinService _winService;
        private readonly ILoseService _loseService;
        private readonly ILevelService _levelService;
        private readonly ILevelLocalProgressService _progress;
        private readonly ISaveLoadFacade _save;

        public FinishService(
            IWinService winService, 
            ILoseService loseService,
            ILevelService levelService, 
            ILevelLocalProgressService progress, 
            ISaveLoadFacade save)
        {
            _winService = winService;
            _loseService = loseService;
            _levelService = levelService;
            _progress = progress;
            _save = save;
        }

        public void Win()
        {
            _levelService.FinishRun(true, _progress.Score);
            
            switch (_levelService.GetCurrentLevelStaticData().LevelTypeId)
            {
                case LevelTypeId.Regular:
                    _winService.Win();
                    break;
                case LevelTypeId.Special:
                case LevelTypeId.Bonus:
                    _winService.BonusWin();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Lose()
        {
            _levelService.FinishRun(false, _progress.Score);
            _save.SaveProgress(SaveMethodType.PlayerPrefs);
            _loseService.Lose();
        }
    }
}
