using System.Text.Json;

namespace VideoPlayer.Core.Library;

/// <summary>
/// Global player settings (AppData). Not per-title.
/// <see cref="JumpSeconds"/> is reserved for v1.5 skip interval; v1 keeps default 10
/// and wireframe copy ±10초. No settings UI in P0.
/// </summary>
public sealed class AppSettings
{
    public const string FileName = "settings.json";
    public const string JumpSecondsKey = "jumpSeconds";

    public int JumpSeconds { get; private set; } = JumpInterval.DefaultSeconds;

    /// <summary>v1.5 live-apply hook. Clamps 1–60 and is used by the next skip immediately.</summary>
    public int SetJumpSeconds(int seconds)
    {
        JumpSeconds = JumpInterval.Clamp(seconds);
        return JumpSeconds;
    }

    public string ToJson()
        => JsonSerializer.Serialize(new AppSettingsDto(JumpSeconds), JsonOptions);

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
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(JumpSecondsKey, out var jump)
                && jump.TryGetInt32(out var seconds))
            {
                settings.JumpSeconds = JumpInterval.Clamp(seconds);
            }
        }
        catch (JsonException)
        {
            return settings;
        }

        return settings;
    }

    private sealed record AppSettingsDto(int JumpSeconds);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
