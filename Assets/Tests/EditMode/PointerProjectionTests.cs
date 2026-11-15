using Code.Extensions;
using Code.Services.Input;
using Code.StaticData;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class PointerProjectionTests
    {
        [Test]
        public void GameplayInputArea_RespectsScreenSizeAndDeviceSafeArea()
        {
            GameplayInputStaticData config = ScriptableObject.CreateInstance<GameplayInputStaticData>();
            try
            {
                Rect screen = new(0f, 0f, 1920f, 1080f);
                Rect safeArea = new(80f, 24f, 1740f, 1056f);
                Rect area = config.GetGameplayInputArea(screen, safeArea);
                Assert.That(area, Is.EqualTo(Rect.MinMaxRect(80f, 24f, 1820f, 945f)));
                Assert.That(area.Contains(new Vector2(960f, 500f)), Is.True);
                Assert.That(area.Contains(new Vector2(960f, 1000f)), Is.False);
                Assert.That(area.Contains(new Vector2(10f, 500f)), Is.False);

                config.GameplayInputArea = new Rect(-0.2f, 0.5f, 0.75f, 0.75f);
                Assert.That(config.GetGameplayInputArea(screen, screen),
                    Is.EqualTo(Rect.MinMaxRect(0f, 270f, 1440f, 1080f)));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ScreenPointer_ProjectsAtTheShootersDepth()
        {
            GameObject cameraObject = new("Pointer projection test camera");
            Camera previousMainCamera = Camera.main;
            bool previousCameraEnabled = previousMainCamera != null && previousMainCamera.enabled;
            try
            {
                if (previousCameraEnabled)
                    previousMainCamera.enabled = false;

                cameraObject.tag = "MainCamera";
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.transform.SetPositionAndRotation(new Vector3(0f, 16f, 0f), Quaternion.Euler(90f, 0f, 0f));
                Vector3 shooter = new(-3f, 0f, 1f);
                Vector3 target = new(4f, 0f, -2f);
                Vector3 screen = camera.WorldToScreenPoint(target);
                IInputService input = Substitute.For<IInputService>();
                input.TouchPosition.Returns(new Vector3(screen.x, screen.y, 0f));

                Vector3 projected = input.GetWorldPointerPosition(shooter);

                Assert.That(Vector3.Distance(projected, target), Is.LessThan(0.001f));
                Assert.That(input.GetWorldDistanceTo(shooter), Is.EqualTo(Vector3.Distance(shooter, target)).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                if (previousCameraEnabled)
                    previousMainCamera.enabled = true;
            }
        }
    }
}
