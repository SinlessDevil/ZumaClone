using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class UIFactoryResourcesTests
    {
        [Test]
        public void Resource_UiRoot_ShouldExist()
        {
            var prefab = Resources.Load<GameObject>("UI/UiRoot");
            prefab.Should().NotBeNull("UiRoot prefab must exist at Resources/UI/UiRoot");
        }

        [Test]
        public void Resource_GameHud_ShouldExist()
        {
            var prefab = Resources.Load<GameObject>("Huds/GameHud");
            prefab.Should().NotBeNull("GameHud prefab must exist at Resources/Huds/GameHud");
        }

        [Test]
        public void Resource_MenuHud_ShouldExist()
        {
            var prefab = Resources.Load<GameObject>("Huds/MenuHud");
            prefab.Should().NotBeNull("MenuHud prefab must exist at Resources/Huds/MenuHud");
        }

        [Test]
        public void Resource_Widget_ShouldExist()
        {
            var prefab = Resources.Load<GameObject>("UI/Widget");
            prefab.Should().NotBeNull("Widget prefab must exist at Resources/UI/Widget");
        }

        [Test]
        public void Resource_ItemLevel_ShouldExist()
        {
            var prefab = Resources.Load<GameObject>("UI/Menu/ItemLevel");
            prefab.Should().NotBeNull("ItemLevel prefab must exist at Resources/UI/Menu/ItemLevel");
        }

        [Test]
        public void Resource_StartLevelInfoDisplayer_ShouldExist()
        {
            var prefab = Resources.Load<GameObject>("UI/StartLevelInfo");
            prefab.Should().NotBeNull("StartLevelInfo prefab must exist at Resources/UI/StartLevelInfo");
        }
    }
}
