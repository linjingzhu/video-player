using VideoPlayer.Core.Safety;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Playback;

/// <summary>
/// File &gt; 다른 이름으로 저장. OS save dialog only. Plain GET of the current http(s) URL.
/// No cookies, custom headers, Range, auth, or HLS key unwrapping.
/// </summary>
public static class UrlSaveAs
{
    public const bool UsesOsDialog = true;
    public const bool HasInAppSheet = false;
    public const bool PromptsForCookies = false;
    public const bool PromptsForKeys = false;
    public const bool PromptsForHeaders = false;

    public static bool CanSave(MediaSourceKind source, string? url)
        => source == MediaSourceKind.HttpUrl && OpenUrlRules.IsAcceptedHttpUrl(url);

    public static string SuggestedFileName(string url)
    {
        var name = OpenUrlRules.DisplayName(url);
        return string.IsNullOrWhiteSpace(name) || name == "(이름 없음)" ? "video.mp4" : name;
    }

    public static bool LooksLikeKeyedHls(ReadOnlySpan<byte> prefix)
    {
        var text = System.Text.Encoding.UTF8.GetString(prefix);
        if (!text.Contains("#EXTM3U", StringComparison.Ordinal))
        {
            return false;
        }

        return text.Contains("#EXT-X-KEY", StringComparison.OrdinalIgnoreCase)
               || text.Contains("#EXT-X-SESSION-KEY", StringComparison.OrdinalIgnoreCase);
    }

    public static ValidationResult ValidateDestination(string? path)
    {
        var check = PathValidator.ValidateLocalFilePath(path);
        if (!check.Success || check.FullPath is null)
        {
            return check;
        }

        var name = Path.GetFileName(check.FullPath);
        if (FileNameSanitizer.LooksMalicious(name))
        {
            return ValidationResult.Fail("저장 경로가 올바르지 않습니다.");
        }

        var directory = Path.GetDirectoryName(check.FullPath);
        if (string.IsNullOrEmpty(directory))
        {
            return ValidationResult.Fail("저장 경로가 올바르지 않습니다.");
        }

        return check;
    }
}

public sealed record UrlGetResult(
    bool Success,
    int? StatusCode,
    bool NeedsCredentials,
    string? Error);

public interface IUrlGetClient
{
    UrlGetResult Get(string url, string destinationPath);
}

/// <summary>
/// Bare GET. Cookie container off, no default credentials, no extra headers, no Range.
/// </summary>
public sealed class PlainHttpGetClient : IUrlGetClient
{
    public UrlGetResult Get(string url, string destinationPath)
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                UseCookies = false,
                UseDefaultCredentials = false,
                PreAuthenticate = false,
                AllowAutoRedirect = true
            };
            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(2)
            };
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            using var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);
            var code = (int)response.StatusCode;
            var wantsSecrets = code is 401 or 403 or 407
                               || response.Headers.WwwAuthenticate.Count > 0;
            if (wantsSecrets)
            {
                TryDelete(destinationPath);
                return new UrlGetResult(false, code, true, UiCopy.SaveAsNeedsSecrets);
            }

            if (!response.IsSuccessStatusCode)
            {
                TryDelete(destinationPath);
                return new UrlGetResult(false, code, false, UiCopy.NetworkFailed);
            }

            using (var input = response.Content.ReadAsStream())
            using (var output = File.Create(destinationPath))
            {
                input.CopyTo(output);
            }

            if (FileLooksLikeKeyedHls(destinationPath))
            {
                TryDelete(destinationPath);
                return new UrlGetResult(false, code, true, UiCopy.SaveAsNeedsSecrets);
            }

            return new UrlGetResult(true, code, false, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            TryDelete(destinationPath);
            return new UrlGetResult(false, null, false, UiCopy.NetworkFailed);
        }
    }

    private static bool FileLooksLikeKeyedHls(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length == 0 || info.Length > 512 * 1024)
            {
                return false;
            }

            var prefix = File.ReadAllBytes(path);
            return UrlSaveAs.LooksLikeKeyedHls(prefix);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}

public sealed record UrlSaveResult(bool Success, string? Path, string Status);