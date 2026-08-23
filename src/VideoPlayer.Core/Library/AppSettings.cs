using System.Text.Json;
using System.Text.Json.Serialization;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.Core.Library;

/// <summary>
/// Global player settings (AppData). Not per-title.
/// <see cref="JumpSeconds"/> is reserved for v1.5 skip interval; v1 keeps default 10
/// and wireframe copy ±10초. No settings UI in P0.
/// Capture and clip-save keep last-used folders on separate keys.
/// HDR is a global 자동 / 끄기 key (default 자동).
/// </summary>
public sealed class AppSettings
{
    public const string FileName = "settings.json";
    public const string JumpSecondsKey = "jumpSeconds";
    public const string CaptureFolderKey = "captureFolder";
    public const string ClipFolderKey = "clipFolder";
    public const string HdrKey = "hdr";

    public int JumpSeconds { get; private set; } = JumpInterval.DefaultSeconds;
    public string? CaptureFolder { get; private set; }
    public string? ClipFolder { get; private set; }
    public HdrMode Hdr { get; private set; } = HdrPassThrough.Default;

    /// <summary>v1.5 live-apply hook. Clamps 1–60 and is used by the next skip immediately.</summary>
    public int SetJumpSeconds(int seconds)
    {
        JumpSeconds = JumpInterval.Clamp(seconds);
        return JumpSeconds;
    }

    public string? SetCaptureFolder(string? path)
    {
        CaptureFolder = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        return CaptureFolder;
    }

    public string? SetClipFolder(string? path)
    {
        ClipFolder = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        return ClipFolder;
    }

    public HdrMode SetHdr(HdrMode mode)
    {
        Hdr = HdrPassThrough.Clamp(mode);
        return Hdr;
    }

    public string ToJson()
        => JsonSerializer.Serialize(
            new AppSettingsDto(JumpSeconds, CaptureFolder, ClipFolder, HdrPassThrough.ToSetting(Hdr)),
            JsonOptions);

    public static AppSettings FromJson(string? json)
    {
        var settings = new AppSettings();
        if (string.IsNullOrWhiteSpace(json))
        {
            return settings;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return settings;
            }

            if (document.RootElement.TryGetProperty(JumpSecondsKey, out var jump)
                && jump.TryGetInt32(out var seconds))
            {
                settings.JumpSeconds = JumpInterval.Clamp(seconds);
            }

            if (document.RootElement.TryGetProperty(CaptureFolderKey, out var captureFolder)
                && captureFolder.ValueKind == JsonValueKind.String)
            {
                var value = captureFolder.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    settings.CaptureFolder = value;
                }
            }

            if (document.RootElement.TryGetProperty(ClipFolderKey, out var clipFolder)
                && clipFolder.ValueKind == JsonValueKind.String)
            {
                var value = clipFolder.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    settings.ClipFolder = value;
                }
            }

            if (document.RootElement.TryGetProperty(HdrKey, out var hdr)
                && hdr.ValueKind == JsonValueKind.String)
            {
                settings.Hdr = HdrPassThrough.Parse(hdr.GetString());
            }
        }
        catch (JsonException)
        {
            return settings;
        }

        return settings;
    }

    private sealed record AppSettingsDto(int JumpSeconds, string? CaptureFolder, string? ClipFolder, string Hdr);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };
}

/// <summary>Single global skip interval. Same value for back and forward.</summary>
public static class JumpInterval
{
    public const int DefaultSeconds = 10;
    public const int MinSeconds = 1;
    public const int MaxSeconds = 60;

    public static int Clamp(int seconds)
        => seconds is < MinSeconds or > MaxSeconds ? DefaultSeconds : seconds;

    public static int Clamp(int? seconds)
        => seconds is { } value ? Clamp(value) : DefaultSeconds;

    /// <summary>v1.5 OSD / button / taskbar copy. v1 chrome stays ±10초.</summary>
    public static string FormatSkipBack(int seconds) => $"-{Clamp(seconds)}초";

    public static string FormatSkipForward(int seconds) => $"+{Clamp(seconds)}초";
}
