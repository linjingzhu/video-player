using System.Globalization;
using System.Text;

namespace VideoPlayer.Core.Safety;

/// <summary>
/// Append-only crash log under AppData. Logging must not throw: a full disk
/// or a locked file is not another crash.
/// </summary>
public static class CrashLog
{
    public const string FolderName = "VideoPlayer";
    public const string FileName = "crash.log";
    public const string BannerText = "예기치 않은 오류";

    public static string DefaultDirectory()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName);

    public static string FormatEntry(DateTimeOffset when, string source, Exception? exception, bool terminating = false)
    {
        var header = new StringBuilder();
        header.Append(when.ToString("yyyy-MM-dd HH:mm:ss.fffK", CultureInfo.InvariantCulture));
        header.Append(" [");
        header.Append(string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim());
        header.Append(']');
        if (terminating)
        {
            header.Append(" terminating");
        }

        if (exception is null)
        {
            header.Append(" (no exception)");
            header.AppendLine();
            return header.ToString();
        }

        header.Append(' ');
        header.Append(exception.GetType().FullName);
        header.Append(": ");
        header.AppendLine(exception.Message);
        header.AppendLine(exception.ToString());
        return header.ToString();
    }

    public static bool TryAppend(string directory, string entry)
    {
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrEmpty(entry))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, FileName), entry);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }
}
