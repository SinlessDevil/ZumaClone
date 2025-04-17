using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma.Level;
using Code.Services.PersistenceProgress;
using Code.Services.PersistenceProgress.Player;
using Code.Services.StaticData;
using Code.Services.Timer;
using Code.StaticData.Levels;

namespace Code.Services.Levels
{
    public class LevelService : ILevelService
    {
        private LevelHolder _levelHolder;
        
        private readonly IPersistenceProgressService _persistenceProgressService;
        private readonly IStaticDataService _staticDataService;
        private readonly ITimeService _timerService;
        
        public LevelService(
            IPersistenceProgressService persistenceProgressService,
            IStaticDataService staticDataService,
            ITimeService timerService)
        {
            _persistenceProgressService = persistenceProgressService;
            _staticDataService = staticDataService;
            _timerService = timerService;
        }
        
        public LevelStaticData GetCurrentLevelStaticData()
        {
            return _staticDataService.ForLevel(_persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.ChapterId,
                _persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.LevelId);
        }

        public int GetCurrentLevel() => 
            _persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.LevelId;

        public int GetCurrentChapter() =>
            _persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.ChapterId;

        public int GetCurrentLevelIndex() =>
            _persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.LevelId - 1;
        
        public int GetCurrentChapterIndex() => 
            _persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.ChapterId - 1;

        public LevelContainer GetCurrentLevelContainer()
        {
            var currentLevel = GetCurrentLevel();
            var currentChapter = GetCurrentChapter();
            
            var playerData = _persistenceProgressService.PlayerData.PlayerLevelData;
            var levelContainer = playerData.LevelsComleted.Find(lc =>
                lc.ChapterId == currentChapter && lc.LevelId == currentLevel);

            return levelContainer ?? null;
        }
        
        public void SetUpCurrentLevel(int levelNumber, int chapterId)
        {
            _persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.LevelId = levelNumber;
            _persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.ChapterId = chapterId;
        }
        
        public List<ChapterStaticData> GetAllChapters()
        {
            HashSet<int> addedChapters = new HashSet<int>();
            List<ChapterStaticData> chapters = new List<ChapterStaticData>();

            var currentChapter = _persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.ChapterId;
            var lastChapter = _persistenceProgressService.PlayerData.PlayerLevelData.LastProgress.ChapterId;
            
            foreach (var completedLevel in _persistenceProgressService.PlayerData.PlayerLevelData.LevelsComleted)
            {
                int chapterId = completedLevel.ChapterId;

                if (addedChapters.Add(chapterId))
                {
                    chapters.Add(_staticDataService.ForChapter(chapterId));
                }
            }
            
            if (addedChapters.Add(currentChapter))
            {
                chapters.Add(_staticDataService.ForChapter(currentChapter));
            }
            
            if (addedChapters.Add(lastChapter))
            {
                chapters.Add(_staticDataService.ForChapter(lastChapter));
            }

            return chapters;
        }

        
        public void LevelsComplete()
        {
            var playerData = _persistenceProgressService.PlayerData.PlayerLevelData;
            
            bool alreadyCompleted = playerData.LevelsComleted.Any(lc =>
                lc.ChapterId == playerData.CurrentProgress.ChapterId && lc.LevelId == playerData.CurrentProgress.LevelId);
            
            if (!alreadyCompleted)
            {
                playerData.LevelsComleted.Add(new LevelContainer
                {
                    ChapterId = playerData.LastProgress.ChapterId,
                    LevelId = playerData.LastProgress.LevelId,
                    Time = _timerService.GetElapsedTime()
                });
            }
            else
            {
                return;
            }
            
            playerData.LastProgress.LevelId++;
            
            var currentChapter = _staticDataService.ForChapter(playerData.LastProgress.ChapterId);
            
            if (playerData.LastProgress.LevelId > currentChapter.Levels.Count)
            {
                playerData.LastProgress.ChapterId++;
                playerData.LastProgress.LevelId = 1;
            }

            playerData.CurrentProgress.ChapterId = playerData.LastProgress.ChapterId;
            playerData.CurrentProgress.LevelId = playerData.LastProgress.LevelId;
        }
        
        public bool IsLevelCompleted(int chapterId, int levelId)
        {
            return _persistenceProgressService.PlayerData.PlayerLevelData.LevelsComleted
                .Any(completedLevel => completedLevel.ChapterId == chapterId && completedLevel.LevelId == levelId);
        }
        
        public bool IsLevelCurrent(int chapterId, int levelId)
        {
            return _persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.LevelId == levelId &&
                   _persistenceProgressService.PlayerData.PlayerLevelData.CurrentProgress.ChapterId == chapterId;
        }
        
        public bool IsLastCompletedLevel(int chapterId, int levelId)
        {
            return _persistenceProgressService.PlayerData.PlayerLevelData.LastProgress.ChapterId == chapterId &&
                   _persistenceProgressService.PlayerData.PlayerLevelData.LastProgress.LevelId == levelId;
        }
        
        public void SetLevelHolder(LevelHolder levelHolder)
        {
            _levelHolder = levelHolder;
        }

        public LevelHolder GetLevelHolder()
        {
            return _levelHolder;
        }

        public void Cleanup()
        {
            _levelHolder = null;
        }
    }
}
