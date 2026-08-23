using VideoPlayer.Core.Media;
using VideoPlayer.Core.Safety;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Playback;

/// <summary>
/// File &gt; URL 열기 accept list. http and https only. No cookies, headers, login, or other schemes.
/// </summary>
public static class OpenUrlRules
{
    public static bool IsAcceptedHttpUrl(string? value)
        => Validate(value).Success;

    public static ValidationResult Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Fail(UiCopy.OpenUrlEmpty);
        }

        var trimmed = value.Trim();
        if (trimmed.IndexOfAny(['\0', '\r', '\n']) >= 0 || LooksLikeCookiesOrHeaders(trimmed))
        {
            return ValidationResult.Fail(UiCopy.OpenUrlNoCookiesOrHeaders);
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return FailScheme(trimmed);
        }

        if (uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Fail(UiCopy.OpenUrlNoFileScheme);
        }

        if (uri.Scheme.Equals("rtmp", StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals("rtmps", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Fail(UiCopy.OpenUrlNoRtmp);
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Fail(UiCopy.OpenUrlHttpOnlyReason);
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return ValidationResult.Fail(UiCopy.OpenUrlNoLogin);
        }

        return ValidationResult.Ok(trimmed);
    }

    public static string? ContainerPath(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var ext = SupportedFormats.NormalizeExtension(uri.AbsolutePath);
        return ext is null ? null : uri.AbsolutePath;
    }

    public static string DisplayName(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath.TrimEnd('/');
            var slash = path.LastIndexOf('/');
            var leaf = slash >= 0 ? path[(slash + 1)..] : path;
            if (!string.IsNullOrWhiteSpace(leaf))
            {
                return FileNameSanitizer.ForDisplay(Uri.UnescapeDataString(leaf));
            }

            return FileNameSanitizer.ForDisplay(uri.Host);
        }

        return FileNameSanitizer.ForDisplay(url);
    }

    private static ValidationResult FailScheme(string trimmed)
    {
        if (StartsWithScheme(trimmed, "file"))
        {
            return ValidationResult.Fail(UiCopy.OpenUrlNoFileScheme);
        }

        if (StartsWithScheme(trimmed, "rtmp") || StartsWithScheme(trimmed, "rtmps"))
        {
            return ValidationResult.Fail(UiCopy.OpenUrlNoRtmp);
        }

        return ValidationResult.Fail(UiCopy.OpenUrlHttpOnlyReason);
    }

    private static bool StartsWithScheme(string value, string scheme)
        => value.StartsWith(scheme + ":", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeCookiesOrHeaders(string value)
    {
        if (value.Contains("Cookie:", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Set-Cookie:", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Referer:", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Authorization:", StringComparison.OrdinalIgnoreCase)
            || value.Contains("--http-header", StringComparison.OrdinalIgnoreCase)
            || value.Contains("--cookies", StringComparison.OrdinalIgnoreCase)
            || value.Contains("--ytdl", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return value.Contains(' ') && !Uri.TryCreate(value, UriKind.Absolute, out _);
    }
}
