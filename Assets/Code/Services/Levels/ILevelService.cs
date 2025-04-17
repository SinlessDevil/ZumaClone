using System.Collections.Generic;
using Code.Logic.Zuma.Level;
using Code.Services.PersistenceProgress.Player;
using Code.StaticData.Levels;

namespace Code.Services.Levels
{
    public interface ILevelService
    {
        public LevelStaticData GetCurrentLevelStaticData();
        public int GetCurrentLevel();
        public int GetCurrentChapter();
        public int GetCurrentLevelIndex();
        public int GetCurrentChapterIndex();
        public void SetUpCurrentLevel(int levelNumber, int chapterId);
        public void LevelsComplete();
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