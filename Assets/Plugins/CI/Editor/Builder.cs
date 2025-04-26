using System;
using System.Linq;
using Code.Services.StaticData;
using Code.StaticData;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Plugins.CI.Editor
{
    public static class Builder
    {
        [MenuItem("CI/Build/📦 Android APK Dev")]
        public static void BuildAndroidAPK_Dev()
        {
            PlayerSettings.stripEngineCode = true;
            DisableSplashScreen();
            SetStackTraceLogTypes(gameStaticData => gameStaticData.LogStackTraceDev);
            BuildApk();
        }

        [MenuItem("CI/Build/📦 Android APK Release")]
        public static void BuildAndroidAPK_Release()
        {
            PlayerSettings.stripEngineCode = true;
            DisableSplashScreen();
            SetStackTraceLogTypes(gameStaticData => gameStaticData.LogStackTraceRelease);
            BuildApk();
            BuildAab();
        }

        [MenuItem("CI/Build/🚚 Android Project")]
        public static void BuildAndroidProject()
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            BuildAndroid(Navigator.AndroidProjectPath());
        }


        [MenuItem("CI/Build/🍎 iOS Project Dev")]
        public static void BuildXCodeProject_Dev()
        {
            PlayerSettings.stripEngineCode = true;
            DisableSplashScreen();
            SetStackTraceLogTypes(gameStaticData => gameStaticData.LogStackTraceDev);

            SetPlayerSettingsIOS();
            BuildIOS(Navigator.ApkPath());
        }

        [MenuItem("CI/Build/🍎 iOS Project Release")]
        public static void BuildXCodeProject_Release()
        {
            PlayerSettings.stripEngineCode = true;
            DisableSplashScreen();
            SetStackTraceLogTypes(gameStaticData => gameStaticData.LogStackTraceRelease);

            SetPlayerSettingsIOS();
            BuildIOS(Navigator.ApkPath());
        }
        
        private static void BuildApk()
        {
            BuildAndroidWithConfig(false, Navigator.ApkPath());
        }

        private static void BuildAab()
        {
            BuildAndroidWithConfig(true, Navigator.AabPath());
        }

        private static void BuildAndroidWithConfig(bool isAppBundle, string locationPath)
        {
            SetupKeystore();
            PlayerSettings.Android.splitApplicationBinary = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.buildAppBundle = isAppBundle;
            BuildAndroid(locationPath);
        }
        
        private static void BuildAndroid(string locationPath)
        {
            BuildReport report = Build(new BuildPlayerOptions
            {
                target = BuildTarget.Android,
                locationPathName = locationPath,
                scenes = Scenes(),
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Android Build Failed. See log for details");
        }

        private static void BuildIOS(string locationPath)
        {
            BuildReport report = Build(new BuildPlayerOptions
            {
                target = BuildTarget.iOS,
                locationPathName = locationPath,
                options = BuildOptions.CompressWithLz4HC,
                scenes = Scenes(),
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("iOS Build Failed. See log for details");
        }
        
        private static BuildReport Build(BuildPlayerOptions buildOptions) =>
            BuildPipeline.BuildPlayer(buildOptions);

        private static string[] Scenes() =>
            EditorBuildSettings.scenes
                .Where(x => x.enabled)
                .Select(x => x.path)
                .ToArray();

        private static void SetPlayerSettingsIOS()
        {
            var ciConfig = LoadCIConfig();

            PlayerSettings.iOS.appleEnableAutomaticSigning = ciConfig.UseAutomaticSigning;
            PlayerSettings.iOS.appleDeveloperTeamID = ciConfig.AppleDeveloperTeamID;
        }
        
        private static void SetupKeystore()
        {
            var ciConfig = LoadCIConfig();

            if (ciConfig.UseCustomKeystore)
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ciConfig.KeystoreName;
                PlayerSettings.Android.keystorePass = ciConfig.KeystorePass;
                PlayerSettings.Android.keyaliasName = ciConfig.KeyaliasName;
                PlayerSettings.Android.keyaliasPass = ciConfig.KeyaliasPass;
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
            }
        }

        private static CIConfig LoadCIConfig() =>
            AssetDatabase.LoadAssetAtPath<CIConfig>("Assets/Plugins/CI/CIConfig.asset");
        
        private static void SetStackTraceLogTypes(Func<GameStaticData, LogStackTrace> selectLogTrace)
        {
            StaticDataService staticData = new StaticDataService();
            staticData.LoadData();
            LogStackTrace stackTraceConfig = selectLogTrace(staticData.GameConfig);
            SetStackTraceLogTypes(stackTraceConfig.Info, stackTraceConfig.Errors);
        }

        private static void SetStackTraceLogTypes(StackTraceLogType info, StackTraceLogType errors)
        {
            PlayerSettings.SetStackTraceLogType(LogType.Log, info);
            PlayerSettings.SetStackTraceLogType(LogType.Warning, info);
            PlayerSettings.SetStackTraceLogType(LogType.Error, errors);
            PlayerSettings.SetStackTraceLogType(LogType.Exception, errors);
            PlayerSettings.SetStackTraceLogType(LogType.Assert, errors);
        }

        /// <summary>
        /// Disables Unity splash screen for builds.
        /// Ensures clean startup visuals, regardless of license type.
        /// </summary>
        private static void DisableSplashScreen()
        {
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.logos = Array.Empty<PlayerSettings.SplashScreenLogo>();
        }
    }
}