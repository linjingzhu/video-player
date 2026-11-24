using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Playback;

/// <summary>
/// YouTube watch URLs streamed through libmpv's ytdl hook. The resume identity is the
/// 11-character video id so youtu.be, shorts, and a watch URL with list= share one position.
/// </summary>
public static class YouTubeUrl
{
    public static bool IsYouTube(string? url) => TryGetVideoId(url) is not null;

    public static string? TryGetVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var host = uri.IdnHost;
        if (IsHost(host, "youtu.be") || IsHost(host, "www.youtu.be"))
        {
            return IdFromSegment(uri.AbsolutePath, index: 0);
        }

        if (IsWatchHost(host) && IsWatchPath(uri.AbsolutePath))
        {
            return NormalizeId(QueryValue(uri, "v"));
        }

        if (IsShortsHost(host))
        {
            return IdFromSegment(uri.AbsolutePath, index: 1, folder: "shorts");
        }

        return null;
    }

    private static bool IsWatchHost(string host)
        => IsHost(host, "youtube.com")
           || IsHost(host, "www.youtube.com")
           || IsHost(host, "m.youtube.com")
           || IsHost(host, "music.youtube.com");

    private static bool IsShortsHost(string host)
        => IsHost(host, "youtube.com")
           || IsHost(host, "www.youtube.com")
           || IsHost(host, "m.youtube.com");

    private static bool IsHost(string host, string expected)
        => host.Equals(expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsWatchPath(string absolutePath)
        => absolutePath.TrimEnd('/').Equals("/watch", StringComparison.OrdinalIgnoreCase);

    private static string? IdFromSegment(string absolutePath, int index, string? folder = null)
    {
        var parts = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (folder is not null)
        {
            if (parts.Length < 2 || !parts[0].Equals(folder, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return index >= 0 && index < parts.Length ? NormalizeId(parts[index]) : null;
    }

    private static string? QueryValue(Uri uri, string key)
    {
        var query = uri.Query;
        if (query.StartsWith('?'))
        {
            query = query[1..];
        }

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = part.IndexOf('=');
            var name = split >= 0 ? part[..split] : part;
            if (!name.Equals(key, StringComparison.Ordinal))
            {
                continue;
            }

            var raw = split >= 0 ? part[(split + 1)..] : "";
            return Uri.UnescapeDataString(raw.Replace('+', ' '));
        }

        return null;
    }

    private static string? NormalizeId(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != 11)
        {
            return null;
        }

        foreach (var character in value)
        {
            var ok = character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-';
            if (!ok)
            {
                return null;
            }
        }

        return value;
    }
}

/// <summary>
/// Window and caption title while a YouTube item is open. Loading text stays out of the status slot.
/// </summary>
public static class YouTubeTitle
{
    public static string Display(string? mediaTitle, string url)
    {
        if (!IsResolved(mediaTitle, url))
        {
            return UiCopy.YouTubeLoadingTitle;
        }

        return mediaTitle!.Trim();
    }

    public static bool IsResolved(string? mediaTitle, string url)
    {
        if (string.IsNullOrWhiteSpace(mediaTitle))
        {
            return false;
        }

        var title = mediaTitle.Trim();
        if (title.Equals(UiCopy.YouTubeLoadingTitle, StringComparison.Ordinal))
        {
            return false;
        }

        if (title.Equals(url, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (YouTubeUrl.TryGetVideoId(url) is { } id && title.Equals(id, StringComparison.Ordinal))
        {
            return false;
        }

        if (Uri.TryCreate(title, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// libmpv ytdl_hook options. no-playlist keeps a watch URL that also has list= on the single v= video.
/// Cookie and credential options are never included.
/// </summary>
public static class YouTubeMpvOptions
{
    public static IReadOnlyList<(string Name, string Value)> For(string ytdlpPath)
        =>
        [
            ("ytdl", "yes"),
            ("script-opts", "ytdl_hook-ytdl_path=" + ytdlpPath),
            ("ytdl-raw-options", "no-playlist=")
        ];
}

/// <summary>
/// yt-dlp.exe beside the application executable, then each PATH directory.
/// The release zip places the pinned official yt-dlp.exe beside Ieseo.exe.
/// </summary>
public static class YtDlpLocator
{
    public const string ExecutableName = "yt-dlp.exe";

    public static string? Resolve(string? applicationDirectory, IEnumerable<string>? pathDirectories, Func<string, bool> exists)
    {
        if (!string.IsNullOrWhiteSpace(applicationDirectory))
        {
            var beside = Path.Combine(applicationDirectory, ExecutableName);
            if (exists(beside))
            {
                return beside;
            }
        }

        if (pathDirectories is null)
        {
            return null;
        }

        foreach (var directory in pathDirectories)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            var candidate = Path.Combine(directory.Trim().Trim('"'), ExecutableName);
            if (exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static string? ResolveFromEnvironment()
    {
        var executable = Environment.ProcessPath;
        var directory = string.IsNullOrEmpty(executable)
            ? AppContext.BaseDirectory
            : Path.GetDirectoryName(executable);
        var path = Environment.GetEnvironmentVariable("PATH");
        var entries = string.IsNullOrEmpty(path) ? [] : path.Split(Path.PathSeparator);
        return Resolve(directory, entries, File.Exists);
    }
}
