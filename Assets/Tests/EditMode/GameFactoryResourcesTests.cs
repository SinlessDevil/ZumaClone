using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class GameFactoryResourcesTests
    {
        [Test]
        public void PlayerPrefab_ShouldExist()
        {
            var prefab = Resources.Load<GameObject>("Players/Player");
            prefab.Should().NotBeNull("Player prefab must exist at Resources/Players/Player");
        }

        [Test]
        public void BallPrefab_ShouldExist()
        {
            var prefab = Resources.Load<GameObject>("Balls/Ball");
            prefab.Should().NotBeNull("Ball prefab must exist at Resources/Balls/Ball");
        }
    }
}