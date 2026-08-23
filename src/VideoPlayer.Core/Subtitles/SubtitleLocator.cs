using VideoPlayer.Core.Media;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Subtitles;

public static class SubtitleLocator
{
    public static IReadOnlyList<string> FindSidecars(string mediaPath)
    {
        var media = PathValidator.ValidateLocalFilePath(mediaPath);
        if (!media.Success || media.FullPath is null)
        {
            return [];
        }

        var directory = Path.GetDirectoryName(media.FullPath);
        var stem = Path.GetFileNameWithoutExtension(media.FullPath);
        if (directory is null || string.IsNullOrEmpty(stem) || FileNameSanitizer.LooksMalicious(stem + ".srt"))
        {
            return [];
        }

        var found = new List<string>();
        foreach (var name in SidecarFileNames(stem))
        {
            if (FileNameSanitizer.LooksMalicious(name))
            {
                continue;
            }

            var candidate = Path.Combine(directory, name);
            var resolved = PathValidator.ValidateLocalFilePath(candidate);
            if (!resolved.Success || resolved.FullPath is null)
            {
                continue;
            }

            if (!PathValidator.IsSameDirectory(media.FullPath, resolved.FullPath))
            {
                continue;
            }

            if (File.Exists(resolved.FullPath))
            {
                found.Add(resolved.FullPath);
            }
        }

        return found;
    }

    public static IReadOnlyList<string> SidecarFileNames(string stem)
        =>
        [
            stem + ".ko.srt",
            stem + ".srt",
            stem + ".smi"
        ];

    public static string? PreferPrimary(IReadOnlyList<string> tracks)
    {
        var ko = tracks.FirstOrDefault(path => path.EndsWith(".ko.srt", StringComparison.OrdinalIgnoreCase));
        if (ko is not null)
        {
            return ko;
        }

        return tracks.FirstOrDefault(path => !IsEnglishSidecar(path));
    }

    public static IReadOnlyList<string> FindAllTracks(string mediaPath)
    {
        var found = new List<string>(FindSidecars(mediaPath));
        foreach (var extra in EnumerateLanguageSidecars(mediaPath))
        {
            if (!found.Contains(extra, StringComparer.OrdinalIgnoreCase))
            {
                found.Add(extra);
            }
        }

        return found;
    }

    public static string? SuggestSecondary(IReadOnlyList<string> tracks)
        => tracks.FirstOrDefault(IsEnglishSidecar);

    public static bool IsEnglishSidecar(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var name = Path.GetFileName(path);
        return name.EndsWith(".en.srt", StringComparison.OrdinalIgnoreCase)
               || name.EndsWith(".en.smi", StringComparison.OrdinalIgnoreCase)
               || name.EndsWith(".en.sami", StringComparison.OrdinalIgnoreCase)
               || name.EndsWith(".eng.srt", StringComparison.OrdinalIgnoreCase)
               || name.EndsWith(".eng.smi", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> EnumerateLanguageSidecars(string mediaPath)
    {
        var media = PathValidator.ValidateLocalFilePath(mediaPath);
        if (!media.Success || media.FullPath is null)
        {
            return [];
        }

        var directory = Path.GetDirectoryName(media.FullPath);
        var stem = Path.GetFileNameWithoutExtension(media.FullPath);
        if (directory is null || string.IsNullOrEmpty(stem) || !Directory.Exists(directory))
        {
            return [];
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        var found = new List<string>();
        foreach (var file in files)
        {
            var name = Path.GetFileName(file);
            if (FileNameSanitizer.LooksMalicious(name) || !IsLanguageSidecar(stem, name))
            {
                continue;
            }

            var resolved = PathValidator.ValidateLocalFilePath(file);
            if (!resolved.Success || resolved.FullPath is null)
            {
                continue;
            }

            if (!PathValidator.IsSameDirectory(media.FullPath, resolved.FullPath))
            {
                continue;
            }

            found.Add(resolved.FullPath);
        }

        return found;
    }

    private static bool IsLanguageSidecar(string stem, string fileName)
    {
        if (!fileName.StartsWith(stem + ".", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var ext = SupportedFormats.NormalizeExtension(fileName);
        if (ext is null || !SupportedFormats.SubtitleExtensions.Contains(ext))
        {
            return false;
        }

        var rest = fileName[(stem.Length + 1)..];
        var tag = Path.GetFileNameWithoutExtension(rest);
        return tag.Length is > 0 and <= 8 && tag.All(ch => char.IsLetterOrDigit(ch) || ch == '-');
    }

    public static ValidationResult AcceptExternalSubtitle(string mediaPath, string subtitlePath)
    {
        var media = PathValidator.ValidateLocalFilePath(mediaPath);
        if (!media.Success)
        {
            return media;
        }

        var sub = PathValidator.ValidateLocalFilePath(subtitlePath);
        if (!sub.Success)
        {
            return sub;
        }

        var ext = SupportedFormats.NormalizeExtension(sub.FullPath);
        if (ext is null || !SupportedFormats.SubtitleExtensions.Contains(ext))
        {
            return ValidationResult.Fail("SRT 또는 SMI 자막만 사용할 수 있습니다.");
        }

        if (!PathValidator.IsSameDirectory(media.FullPath!, sub.FullPath!))
        {
            return ValidationResult.Fail("자막은 영상과 같은 폴더에 있어야 합니다.");
        }

        return sub;
    }
}
