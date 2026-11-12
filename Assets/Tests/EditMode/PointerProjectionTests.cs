using Code.Extensions;
using Code.Services.Input;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class PointerProjectionTests
    {
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
