using System.Collections.Generic;
using Code.Logic.Zuma.Level;
using Code.Services.PersistenceProgress.Player;
using Code.StaticData.Levels;

namespace Code.Services.Levels
{
    public enum ZumaMode
    {
        Adventure,
        Practice,
        Survival
    }
    public interface ILevelService
    {
        public LevelStaticData GetCurrentLevelStaticData();
        public int GetCurrentLevel();
        public int GetCurrentChapter();
        public int GetCurrentLevelIndex();
        public int GetCurrentChapterIndex();
        public void SetUpCurrentLevel(int levelNumber, int chapterId);
        public void LevelsComplete();
        ZumaMode Mode { get; }
        int AdventureLives { get; }
        bool CanContinueAdventure { get; }
        bool AdventureFinished { get; }
        string GetGauntletRank(int score);
        bool VisitLevel();
        bool IsGauntletUnlocked(int chapter, int level);
        bool SelectGauntlet(ZumaMode mode, int chapter, int level, bool random);
        void ReturnToAdventure();
        void NextGauntletRound();
        void PrepareAdventureSelection();
        void FinishRun(bool won, int score);
        public void SetLevelHolder(LevelHolder levelHolder);
        public LevelHolder GetLevelHolder();
        public void Cleanup();
        List<ChapterStaticData> GetAllChapters();
        bool IsLevelCompleted(int chapterId, int levelId);
        bool IsLevelCurrent(int chapterId, int levelId);
        bool IsLastCompletedLevel(int chapterId, int levelId);
        LevelContainer GetCurrentLevelContainer();
    }
}