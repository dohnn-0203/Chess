using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Unity -batchmode -quit -executeMethod ChessBuild.Windows -chessBuildPath <exe>
public static class ChessBuild
{
    public static void Windows()
    {
        string[] args = Environment.GetCommandLineArgs();
        string path = "Builds/Windows/Chess.exe";
        bool development = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-chessBuildPath" && i + 1 < args.Length) path = args[++i];
            if (args[i] == "-chessDevelopment") development = true;
        }
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/chess/Scenes/Chess.unity" },
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = development ? BuildOptions.Development : BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Chess build failed: " + report.summary.result);
        UnityEngine.Debug.Log("CHESS_BUILD_SUCCEEDED " + path);
    }
}
