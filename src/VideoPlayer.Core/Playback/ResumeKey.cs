using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Playback;

/// <summary>Confirmed resume identity: absolute path + file size.</summary>
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

public readonly record struct MediaIdentity(string Path, long Size)
{
    public string Key => ResumeKey.From(Path, Size);

    public static MediaIdentity FromFile(string path)
    {
        var info = new FileInfo(path);
        return new MediaIdentity(PathText.NormalizeForKey(info.FullName), info.Length);
    }
}
