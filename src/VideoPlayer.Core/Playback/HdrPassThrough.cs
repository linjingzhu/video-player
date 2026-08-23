using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Playback;

/// <summary>
/// HDR pack: automatic pass-through when the display supports it (libmpv / D3D11).
/// View menu is 자동 / 끄기. Default 자동. No transport badge. No Cast/Miracast.
/// </summary>
public enum HdrMode
{
    Auto = 0,
    Off = 1
}

public readonly record struct MpvOption(string Name, string Value);

public static class HdrPassThrough
{
    public const string GpuApi = "d3d11";
    public const string Vo = "gpu";
    public const string Hwdec = "d3d11va";
    public const string SettingAuto = "auto";
    public const string SettingOff = "off";

    public static HdrMode Default { get; } = HdrMode.Auto;

    public static IReadOnlyList<MpvOption> InitVideoOutput { get; } =
    [
        new("gpu-api", GpuApi),
        new("vo", Vo)
    ];

    public static HdrMode Clamp(HdrMode mode)
        => mode == HdrMode.Off ? HdrMode.Off : HdrMode.Auto;

    public static HdrMode Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Default;
        }

        var trimmed = value.Trim();
        if (string.Equals(trimmed, SettingOff, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, UiCopy.HdrOff, StringComparison.Ordinal)
            || string.Equals(trimmed, "no", StringComparison.OrdinalIgnoreCase))
        {
            return HdrMode.Off;
        }

        return Default;
    }

    public static string ToSetting(HdrMode mode)
        => Clamp(mode) == HdrMode.Off ? SettingOff : SettingAuto;

    public static bool IsPassThrough(HdrMode mode)
        => Clamp(mode) == HdrMode.Auto;

    public static IReadOnlyList<MpvOption> RuntimeOptions(HdrMode mode)
    {
        if (Clamp(mode) == HdrMode.Off)
        {
            return
            [
                new("target-colorspace-hint", "no"),
                new("target-trc", "srgb"),
                new("target-prim", "bt.709")
            ];
        }

        return
        [
            new("target-colorspace-hint", "yes"),
            new("target-trc", "auto"),
            new("target-prim", "auto")
        ];
    }

    public static IReadOnlyList<MpvOption> Options(HdrMode mode)
        => [.. InitVideoOutput, .. RuntimeOptions(mode)];
}
