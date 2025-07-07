using Code.StaticData;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class LevelDataValidationTests
    {
        [Test]
        public void GameConfigCompiling()
        {
            GameConfigSettings().MakeBuild.Should().BeTrue();
        }
        
        private static GameStaticData GameConfigSettings() => Resources.Load<GameStaticData>("StaticData/Balance/GameConfig");
    }
}