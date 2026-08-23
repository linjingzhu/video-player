using System.Text;

namespace VideoPlayer.Core.Safety;

/// <summary>
/// Local-file-only path rules. Rejects remote URIs, traversal, and device paths.
/// </summary>
public static class PathValidator
{
    public const int MaxPathLength = 4096;

    private static readonly string[] RemoteSchemes =
    [
        "http", "https", "ftp", "ftps", "rtsp", "rtsps", "mms", "mmsh",
        "udp", "tcp", "srt", "rist", "smb", "nfs", "webdav"
    ];

    public static bool IsRemoteUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return LooksLikeSchemePrefix(trimmed);
        }

        return RemoteSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);
    }

    public static bool LooksLikeUnc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return trimmed.StartsWith(@"\\", StringComparison.Ordinal)
               || trimmed.StartsWith("//", StringComparison.Ordinal);
    }

    public static ValidationResult ValidateLocalFilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationResult.Fail("경로가 비어 있습니다.");
        }

        var trimmed = path.Trim();
        if (trimmed.Length > MaxPathLength)
        {
            return ValidationResult.Fail("경로가 너무 깁니다.");
        }

        if (trimmed.IndexOf('\0') >= 0)
        {
            return ValidationResult.Fail("경로에 허용되지 않는 문자가 있습니다.");
        }

        if (IsRemoteUri(trimmed))
        {
            return ValidationResult.Fail("원격 주소는 열 수 없습니다. 로컬 파일만 지원합니다.");
        }

        if (LooksLikeUnc(trimmed))
        {
            return ValidationResult.Fail("네트워크 경로는 열 수 없습니다. 로컬 파일만 지원합니다.");
        }

        if (ContainsTraversalSegment(trimmed))
        {
            return ValidationResult.Fail("경로 탐색(..)은 허용되지 않습니다.");
        }

        try
        {
            var full = Path.GetFullPath(trimmed);
            if (LooksLikeUnc(full) || IsRemoteUri(full))
            {
                return ValidationResult.Fail("로컬 파일이 아닙니다.");
            }

            if (ContainsTraversalSegment(full) && !File.Exists(full) && !Directory.Exists(full))
            {
                return ValidationResult.Fail("경로 탐색(..)은 허용되지 않습니다.");
            }

            return ValidationResult.Ok(full);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or IOException)
        {
            return ValidationResult.Fail("경로를 해석할 수 없습니다.");
        }
    }

    public static bool IsInsideDirectory(string candidateFullPath, string directoryFullPath)
    {
        if (string.IsNullOrWhiteSpace(candidateFullPath) || string.IsNullOrWhiteSpace(directoryFullPath))
        {
            return false;
        }

        var file = Path.GetFullPath(candidateFullPath);
        var dir = Path.GetFullPath(directoryFullPath);
        if (!dir.EndsWith(Path.DirectorySeparatorChar) && !dir.EndsWith(Path.AltDirectorySeparatorChar))
        {
            dir += Path.DirectorySeparatorChar;
        }

        return file.StartsWith(dir, PathComparison);
    }

    public static bool IsSameDirectory(string leftFile, string rightFile)
    {
        var leftDir = Path.GetDirectoryName(Path.GetFullPath(leftFile));
        var rightDir = Path.GetDirectoryName(Path.GetFullPath(rightFile));
        if (leftDir is null || rightDir is null)
        {
            return false;
        }

        return string.Equals(
            Path.GetFullPath(leftDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(rightDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            PathComparison);
    }

    public static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static bool ContainsTraversalSegment(string path)
    {
        var parts = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(part => part == "..");
    }

    private static bool LooksLikeSchemePrefix(string value)
    {
        var colon = value.IndexOf(':');
        if (colon <= 1)
        {
            return false;
        }

        var scheme = value[..colon];
        return RemoteSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase);
    }
}

public readonly record struct ValidationResult(bool Success, string? FullPath, string? Error)
{
    public static ValidationResult Ok(string fullPath) => new(true, fullPath, null);

    public static ValidationResult Fail(string error) => new(false, null, error);
}

public static class PathText
{
    public static string NormalizeForKey(string path)
    {
        var full = Path.GetFullPath(path);
        return OperatingSystem.IsWindows() ? full.Replace('/', '\\') : full;
    }

    public static string Utf8Preview(string value, int maxChars = 120)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var ch in value)
        {
            if (char.IsControl(ch) && ch is not '\t')
            {
                continue;
            }

            builder.Append(ch);
            if (builder.Length >= maxChars)
            {
                builder.Append('…');
                break;
            }
        }

        return builder.ToString();
    }
}
