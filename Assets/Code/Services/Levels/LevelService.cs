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
        private const int StartingLives = 3;
        private const int ExtraLifePoints = 50000;
        private LevelHolder _levelHolder;
        private LevelContainer _gauntletLevel;
        private bool _randomGauntlet;
        private PlayerLevelData Progress => _persistenceProgressService.PlayerData.PlayerLevelData;

        public ZumaMode Mode { get; private set; }
        public int AdventureLives => Progress.AdventureLives;
        public bool CanContinueAdventure => AdventureLives > 0;
        public bool AdventureFinished => Progress.AdventureComplete;
        
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
            return _staticDataService.ForLevel(GetCurrentChapter(), GetCurrentLevel());
        }

        public int GetCurrentLevel() => (_gauntletLevel ?? Progress.CurrentProgress).LevelId;

        public int GetCurrentChapter() => (_gauntletLevel ?? Progress.CurrentProgress).ChapterId;

        public int GetCurrentLevelIndex() =>
            GetCurrentLevel() - 1;
        
        public int GetCurrentChapterIndex() => 
            GetCurrentChapter() - 1;

        public LevelContainer GetCurrentLevelContainer()
        {
            if (Mode != ZumaMode.Adventure)
                return null;

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

            return chapters.OrderBy(chapter => chapter.ChapterId).ToList();
        }

        
        public void LevelsComplete()
        {
            if (Mode != ZumaMode.Adventure)
                return;
            var playerData = _persistenceProgressService.PlayerData.PlayerLevelData;
            
            LevelContainer completed = playerData.LevelsComleted.Find(lc =>
                lc.ChapterId == playerData.CurrentProgress.ChapterId && lc.LevelId == playerData.CurrentProgress.LevelId);
            
            float time = _timerService.GetElapsedTime();
            if (completed == null)
            {
                completed = new LevelContainer
                {
                    ChapterId = playerData.CurrentProgress.ChapterId,
                    LevelId = playerData.CurrentProgress.LevelId
                };
                playerData.LevelsComleted.Add(completed);
            }

            if (completed.Time <= 0f || time < completed.Time)
                completed.Time = time;

            if (completed.ChapterId != playerData.LastProgress.ChapterId ||
                completed.LevelId != playerData.LastProgress.LevelId)
                return;

            playerData.LastProgress.LevelId++;
            
            var currentChapter = _staticDataService.ForChapter(playerData.LastProgress.ChapterId);
            
            if (playerData.LastProgress.LevelId > currentChapter.Levels.Count)
            {
                if (playerData.LastProgress.ChapterId >= _staticDataService.Chapters.Count)
                {
                    playerData.LastProgress.LevelId = currentChapter.Levels.Count;
                    playerData.AdventureComplete = true;
                }
                else
                {
                    playerData.LastProgress.ChapterId++;
                    playerData.LastProgress.LevelId = 1;
                    playerData.StageCheckpoint = new LevelContainer
                    {
                        ChapterId = playerData.LastProgress.ChapterId,
                        LevelId = 1
                    };
                }
            }

            playerData.CurrentProgress.ChapterId = playerData.LastProgress.ChapterId;
            playerData.CurrentProgress.LevelId = playerData.LastProgress.LevelId;
        }
        
        public bool VisitLevel()
        {
            if (Mode != ZumaMode.Adventure || IsGauntletUnlocked(GetCurrentChapter(), GetCurrentLevel()))
                return false;

            (Progress.VisitedLevels ??= new List<LevelContainer>()).Add(new LevelContainer
            {
                ChapterId = GetCurrentChapter(),
                LevelId = GetCurrentLevel()
            });
            return true;
        }

        public bool IsGauntletUnlocked(int chapter, int level) =>
            IsLevelCompleted(chapter, level) || Progress.VisitedLevels?.Any(item =>
                item.ChapterId == chapter && item.LevelId == level) == true;

        public bool SelectGauntlet(ZumaMode mode, int chapter, int level, bool random)
        {
            if (mode == ZumaMode.Adventure)
                return false;

            if (random)
            {
                List<LevelContainer> unlocked = new();
                foreach (LevelContainer item in Progress.LevelsComleted.Concat(
                             Progress.VisitedLevels ?? Enumerable.Empty<LevelContainer>()))
                {
                    if (!unlocked.Any(existing => existing.ChapterId == item.ChapterId && existing.LevelId == item.LevelId))
                        unlocked.Add(item);
                }

                if (unlocked.Count == 0)
                    return false;

                LevelContainer selected = unlocked[UnityEngine.Random.Range(0, unlocked.Count)];
                chapter = selected.ChapterId;
                level = selected.LevelId;
            }
            else if (!IsGauntletUnlocked(chapter, level))
            {
                return false;
            }

            _gauntletLevel = new LevelContainer { ChapterId = chapter, LevelId = level };
            _randomGauntlet = random;
            Mode = mode;
            return true;
        }

        public void ReturnToAdventure()
        {
            _gauntletLevel = null;
            _randomGauntlet = false;
            Mode = ZumaMode.Adventure;
        }

        public void PrepareAdventureSelection()
        {
            ReturnToAdventure();
            if (Progress.DeluxeProgressVersion == 0)
            {
                Progress.AdventureLives = StartingLives;
                int finalChapter = _staticDataService.Chapters.Count;
                if (finalChapter > 0 && Progress.LastProgress.ChapterId > finalChapter)
                {
                    int finalLevel = _staticDataService.ForChapter(finalChapter).Levels.Count;
                    Progress.LastProgress = new LevelContainer { ChapterId = finalChapter, LevelId = finalLevel };
                    Progress.AdventureComplete = IsLevelCompleted(finalChapter, finalLevel);
                }

                if (finalChapter > 0 && Progress.CurrentProgress.ChapterId > finalChapter)
                    SetUpCurrentLevel(Progress.LastProgress.LevelId, Progress.LastProgress.ChapterId);

                Progress.StageCheckpoint = new LevelContainer { ChapterId = Progress.LastProgress.ChapterId };
                Progress.DeluxeProgressVersion = 1;
            }

            if (CanContinueAdventure)
                return;

            Progress.AdventureLives = StartingLives;
            Progress.AdventureScore = 0;
            LevelContainer checkpoint = Progress.StageCheckpoint ?? new LevelContainer();
            SetUpCurrentLevel(checkpoint.LevelId, checkpoint.ChapterId);
        }

        public void FinishRun(bool won, int score)
        {
            if (Mode == ZumaMode.Adventure)
            {
                int previous = Progress.AdventureScore;
                Progress.AdventureScore += score;
                Progress.AdventureLives += Progress.AdventureScore / ExtraLifePoints - previous / ExtraLifePoints;
                if (!won)
                    Progress.AdventureLives = System.Math.Max(0, Progress.AdventureLives - 1);
                return;
            }

            List<GauntletRecord> records = Progress.GauntletRecords ??= new List<GauntletRecord>();
            GauntletRecord record = records.Find(item => item.Mode == (int)Mode &&
                item.ChapterId == GetCurrentChapter() && item.LevelId == GetCurrentLevel());
            if (record == null)
            {
                record = new GauntletRecord { Mode = (int)Mode, ChapterId = GetCurrentChapter(), LevelId = GetCurrentLevel() };
                records.Add(record);
            }

            if (score >= record.Score)
            {
                record.Score = score;
                record.Rank = GetGauntletRank(score);
                record.SunGem = record.Rank == "Sun God";
            }
        }

        public void NextGauntletRound()
        {
            if (_randomGauntlet)
                SelectGauntlet(Mode, GetCurrentChapter(), GetCurrentLevel(), true);
        }

        public string GetGauntletRank(int score)
        {
            if (score >= 20000) return "Sun God";
            if (score >= 10000) return "Sun";
            if (score >= 5000) return "Jaguar";
            if (score >= 1500) return "Eagle";
            return "Rabbit";
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
