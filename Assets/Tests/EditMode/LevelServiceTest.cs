using System.Collections.Generic;
using Code.Services.Levels;
using Code.Services.PersistenceProgress;
using Code.Services.PersistenceProgress.Player;
using Code.Services.StaticData;
using Code.Services.Timer;
using Code.StaticData.Levels;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class LevelServiceTest
    {
        private LevelService _levelService;
        private IPersistenceProgressService _persistenceProgress;
        private IStaticDataService _staticData;
        private ITimeService _timeService;
        private ChapterStaticData _chapter;

        [SetUp]
        public void SetUp()
        {
            _persistenceProgress = Substitute.For<IPersistenceProgressService>();
            _staticData = Substitute.For<IStaticDataService>();
            _timeService = Substitute.For<ITimeService>();

            _persistenceProgress.PlayerData.Returns(new PlayerData
            {
                PlayerLevelData = new PlayerLevelData
                {
                    CurrentProgress = new LevelContainer { ChapterId = 1, LevelId = 2 },
                    LastProgress = new LevelContainer { ChapterId = 1, LevelId = 2 },
                    LevelsComleted = new List<LevelContainer>()
                }
            });

            _chapter = ScriptableObject.CreateInstance<ChapterStaticData>();
            _chapter.ChapterId = 1;
            for (int id = 1; id <= 3; id++)
            {
                LevelStaticData level = ScriptableObject.CreateInstance<LevelStaticData>();
                level.LevelId = id;
                _chapter.Levels.Add(level);
            }

            _staticData.ForChapter(1).Returns(_chapter);
            _staticData.Chapters.Returns(new List<ChapterStaticData> { _chapter });

            _levelService = new LevelService(_persistenceProgress, _staticData, _timeService);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (LevelStaticData level in _chapter.Levels)
                Object.DestroyImmediate(level);
            Object.DestroyImmediate(_chapter);
        }

        [Test]
        public void GetCurrentLevel_ReturnsCorrectLevel()
        {
            _levelService.GetCurrentLevel().Should().Be(2);
        }

        [Test]
        public void IsLevelCompleted_ShouldReturnFalse_WhenLevelNotCompleted()
        {
            _levelService.IsLevelCompleted(1, 3).Should().BeFalse();
        }

        [Test]
        public void LevelsComplete_AddsCompletedLevelAndAdvancesProgress()
        {
            _timeService.GetElapsedTime().Returns(5);

            _levelService.LevelsComplete();

            var playerData = _persistenceProgress.PlayerData.PlayerLevelData;

            playerData.LevelsComleted.Should().ContainSingle(l =>
                l.LevelId == 2 &&
                l.ChapterId == 1 &&
                l.Time == 5);

            playerData.CurrentProgress.LevelId.Should().Be(3);
            playerData.CurrentProgress.ChapterId.Should().Be(1);
        }

        [Test]
        public void GetAllChapters_IncludesCurrentAndLastAndCompleted()
        {
            _persistenceProgress.PlayerData.PlayerLevelData.LevelsComleted.Add(new LevelContainer
            {
                ChapterId = 1,
                LevelId = 1,
                Time = 10
            });

            var chapters = _levelService.GetAllChapters();
            chapters.Should().ContainSingle(c => c.ChapterId == 1);
        }
    }
}
