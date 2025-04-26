using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Plugins.CI.Editor
{
    [InitializeOnLoad]
    public static class Installer
    {
        private const string SuccessMessage =
            "CI successfully installed! Call @kekril or @utincomputer in Telegram for activation automatic builds";

        static Installer()
        {
            if (!IsInstalled())
                AutoInstall();
        }

        [MenuItem("CI/⚙️Config")]
        public static void OpenCiConfig()
        {
            CIConfig config = LoadCiConfig();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        private static CIConfig LoadCiConfig()
        {
            CIConfig config = AssetDatabase.LoadAssetAtPath<CIConfig>("Assets/Plugins/CI/CIConfig.asset");
            return config;
        }

        [MenuItem("CI/🎁Export", priority = 1002)]
        public static void ExportTemplate()
        {
            const string root = @"./";

            var noAddRegex = new List<PathFilter>
            {
                new PathFilter(@"^(?!.*\bAsset\b).*\.csproj$"),
                new PathFilter(@"^(?!.*\bAsset\b).*\.sln$"),
                new PathFilter(@"^(?!.*\bAsset\b).*\.DotSettings$"),
            };

            var noAddPaths = new List<Func<string, bool>>
            {
                path => path.StartsWith(Path.Combine(root, ".git", " ").TrimEnd()),
                path => path.StartsWith(Path.Combine(root, ".idea")),
                path => path.StartsWith(Path.Combine(root, "artifacts")),
                path => path.StartsWith(Path.Combine(root, "README.md")),
                path => path.StartsWith(Path.Combine(root, "build.ps1")),
                path => path.StartsWith(Path.Combine(root, "Builds")),
                path => path.StartsWith(Path.Combine(root, "Logs")),
                path => path.StartsWith(Path.Combine(root, "releases")),
                path => path.StartsWith(Path.Combine(root, "tools")),
                path => path.StartsWith(Path.Combine(root, "Temp")),
                path => path.StartsWith(Path.Combine(root, "Library", "Build")),
                path => path.StartsWith(Path.Combine(root, "Library", "il2cpp")),
                path => path.StartsWith(Path.Combine(root, "Library", "Il2cpp")),
                path => path.StartsWith(Path.Combine(root, "Library", "com.unity.ide.rider")),
            };

            IEnumerable<string> filesToArchive =
                AllFiles(@in: root)
                    .Where(path =>
                    {
                        return noAddPaths.All(x => x(path) == false) &&
                               noAddRegex.All(x => x.IsMatch(path) == false);
                    })
                    .ToList();

            string
                tempFolder = "./TempFolderToExptortSDK/"; //Path.Combine(Path.GetTempPath(), "TempFolderToExptortSDK");

            if (Directory.Exists(tempFolder))
            {
                foreach (string file in AllFiles(@in: tempFolder))
                {
                    File.Delete(file);
                }

                foreach (string directory in AllDirectories(tempFolder))
                {
                    Directory.Delete(directory);
                }

                Directory.Delete(tempFolder);
            }

            foreach (string file in filesToArchive)
            {
                string tempPath = Path.Combine(tempFolder, file.TrimStart('.').TrimStart('/'));

                string directoryPath = Path.GetDirectoryName(tempPath);

                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                try
                {
                    File.Copy(file, tempPath);
                }
                catch (IOException e)
                {
                    Debug.Log(e);
                }
            }

            FileStream fileStream = File.Create(Path.Combine(tempFolder, "last.sha"));

            string message = "85cd5209c843c464eaca5b8c8812772ab87b246b";
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            fileStream.Write(bytes, 0, bytes.Length);
            fileStream.Close();
        }

        private static IEnumerable<string> AllFiles(string @in)
        {
            foreach (string file in Directory.EnumerateFiles(@in))
            {
                yield return file;
            }

            foreach (string directory in Directory.EnumerateDirectories(@in))
            {
                foreach (string file in AllFiles(directory))
                {
                    yield return file;
                }
            }
        }

        private static IEnumerable<string> AllDirectories(string @in)
        {
            foreach (string directory in Directory.EnumerateDirectories(@in))
            {
                foreach (string nestedDirectory in AllDirectories(directory))
                {
                    yield return nestedDirectory;
                }

                yield return directory;
            }
        }

        [MenuItem("CI/🛠 Install")]
        public static void Install()
        {
            using (FileStream fileStream = new FileStream("Assets/Plugins/CI/CI.zip", FileMode.Open))
            {
                using (ZipFile zip = new ZipFile(fileStream))
                {
                    foreach (ZipEntry entry in zip)
                    {
                        string entryName = entry.Name;

                        if (!IsValidFile(entry))
                            continue;

                        Debug.Log("Unpacking zip file entry: " + entryName);

                        byte[] buffer = new byte[4096];
                        Stream zipStream = zip.GetInputStream(entry);

                        string unzipPath = Path.Combine(Navigator.ProjectPath() + entryName);
                        Debug.Log("Unpacking file path: " + unzipPath);

                        string directoryName = Path.GetDirectoryName(unzipPath);
                        if (!Directory.Exists(directoryName) && directoryName != String.Empty)
                            Directory.CreateDirectory(directoryName);

                        using (FileStream streamWriter = File.Create(unzipPath))
                            StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }

                PatchGitignore();

                Debug.Log(SuccessMessage);
            }

            bool IsValidFile(ZipEntry entry) => !entry.Name.Contains("DS_Store") && entry.IsFile;
        }

        private static void PatchGitignore()
        {
            string[] gitignore = File.ReadAllLines(Path.Combine(Navigator.ProjectPath(), ".gitignore"));

            bool hasArtifacts = gitignore.Contains("/artifacts");
            bool hasTools = gitignore.Contains("/tools");
            bool hasGradle = gitignore.Contains("/.gradle");

            if (hasArtifacts && hasTools && hasGradle)
                return;

            using (StreamWriter writer = new StreamWriter(Path.Combine(Navigator.ProjectPath(), ".gitignore"), true))
            {
                writer.WriteLine();
                writer.WriteLine();
                writer.WriteLine("#Utin CI");

                if (!hasArtifacts)
                    writer.WriteLine("/artifacts");
                if (!hasTools)
                    writer.WriteLine("/tools");
                if (!hasGradle)
                    writer.WriteLine("/.gradle");
            }
        }

        private static void AutoInstall()
        {
            try
            {
                GUIInstall();
            }
            catch (Exception e)
            {
                ShowError();
                throw;
            }
        }

        private static void ShowError() =>
            EditorUtility.DisplayDialog("Installation",
                $"CI installation went wrong. Please call @kekril in Telegram",
                "Close");

        private static void GUIInstall()
        {
            Install();
            EditorUtility.DisplayDialog("Installation", SuccessMessage, "Close");
        }

        private static bool IsInstalled() =>
            File.Exists(Path.Combine(Navigator.ProjectPath(), "build.cake"));
    }
}