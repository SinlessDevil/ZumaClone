#if UNITY_IOS || UNITY_IPHONE
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

namespace Plugins.CI.Editor
{
    public class IOSBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS)
                PostprocessIOS(report);
        }

        private void PostprocessIOS(BuildReport buildReport)
        {
            string plistPath = buildReport.summary.outputPath + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            PlistElementDict elementDict = plist.root;
            elementDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            
            File.WriteAllText(plistPath, plist.WriteToString());

            string pbxProjectPath = PBXProject.GetPBXProjectPath(buildReport.summary.outputPath);
            var pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(pbxProjectPath));

            string targetGuid = pbxProject.GetUnityFrameworkTargetGuid();
            pbxProject.SetBuildProperty(targetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            File.WriteAllText(pbxProjectPath, pbxProject.WriteToString());
        }
    }
}
#endif