using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Playback;

/// <summary>
/// Resume identity: local files are path + size. http(s) sources use the exact URL string and no size.
/// </summary>
public static class ResumeKey
{
    public static string From(string path, long size)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("경로가 필요합니다.", nameof(path));
        }

        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "파일 크기는 0 이상이어야 합니다.");
        }

        return $"{PathText.NormalizeForKey(path)}|{size}";
    }

    public static string FromUrl(string url)
    {
        var check = OpenUrlRules.Validate(url);
        if (!check.Success || check.FullPath is null)
        {
            throw new ArgumentException(check.Error ?? "http(s) 주소가 필요합니다.", nameof(url));
        }

        return check.FullPath;
    }

    public static bool TryParse(string? key, out string path, out long size)
    {
        path = "";
        size = 0;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var split = key.LastIndexOf('|');
        if (split <= 0 || split == key.Length - 1)
        {
            return false;
        }

        path = key[..split];
        return long.TryParse(key[(split + 1)..], out size) && size >= 0 && path.Length > 0;
    }
}

public readonly record struct MediaIdentity(string Path, long Size, MediaSourceKind Kind)
{
    public MediaIdentity(string path, long size)
        : this(path, size, MediaSourceKind.LocalFile)
    {
    }

    public string Key => Kind == MediaSourceKind.HttpUrl
        ? ResumeKey.FromUrl(Path)
        : ResumeKey.From(Path, Size);

    public static MediaIdentity FromFile(string path)
    {
        var info = new FileInfo(path);
        return new MediaIdentity(PathText.NormalizeForKey(info.FullName), info.Length, MediaSourceKind.LocalFile);
    }

    public static MediaIdentity FromUrl(string url)
        => new(ResumeKey.FromUrl(url), 0, MediaSourceKind.HttpUrl);
}
