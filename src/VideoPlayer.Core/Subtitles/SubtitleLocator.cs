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
        foreach (var ext in SupportedFormats.SubtitleExtensions)
        {
            var candidate = Path.Combine(directory, stem + ext);
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
