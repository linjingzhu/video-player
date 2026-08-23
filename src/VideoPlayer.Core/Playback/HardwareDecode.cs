using VideoPlayer.Core.Media;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Playback;

public enum DecodePath
{
    Hardware,
    Software
}

public enum HardwareDecodeAttempt
{
    D3D11VA,
    Dxva2,
    None
}

public sealed class HardwareDecodePolicy
{
    public IReadOnlyList<HardwareDecodeAttempt> Attempts { get; } =
    [
        HardwareDecodeAttempt.D3D11VA,
        HardwareDecodeAttempt.Dxva2,
        HardwareDecodeAttempt.None
    ];

    public DecodeOutcome OnHardwareFailed(string? videoCodec, string? audioCodec)
        => new(
            DecodePath.Software,
            HardwareDecodeAttempt.None,
            ContinuePlayback: true,
            StatusText: StatusText.Format(DecodePath.Software, videoCodec, audioCodec));
}

public readonly record struct DecodeOutcome(
    DecodePath Path,
    HardwareDecodeAttempt Attempt,
    bool ContinuePlayback,
    string StatusText);

public static class StatusText
{
    public static string Format(DecodePath path, string? videoCodec, string? audioCodec)
        => path == DecodePath.Hardware ? "" : SoftwareFallback(videoCodec, audioCodec);

    public static string SoftwareFallback(string? videoCodec, string? audioCodec)
        => $"{UiCopy.SoftwareFallback} · {SupportedFormats.DisplayCodecName(videoCodec)} · {SupportedFormats.DisplayCodecName(audioCodec)}";

    public static string Unsupported(string? codecName)
        => $"{UiCopy.Unsupported} · {SupportedFormats.DisplayCodecName(codecName)}";

    public static string PlaybackFailed(string? reason = null)
        => $"{UiCopy.PlaybackFailed} · {(string.IsNullOrWhiteSpace(reason) ? UiCopy.NetworkFailed : reason.Trim())}";

    public static bool IsConfirmedFailureLine(string? text)
        => !string.IsNullOrWhiteSpace(text)
           && (text.StartsWith(UiCopy.Unsupported, StringComparison.Ordinal)
               || text.StartsWith(UiCopy.SoftwareFallback, StringComparison.Ordinal)
               || text.StartsWith(UiCopy.PlaybackFailed, StringComparison.Ordinal));
}

public sealed record OpenMediaResult
{
    public required bool Success { get; init; }
    public required string Path { get; init; }
    public long Size { get; init; }
    public string? VideoCodec { get; init; }
    public string? AudioCodec { get; init; }
    public bool AddedToRecent { get; init; }
    public bool HardwareActive { get; init; }
    public string Status { get; init; } = "";
    public string? Error { get; init; }
    public string? UnsupportedCodecName { get; init; }

    public static OpenMediaResult Unsupported(string path, string? codec)
        => new()
        {
            Success = false,
            Path = path,
            AddedToRecent = false,
            UnsupportedCodecName = SupportedFormats.DisplayCodecName(codec),
            Status = StatusText.Unsupported(codec),
            Error = StatusText.Unsupported(codec)
        };
}
