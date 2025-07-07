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

namespace Tests.EditMode
{
    public class LevelServiceTest
    {
        private LevelService _levelService;
        private IPersistenceProgressService _persistenceProgress;
        private IStaticDataService _staticData;
        private ITimeService _timeService;

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

            _staticData.ForChapter(1).Returns(new ChapterStaticData
            {
                ChapterId = 1,
                Levels = new List<LevelStaticData>
                {
                    new() { LevelId = 1 },
                    new() { LevelId = 2 },
                    new() { LevelId = 3 },
                }
            });

            _levelService = new LevelService(_persistenceProgress, _staticData, _timeService);
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
