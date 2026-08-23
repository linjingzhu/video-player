using System.Diagnostics;
using System.IO;
using VideoPlayer.Core.Clip;
using Path = System.IO.Path;

namespace VideoPlayer.App.Clip;

public sealed class FfmpegClipRunner : IClipProcessRunner
{
    public FfmpegClipRunner(string? executable = null)
    {
        Executable = executable ?? Find();
    }

    public string? Executable { get; }

    public ClipProcessResult Run(string executable, IReadOnlyList<string> arguments)
    {
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }

            using var process = Process.Start(start);
            if (process is null)
            {
                return new ClipProcessResult(false, -1, "ffmpeg");
            }

            process.WaitForExit(5 * 60 * 1000);
            var error = process.StandardError.ReadToEnd();
            return new ClipProcessResult(process.ExitCode == 0, process.ExitCode, error);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new ClipProcessResult(false, -1, ex.Message);
        }
    }

    public static string? Find(string? baseDirectory = null, string? pathEnv = null)
    {
        var root = baseDirectory ?? AppContext.BaseDirectory;
        foreach (var name in new[] { "ffmpeg.exe", "ffmpeg" })
        {
            var local = Path.Combine(root, name);
            if (File.Exists(local))
            {
                return local;
            }
        }

        var path = pathEnv ?? Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in new[] { "ffmpeg.exe", "ffmpeg" })
            {
                var candidate = Path.Combine(directory, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
