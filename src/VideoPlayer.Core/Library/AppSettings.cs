using System.Text.Json;
using System.Text.Json.Serialization;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.Core.Library;

/// <summary>
/// Global player settings (AppData). Not per-title.
/// <see cref="JumpSeconds"/> is one integer 1–60 (default 10). Forward and back
/// share the value. 보기 / 퀵메뉴 &gt; 점프 초. No transport-bar control.
/// Capture and clip-save keep last-used folders on separate keys.
/// HDR is a global HDR 자동 / HDR 끄기 key (default 자동).
/// Last volume (muted + level) is restored on launch. Speed is not stored.
/// </summary>
public sealed class AppSettings
{
    public const string FileName = "settings.json";
    public const string JumpSecondsKey = "jumpSeconds";
    public const string CaptureFolderKey = "captureFolder";
    public const string ClipFolderKey = "clipFolder";
    public const string HdrKey = "hdr";
    public const string VolumeKey = "volume";
    public const string MutedKey = "muted";
    public const double DefaultVolume = 1.0;

    public int JumpSeconds { get; private set; } = JumpInterval.DefaultSeconds;
    public string? CaptureFolder { get; private set; }
    public string? ClipFolder { get; private set; }
    public HdrMode Hdr { get; private set; } = HdrPassThrough.Default;
    public double Volume { get; private set; } = DefaultVolume;
    public bool Muted { get; private set; }

    /// <summary>Live-apply hook. Clamps 1–60 and is used by the next skip immediately.</summary>
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

    public (double Volume, bool Muted) SetVolume(double volume, bool muted)
    {
        Volume = ClampVolume(volume);
        Muted = muted || Volume <= 0;
        return (Volume, Muted);
    }

    public static double ClampVolume(double volume)
    {
        if (double.IsNaN(volume) || double.IsInfinity(volume))
        {
            return DefaultVolume;
        }

        return Math.Clamp(volume, 0, 1);
    }

    public string ToJson()
        => JsonSerializer.Serialize(
            new AppSettingsDto(JumpSeconds, CaptureFolder, ClipFolder, HdrPassThrough.ToSetting(Hdr), Volume, Muted),
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

            if (document.RootElement.TryGetProperty(VolumeKey, out var volume)
                && volume.TryGetDouble(out var level))
            {
                settings.Volume = ClampVolume(level);
            }

            if (document.RootElement.TryGetProperty(MutedKey, out var muted)
                && muted.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                settings.Muted = muted.GetBoolean();
            }

            if (settings.Volume <= 0)
            {
                settings.Muted = true;
            }
        }
        catch (JsonException)
        {
            return settings;
        }

        return settings;
    }

    private sealed record AppSettingsDto(
        int JumpSeconds,
        string? CaptureFolder,
        string? ClipFolder,
        string Hdr,
        double Volume,
        bool Muted);

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

    public static bool IsInRange(int seconds)
        => seconds is >= MinSeconds and <= MaxSeconds;

    public static int Clamp(int seconds)
        => IsInRange(seconds) ? seconds : DefaultSeconds;

    public static int Clamp(int? seconds)
        => seconds is { } value ? Clamp(value) : DefaultSeconds;

    /// <summary>Sheet stepper: stay on the 1–60 edge instead of snapping to default.</summary>
    public static int ClampDraft(int seconds)
        => Math.Clamp(seconds, MinSeconds, MaxSeconds);

    /// <summary>퀵메뉴 / OSD / arrow copy for the current N.</summary>
    public static string FormatSkipBack(int seconds) => $"-{Clamp(seconds)}초";

    public static string FormatSkipForward(int seconds) => $"+{Clamp(seconds)}초";

    public static string FormatPlusMinus(int seconds) => $"±{ClampDraft(seconds)}";
}
