using System.IO;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class SoundEnumFilesTests
    {
        private const string Sound2DPath = "Assets/Code/Services/SFX/Sound/Sound2DType.cs";
        private const string Sound3DPath = "Assets/Code/Services/SFX/Sound/Sound3DType.cs";
        private const string MusicPath   = "Assets/Code/Services/SFX/Music/MusicType.cs";

        [Test]
        public void Sound2DType_EnumFile_ShouldExist()
        {
            Assert.IsTrue(File.Exists(Sound2DPath), $"Enum file not found: {Sound2DPath}");
        }

        [Test]
        public void Sound3DType_EnumFile_ShouldExist()
        {
            Assert.IsTrue(File.Exists(Sound3DPath), $"Enum file not found: {Sound3DPath}");
        }

        [Test]
        public void MusicType_EnumFile_ShouldExist()
        {
            Assert.IsTrue(File.Exists(MusicPath), $"Enum file not found: {MusicPath}");
        }
    }
}