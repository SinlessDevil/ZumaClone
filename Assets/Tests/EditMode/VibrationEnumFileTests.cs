using System.IO;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class VibrationEnumFileTests
    {
        private const string EnumOutPutPath = "Assets/Code/Services/SFX/Vibration/VibrationType.cs";

        [Test]
        public void VibrationEnumFile_ShouldExist()
        {
            Assert.IsTrue(File.Exists(EnumOutPutPath),
                $"Enum file not found at path: {EnumOutPutPath}");
        }
    }
}