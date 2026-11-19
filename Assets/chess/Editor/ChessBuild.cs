using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Unity -batchmode -quit -executeMethod ChessBuild.Windows -chessBuildPath <exe>
public static class ChessBuild
{
    // Unity -batchmode -quit -buildTarget WebGL -executeMethod ChessBuild.WebGL -chessBuildPath <directory>
    public static void WebGL()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new Exception("WebGL Build Support is missing. Install the module for this Unity Editor version.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            throw new Exception("Select WebGL before building, or pass -buildTarget WebGL when starting Unity.");

        string[] args = Environment.GetCommandLineArgs();
        string path = "Builds/WebGL";
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "-chessBuildPath" && i + 1 < args.Length) path = args[++i];
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(path);

        var compression = PlayerSettings.WebGL.compressionFormat;
        bool fallback = PlayerSettings.WebGL.decompressionFallback;
        bool threads = PlayerSettings.WebGL.threadsSupport;
        string template = PlayerSettings.WebGL.template;
        var exceptions = PlayerSettings.WebGL.exceptionSupport;
        var initialMemory = PlayerSettings.WebGL.initialMemorySize;
        var maximumMemory = PlayerSettings.WebGL.maximumMemorySize;
        var memoryGrowth = PlayerSettings.WebGL.memoryGrowthMode;
        try
        {
            // GitHub Pages는 파일별 Content-Encoding을 설정할 수 없어 로더에서 압축을 해제한다.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.template = "APPLICATION:Minimal";
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 512;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/chess/Scenes/Chess.unity" },
                locationPathName = path,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Chess WebGL build failed: " + report.summary.result
                    + ", errors: " + report.summary.totalErrors + ". Check the Unity build log.");
            UnityEngine.Debug.Log("CHESS_WEBGL_BUILD_SUCCEEDED " + path);
        }
        finally
        {
            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.decompressionFallback = fallback;
            PlayerSettings.WebGL.threadsSupport = threads;
            PlayerSettings.WebGL.template = template;
            PlayerSettings.WebGL.exceptionSupport = exceptions;
            PlayerSettings.WebGL.maximumMemorySize = maximumMemory;
            PlayerSettings.WebGL.initialMemorySize = initialMemory;
            PlayerSettings.WebGL.memoryGrowthMode = memoryGrowth;
        }
    }

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
