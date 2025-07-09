using NUnit.Framework;
using UnityEngine;
using FluentAssertions;

namespace Tests.EditMode.StaticDataService
{
    public class StaticDataResourcesTests
    {
        [Test]
        public void GameStaticData_ShouldExist()
        {
            var asset = Resources.Load<ScriptableObject>("StaticData/Balance/GameConfig");
            asset.Should().NotBeNull("GameStaticData must exist at Resources/StaticData/Balance/GameConfig");
        }

        [Test]
        public void BalanceStaticData_ShouldExist()
        {
            var asset = Resources.Load<ScriptableObject>("StaticData/Balance/Balance");
            asset.Should().NotBeNull("BalanceStaticData must exist at Resources/StaticData/Balance/Balance");
        }

        [Test]
        public void BallChainConfig_ShouldExist()
        {
            var asset = Resources.Load<ScriptableObject>("StaticData/BallChainConfig");
            asset.Should().NotBeNull("BallChainConfig must exist at Resources/StaticData/BallChainConfig");
        }

        [Test]
        public void WindowsStaticData_ShouldExist()
        {
            var asset = Resources.Load<ScriptableObject>("StaticData/WindowsStaticData");
            asset.Should().NotBeNull("WindowsStaticData must exist at Resources/StaticData/WindowsStaticData");
        }

        [Test]
        public void ChapterStaticDataAssets_ShouldExist()
        {
            var assets = Resources.LoadAll<ScriptableObject>("StaticData/Chapters");
            assets.Should().NotBeNull("ChapterStaticData folder must not be empty");
            assets.Should().NotBeEmpty("There should be at least one ChapterStaticData in Resources/StaticData/Chapters");
        }
    }
}