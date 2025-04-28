#define NETSTANDARD2_0
#addin nuget:?package=Cake.Unity&version=0.8.1
#addin nuget:?package=JKang.IpcServiceFramework.Client&version=3.1.0
#addin nuget:?package=JKang.IpcServiceFramework.Client.NamedPipe&version=3.1.0
#addin nuget:?package=JKang.IpcServiceFramework.Client.Tcp&version=3.1.0
#addin nuget:?package=JKang.IpcServiceFramework.Core&version=3.1.0
#addin nuget:?package=Microsoft.Extensions.DependencyInjection&version=5.0.1
#addin nuget:?package=Microsoft.Bcl.AsyncInterfaces&version=5.0.0
#addin nuget:?package=Microsoft.Extensions.DependencyInjection.Abstractions&version=5.0.0
#addin nuget:?package=Cake.Git&version=1.1.0
#addin nuget:?package=Cake.Gradle&version=1.1.0
#addin nuget:?package=Cake.XCode&version=5.0.0
#addin nuget:?package=Cake.Yaml&version=4.0.0
#addin nuget:?package=YamlDotNet&version=6.1.2

//#reference "./bot/BotLib.Abstractions.dll"

#load "./BuilderDependencies/TestResultParser.cake"
#load "./BuilderDependencies/UnityLogParser.cake"

using static Cake.Unity.Arguments.BuildTarget;
using System.Runtime;
using System.Diagnostics;
using System.Threading;
//using BotLib.Abstractions;
//using JKang.IpcServiceFramework.Client;
//using JKang.IpcServiceFramework;
//using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Globalization;
using Cake.Common.Build;

var target = Argument("target", "Build-All");
var CurrentDirectory =  System.IO.Directory.GetCurrentDirectory();
var PathToProject = string.Empty;
var isErrorHappend = false;
var apkPath = "artifacts/Game.apk";
var androidProjectPath = "artifacts/AndroidProject";
var testResultPath = "artifacts/tests.xml";
var logPath = "./artifacts/unity.log";
var commitHistory = "";
var git =".git";
string gardleProjectDirectory;

//IIpcClient<ITelegram> client;
//ServiceProvider serviceProvider;

var IsAndroidBuild = false;
var IsIosBuild = false;
int XCDistributionCode;

Task("Load-CI-Settings")
.Does(() =>
{
    VerboseVerbosity();

    string pathToConfig = $"{PathToProject}/Assets/Plugins/CI/CIConfig.asset";
    string[] configLines = System.IO.File.ReadAllLines(pathToConfig);

    IsAndroidBuild = configLines.First(x => x.Contains("IsAndroidBuild")).Split(':')[1].Trim() == "1";
    IsIosBuild = configLines.First(x => x.Contains("IsIosBuild")).Split(':')[1].Trim() == "1";

    Console.WriteLine($"Android is {IsAndroidBuild}. Ios is {IsIosBuild}");
});

Task("Clean-Artifacts-Android")
.WithCriteria(() => IsAndroidBuild, "Android disabled in config")
.Does(() =>
{
    string artifactsPath = "./artifacts";

    if (DirectoryExists(artifactsPath))
    {
        Console.WriteLine($"[INFO] Cleaning artifacts directory: {artifactsPath}");

        foreach (var file in GetFiles($"{artifactsPath}/**/*"))
        {
            try
            {
                System.IO.File.SetAttributes(file.FullPath, FileAttributes.Normal);
                DeleteFile(file);
                Console.WriteLine($"[INFO] Deleted file: {file}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to delete file: {file}. Reason: {ex.Message}");
            }
        }

        foreach (var dir in GetDirectories($"{artifactsPath}/**/*").Reverse())
        {
            try
            {
                DeleteDirectory(dir, new DeleteDirectorySettings { Force = true, Recursive = true });
                Console.WriteLine($"[INFO] Deleted directory: {dir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to delete directory: {dir}. Reason: {ex.Message}");
            }
        }
    }
});

Task("Clean-Artifacts-Ios")
    .WithCriteria(() => IsIosBuild, "iOS disabled in config")
    .Does(() =>
{
    string artifactsPath = "./artifacts";

    if (DirectoryExists(artifactsPath))
    {
        Console.WriteLine($"[INFO] Cleaning artifacts directory for iOS: {artifactsPath}");

        foreach (var file in GetFiles($"{artifactsPath}/**/*"))
        {
            try
            {
                DeleteFile(file);
                Console.WriteLine($"[INFO] Deleted file: {file}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to delete file: {file}. Reason: {ex.Message}");
            }
        }

        foreach (var dir in GetDirectories($"{artifactsPath}/**/*").Reverse())
        {
            try
            {
                DeleteDirectory(dir, new DeleteDirectorySettings { Force = true, Recursive = true });
                Console.WriteLine($"[INFO] Deleted directory: {dir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to delete directory: {dir}. Reason: {ex.Message}");
            }
        }
    }
});

Task("Find-Project")
    .Does(() =>
{
    PathToProject = GetPathToProject();
});

Task("Build-Commit-History")
    .Does(() =>
{
    //commitHistory = CommitHistory();
    Console.WriteLine(LogMessage());
});

Task("Update-Project-Property-Android")
.WithCriteria(() => IsAndroidBuild, "Android disabled in config")
    .Does(() =>
{
    KeyValuePair<string,string>[] properties = new KeyValuePair<string, string>[]
    {
        new KeyValuePair<string,string>("bundleVersion", $"{Version()}"),
        new KeyValuePair<string,string>("iPhone", $"{BuildCode()}"),
        new KeyValuePair<string,string>("AndroidBundleVersionCode", $"{BuildCode()}"),
    };

    KeyValuePair<string,int>[] ignore = new KeyValuePair<string, int>[]
    {
        new KeyValuePair<string,int>("mobileMTRendering", 3),
        new KeyValuePair<string,int>("applicationIdentifier", 2),
    };

    SetProjectProperty(properties, ignore);
});

Task("Update-Project-Property-Ios")
.WithCriteria(() => IsIosBuild, "Ios disabled in config")
    .Does(() =>
{
    KeyValuePair<string,string>[] properties = new KeyValuePair<string, string>[]
    {
        new KeyValuePair<string,string>("bundleVersion", $"{UtcDateTime()}"),
        new KeyValuePair<string,string>("iPhone", $"{BuildCode()}"),
        new KeyValuePair<string,string>("AndroidBundleVersionCode", $"{BuildCode()}"),
    };

    KeyValuePair<string,int>[] ignore = new KeyValuePair<string, int>[]
    {
        new KeyValuePair<string,int>("mobileMTRendering", 3),
        new KeyValuePair<string,int>("applicationIdentifier", 2),
    };

    SetProjectProperty(properties, ignore);
});

/*
Task("Connect-To-Bot")
    .Does(() => 
{
    serviceProvider = new ServiceCollection()
        .AddTcpIpcClient<ITelegram>("client1",System.Net.IPAddress.Loopback, 8081)
        .BuildServiceProvider();

    IIpcClientFactory<ITelegram> clientFactory = serviceProvider
        .GetRequiredService<IIpcClientFactory<ITelegram>>();

    client = clientFactory.CreateClient("client1");
    
    Console.WriteLine("Sucsessfuly connect to bot");
})
.OnError(exception => 
{
    DisplayError(exception);
});
*/

/*
Task("Send-Welcome-Message")
    .Does(() => 
{
  Task<string> helloMessageId =
     client.InvokeAsync(x 
        => x.SendMessage(
            LogMessage(),
             RepoUrl()));

  TimeSpan timeSpan = new TimeSpan(0, 1, 0);
  
  if (!helloMessageId.Wait(timeSpan))
    throw new TimeoutException($"SendFile Timeout {timeSpan}");

  Console.WriteLine("Hello message send");
})
.OnError(exception => 
{
    DisplayError(exception);
});
*/

Task("Run-Android-Tests")
    .Does(() =>
    {
        EnsureDirectoryExists("./artifacts");

        string logPath = "./artifacts/unity.log";

        if (!System.IO.File.Exists(logPath))
        {
            Console.WriteLine("[WARNING] No unity.log file found. Skipping test result parsing.");
            return;
        }

        string testSummary = "";

        try
        {
            testSummary = ParseTestResult(testResultPath);
            Console.WriteLine("[INFO] Test summary:");
            Console.WriteLine(testSummary);
        }
        catch (System.Exception e)
        {
            Console.WriteLine("[ERROR] Failed to parse test results:");
            Console.WriteLine(e);
            isErrorHappend = true;
        }
    });

Task("Send-Erorr-Logs")
.WithCriteria(() => isErrorHappend)
.Does(() =>
{
    string relativePath = "artifacts/unity.log";
    string path = System.IO.Path.Combine(CurrentDirectory, relativePath);

    if (!System.IO.File.Exists(path))
    {
        Console.WriteLine($"[WARNING] Unity log file {path} not found. Skipping log sending.");
        return;
    }

    Console.WriteLine($"Start sending from {path}");

    string caption = ParseUnityLogError(relativePath);
    Console.WriteLine(caption);

    //Task output = client.InvokeAsync(x => x.SendFile(path, "unity.log", caption, RepoUrl()));

    Console.WriteLine($"[CI][Log] Would send log: '{path}' with caption: {caption}");

    TimeSpan timeSpan = new TimeSpan(0, 5, 0);
    
    //if (!output.Wait(timeSpan))
    //    throw new TimeoutException($"SendFile Timeout {timeSpan}");

    throw new CakeException("Build end with error!");
});

Task("Share-Apk")
.WithCriteria(() => IsAndroidBuild, "Android disabled in config")
.WithCriteria(() => !isErrorHappend, "Error Happend")
    .Does(() => 
{
    string relativePath = "./artifacts/Game.apk/launcher/build/outputs/apk/release/launcher-release.apk";
    string rootPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "artifacts");

    foreach(string path in System.IO.Directory.GetFiles(rootPath, "*.apk").Concat(System.IO.Directory.GetFiles(rootPath, "*.aab")).Concat(System.IO.Directory.GetFiles(rootPath, "*.obb")))
    {
        Console.WriteLine($"Start sending from {path}");

        //Task<string> output = client.InvokeAsync(x => x.SendFile(path, ApkName(System.IO.Path.GetExtension(path)), RepoUrl()));
        
        var fileName = ApkName(System.IO.Path.GetExtension(path));
        Console.WriteLine($"[CI][APK] Would send file: '{path}' as '{fileName}' to repo: {RepoUrl()}");

        TimeSpan timeSpan = new TimeSpan(0, 10, 0);
    
        //if (!output.Wait(timeSpan))
        //    throw new TimeoutException($"SendFile Timeout {timeSpan}");
    }

    SaveLastCommitSha();
});

/*
Task("Send-Success-Message")
.WithCriteria(() => IsIosBuild, "Ios disabled in config")
.WithCriteria(() => !isErrorHappend, "Error")
    .Does(() => 
{
  Task<string> helloMessageId =
     client.InvokeAsync(x 
        => x.SendMessage(
            "TestFlight Upload ===> " + (XCDistributionCode == 0 ? "OK :)" : $"Erorr {XCDistributionCode}"),
             RepoUrl()));

  TimeSpan timeSpan = new TimeSpan(0, 1, 0);
  
  if (!helloMessageId.Wait(timeSpan))
    throw new TimeoutException($"SendFile Timeout {timeSpan}");

  Console.WriteLine("Hello message send");
})
.OnError(exception => 
{
    DisplayError(exception);
});
*/

Task("Send-Telegram-Message")
    .Does(() =>
{
    Console.WriteLine("Sending Telegram message...");

    StartProcess("powershell", new ProcessSettings {
        Arguments = "-ExecutionPolicy Bypass -File ./send_telegram.ps1"
    });
});

Task("Build-Android")
.WithCriteria(() => IsAndroidBuild, "Android disabled in config")
.IsDependentOn("Clean-Artifacts-Android")
.IsDependentOn("Find-Project")
.IsDependentOn("Build-Commit-History")
.IsDependentOn("Update-Project-Property-Android")
//.IsDependentOn("Connect-To-Bot")
//.IsDependentOn("Send-Welcome-Message")
//.IsDependentOn("Run-Android-Tests")
.IsDependentOn("Send-Erorr-Logs")
.IsDependentOn("Share-Apk")
.Does(() =>
{
});

Task("Build-iOS")
.WithCriteria(() => IsIosBuild, "Ios disabled in config")
.IsDependentOn("Clean-Artifacts-Ios")
.IsDependentOn("Find-Project")
.IsDependentOn("Update-Project-Property-Ios")
//.IsDependentOn("Send-Success-Message")
.Does(() =>
{
});

Task("Build-All")
.IsDependentOn("Find-Project")
.IsDependentOn("Load-CI-Settings")
.IsDependentOn("Build-Android")
.IsDependentOn("Build-iOS")
.IsDependentOn("Send-Telegram-Message")
.Does(() =>
{
})
.Finally(() =>
{
})
.IsDependentOn("Cleanup");

Task("Cleanup")
    .Does(() => 
{
    //if(serviceProvider != null)
    //    serviceProvider.Dispose();
});

void DisplayError(Exception exception)
{
    Console.WriteLine("-----Error-----");
    Console.WriteLine(exception.Message);
    Console.WriteLine(exception.StackTrace); 
    Console.WriteLine("---------------");
}

string GetPathToProject()
{
    if(!UnityProjectPath(".", out string projectPath))
        throw new PathTooLongException("Project folder is absent or nestered too deep");

    return projectPath;
}

bool UnityProjectPath(string path, out string output)
{
output = path;

if(
    System.IO.Directory.Exists(System.IO.Path.Combine(path, "Assets")) &&
    System.IO.Directory.Exists( System.IO.Path.Combine(path, "ProjectSettings")))  
 {
     return true;
 }

 if(System.IO.Path.GetFileName(path) == ".git")
    return false;

    
    foreach(string directory in System.IO.Directory.GetDirectories(path))
    {
        string directoryName = System.IO.Path.GetFileName(directory);
        string testedPath = System.IO.Path.Combine(path, directoryName);
        if(directoryName != "." && UnityProjectPath(testedPath, out output))
            return true;
    }

    return false;
}

void SetProjectProperty(KeyValuePair<string,string>[] properies, KeyValuePair<string,int>[] ignore)
{
    string pathToSettings = PathToProject + "/ProjectSettings/ProjectSettings.asset";

    Console.WriteLine($"Read {pathToSettings}");

    int needIngnore = 0;
    string[] updatedLines = System.IO.File.ReadAllLines(pathToSettings)
    .Select(x =>
     {
         if(needIngnore > 0)
         {
                Console.WriteLine($"Skip {needIngnore}");
                needIngnore--;
                return x;
         }

        string lineKey = x.Split(':')[0].Trim(' ');

        KeyValuePair<string,int> inoreSetting = ignore.FirstOrDefault(x => x.Key == lineKey);

         if(inoreSetting.Key == lineKey)
         {
             needIngnore = inoreSetting.Value;
            Console.WriteLine($"Ignore {inoreSetting.Key}. Steps {inoreSetting.Value}");
            return x;
         }

        
         KeyValuePair<string,string> prop = properies.FirstOrDefault(x => x.Key == lineKey);

         if(prop.Key == lineKey)
         {
            Console.WriteLine($"Updated {prop.Key}. Value {prop.Value}");

            int spaceCount =
            x
            .TakeWhile(ch => ch == ' ')
            .Count();

            string spaces =new string(Enumerable.Repeat(' ', spaceCount).ToArray());
            return $"{spaces}{prop.Key}: {prop.Value}";
         }
            
         
         return x;
    })
    .ToArray();

    Console.WriteLine($"Write {pathToSettings}");

    if (System.IO.File.Exists(pathToSettings))
    {
        System.IO.File.SetAttributes(pathToSettings, FileAttributes.Normal);
        var tempPath = pathToSettings + ".temp";
        System.IO.File.Copy(pathToSettings, tempPath, true);
        System.IO.File.Delete(pathToSettings);
        System.IO.File.Move(tempPath, pathToSettings);
    }

    System.IO.File.WriteAllLines(pathToSettings, updatedLines);
}

string GetProjectPropertyLine(string key)
{
    string pathToSettings = PathToProject + "/ProjectSettings/ProjectSettings.asset";

   return System.IO.File.ReadAllLines(pathToSettings)
    .First(x => x.Split(':')[0].Trim(' ') == key);
}

string GetProjectPropertyValue(string key) =>
    GetProjectPropertyLine(key)
    .Split(':')
    [1]
    .Trim(' ');

string ApkName(string ext) =>
    $"{ProductName()}_{Version()}{ext}";

string Version() =>
 $"{UtcDateTime()}:{BranchName()}-{CommitsTodayHead()}";

 string BuildCode() =>
 $"{UtcDateTime()}.{CommitsTodayTotal()}";

string UtcDateTime() =>
 $"{DateTime.UtcNow:yy.MM.dd}";

 string ProductName() =>
 GetProjectPropertyValue("productName").Replace(" ", "_");

 string BranchName() => 
    GitBranchCurrent(".git").FriendlyName;

 string CommitHistory() 
 {
     string[] version = GetProjectPropertyValue("bundleVersion").Split('.');
    int commitsInLastBuild = int.Parse(version[3]);

     DateTime versionDateTime = DateTime.Parse($"20{version[0]}.{version[1]}.{version[2]}");
     DateTimeOffset lastBuildDate = new DateTimeOffset(new DateTime(versionDateTime.Year, versionDateTime.Month, versionDateTime.Day, 0, 0, 0));

    List<GitCommit> newCommits = new List<GitCommit>();
    
    IEnumerable<GitCommit> commits = GitLog(git, 50).Where(x => x.Author.When.UtcDateTime > lastBuildDate);

    newCommits.AddRange(commits.Where(x => x.Author.When.UtcDateTime.Day != lastBuildDate.Day));
    newCommits.AddRange(commits.Where(x => x.Author.When.UtcDateTime.Day == lastBuildDate.Day).Reverse().Skip(commitsInLastBuild).Reverse());
    
    string history = "";
    
    foreach(var newCommit in newCommits)
    {
        DateTime date = newCommit.Author.When.UtcDateTime;
        history += $"- {newCommit.Author.Name} [{date.Month:00}.{date.Day:00} {date.Hour:00}:{date.Minute:00}] -> {newCommit.Message.Trim('\n')}\n\r";
    }

    return history.Trim(' ', '\n');
 }

string DiffMessage()
{
    string branch = GitBranchCurrent(git).FriendlyName;
    string lastInformedCommitSha = LoadLastInformedSha(branch);

    
    ICollection<GitCommit> lastCommits = GitLog(git, 50);
    GitCommit lastInformedCommit = lastCommits.FirstOrDefault(x => x.Sha == lastInformedCommitSha);
    if(lastInformedCommit == null)
        return "\nFirst build!";
    
    IEnumerable<GitCommit> newCommits = lastCommits.Where(x => x.Author.When.UtcDateTime > lastInformedCommit.Author.When.UtcDateTime);

    string message = "";
    foreach(var commit in newCommits)
        message += $"• {commit.Author.Name.Take(3).Select(x => x.ToString()).Aggregate((message, next) => message += next)}: "+ commit.MessageShort + "\n\r";
    
    message.Trim('\r');
    message.Trim('\n');

    return message;
}

string LoadLastInformedSha(string branch)
{
    if (!System.IO.File.Exists("./last.sha"))
        return "";

    string[] lines = System.IO.File.ReadAllLines("./last.sha");
    
    if(lines.Any(line => line.StartsWith(branch)))
        return lines.First(line => line.StartsWith(branch)).Split(' ')[1];

    if(lines.Any(line => line.StartsWith("main")))
        return lines.First(line => line.StartsWith("main")).Split(' ')[1];

    if(lines.Any(line => line.StartsWith("master")))
        return lines.First(line => line.StartsWith("master")).Split(' ')[1];

    return "";
}

void SaveLastCommitSha()
{
    GitCommit lastCommit = GitLog(git, 1).FirstOrDefault();

    if (lastCommit == null)
        return;

    string branchName = GitBranchCurrent(git).FriendlyName;

    if (System.IO.File.Exists("./last.sha"))
    {
        List<string> lines = new List<string>();
        lines.AddRange(System.IO.File.ReadAllLines("./last.sha"));

        lines.RemoveAll(line => line.StartsWith(branchName));
            
        lines.Add($"{branchName} {lastCommit.Sha}");

        System.IO.File.WriteAllLines("./last.sha", lines);

        return;
    }

    
    System.IO.File.WriteAllText("./last.sha", $"{branchName} {lastCommit.Sha}");
}

int CommitsTodayHead()
{
      DateTime now = DateTime.UtcNow;
    DateTimeOffset today = new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, 0, 0, 0));
    
    string command = @"log --after='"+ToRFC822DateUTC(today.DateTime) + @"' --oneline";

    return RunGitCommand(command).Length;
}

int CommitsTodayTotal()
{
    DateTime now = DateTime.UtcNow;
    DateTimeOffset today = new DateTimeOffset(new DateTime(now.Year, now.Month, now.Day, 0, 0, 0));

    string command = @"log --after='"+ToRFC822DateUTC(today.DateTime) + @"' --oneline --all";

    return RunGitCommand(command).Length;

}

string[] RunGitCommand(string command)
{
    Console.WriteLine(command);

    Process process = new Process();
    process.StartInfo.RedirectStandardOutput = true;

    process.StartInfo.FileName = "git";
    process.StartInfo.Arguments = command;
    process.Start();

    StreamReader gitAnswerReader = process.StandardOutput;

    List<string> output = new List<string>();

    while(!gitAnswerReader.EndOfStream)
        output.Add(gitAnswerReader.ReadLine());

    return output.ToArray();
}

string RepoUrl() =>
    GitBranchCurrent(git).Remotes.First().Url;

string RepoBranch() =>
    GitBranchCurrent(git).FriendlyName;

string LogMessage() =>
    $"{Platforms()}\n\r{ProductName()} is building from {BranchName()}\n\r{DiffMessage()}\n\rVersion: {Version()}\n\rBuild Code: {BuildCode()}";

string Platforms() =>
    (IsAndroidBuild ? "🤖" : "") + (IsIosBuild? "🍎" : "");

public static string ToRFC822Date(this DateTime date)
{
    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    int offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours;
    string timeZone = "+" + offset.ToString().PadLeft(2, '0');
 
    if (offset < 0)
    {
        int i = offset * -1;
        timeZone = "-" + i.ToString().PadLeft(2, '0');
    }
 
    return date.ToString("ddd, dd MMM yyyy HH:mm:ss " + timeZone.PadRight(5, '0'));
}

public static string ToRFC822DateUTC(this DateTime date)
{
    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    int offset = 0;
    string timeZone = "+" + offset.ToString().PadLeft(2, '0');
 
    if (offset < 0)
    {
        int i = offset * -1;
        timeZone = "-" + i.ToString().PadLeft(2, '0');
    }
 
    return date.ToString("dd_MMM_yyyy_HH:mm:ss_" + timeZone.PadRight(5, '0'));
}

RunTarget(target);

class CIConfig
{
    public bool IsAndroidBuild;
    public bool IsIosBuild;
}