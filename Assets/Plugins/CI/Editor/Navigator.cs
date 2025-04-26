using System.IO;
using System.Linq;

namespace Plugins.CI.Editor
{
    public class Navigator
    {
        public static string ApkPath() =>
            Path.Combine(ProjectPath(), "artifacts/Game.apk");

        public static string AabPath() =>
            Path.Combine(ProjectPath(), "artifacts/Game.aab");

        public static string AndroidProjectPath() =>
            Path.Combine(ProjectPath(), "artifacts/AndroidProject");

        public static string ProjectPath()
        {
            string path = ".";
            int counter = 0;

            while (!Directory.Exists(Path.Combine(path, ".git")))
            {
                if (path == "")
                    path += "./";
                else if (path == "./")
                    path += "../";
                else
                    path = Path.Combine(path, "..");

                if (counter++ > 6)
                    throw new PathTooLongException($"Git folder is absent or nestered too deep. Current path {path}");
            }

            return path.TrimEnd('/') + "/";
        }
    }
}