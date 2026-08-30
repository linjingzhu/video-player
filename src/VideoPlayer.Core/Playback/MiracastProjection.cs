using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Playback;

/// <summary>
/// Isolated CAST pack: Windows Miracast / Projection only.
/// One existing 보기 item — idle <see cref="UiCopy.CastPlayTo"/>, connected
/// <see cref="UiCopy.CastDisconnect"/>. Opens the OS wireless-display picker
/// (ProjectionManager / Connect). No DLNA, Chromecast, AirPlay, custom
/// receiver, cookies, headers, or DRM.
/// </summary>
public static class MiracastProjection
{
    public const string ProjectionManagerClass = "Windows.UI.ViewManagement.ProjectionManager";
    public const string ConnectPickerUri = "ms-settings-connect:";
    public const uint MiracastOutputTechnology = 15;
    public const bool AllowsDlna = false;
    public const bool AllowsChromecast = false;
    public const bool AllowsAirPlay = false;
    public const bool UsesCustomDeviceList = false;
    public const bool UsesOsPicker = true;
    public const bool UsesProjectionManager = true;
    public const bool AddsTransportButton = false;
    public const bool AddsCaptionIcon = false;

    public static IReadOnlyList<string> MenuLabels { get; } =
        [UiCopy.CastPlayTo, UiCopy.CastDisconnect];

    public static string MenuLabel(bool projecting)
        => projecting ? UiCopy.CastDisconnect : UiCopy.CastPlayTo;

    public static bool AllowsSource(MediaSourceKind kind)
        => kind is MediaSourceKind.None or MediaSourceKind.LocalFile or MediaSourceKind.HttpUrl;

    public static WirelessDisplayResult Guarded(Func<WirelessDisplayResult> action)
    {
        try
        {
            return action();
        }
        catch (Exception)
        {
            return WirelessDisplayResult.Failed();
        }
    }
}

public enum WirelessDisplayKind
{
    Connected,
    Disconnected,
    PickerOpened,
    Cancelled,
    Failed
}

public readonly record struct WirelessDisplayResult(WirelessDisplayKind Kind, string? Error = null)
{
    public bool Succeeded => Kind is WirelessDisplayKind.Connected
        or WirelessDisplayKind.Disconnected
        or WirelessDisplayKind.PickerOpened;

    public bool IsFailure => Kind == WirelessDisplayKind.Failed;

    public static WirelessDisplayResult Connected() => new(WirelessDisplayKind.Connected);

    public static WirelessDisplayResult Disconnected() => new(WirelessDisplayKind.Disconnected);

    public static WirelessDisplayResult PickerOpened() => new(WirelessDisplayKind.PickerOpened);

    public static WirelessDisplayResult Cancelled() => new(WirelessDisplayKind.Cancelled);

    public static WirelessDisplayResult Failed(string? error = null)
        => new(WirelessDisplayKind.Failed, string.IsNullOrWhiteSpace(error) ? null : error.Trim());
}

/// <summary>OS wireless-display / ProjectionManager host. App implements; tests fake.</summary>
public interface IWirelessDisplayHost
{
    bool IsProjecting { get; }

    WirelessDisplayResult Start();

    WirelessDisplayResult Stop();
}

public sealed class IdleWirelessDisplayHost : IWirelessDisplayHost
{
    public static IdleWirelessDisplayHost Instance { get; } = new();

    public bool IsProjecting => false;

    public WirelessDisplayResult Start() => WirelessDisplayResult.Failed();

    public WirelessDisplayResult Stop() => WirelessDisplayResult.Disconnected();
}

public sealed class FakeWirelessDisplayHost : IWirelessDisplayHost
{
    public bool IsProjecting { get; set; }
    public bool FailNext { get; set; }
    public bool ThrowNext { get; set; }
    public bool CancelNext { get; set; }
    public int StartCalls { get; private set; }
    public int StopCalls { get; private set; }

    public WirelessDisplayResult Start()
    {
        StartCalls++;
        if (ThrowNext)
        {
            ThrowNext = false;
            throw new InvalidOperationException("wireless display");
        }

        if (FailNext)
        {
            FailNext = false;
            return WirelessDisplayResult.Failed();
        }

        if (CancelNext)
        {
            CancelNext = false;
            return WirelessDisplayResult.Cancelled();
        }

        IsProjecting = true;
        return WirelessDisplayResult.Connected();
    }

    public WirelessDisplayResult Stop()
    {
        StopCalls++;
        if (ThrowNext)
        {
            ThrowNext = false;
            throw new InvalidOperationException("wireless display");
        }

        if (FailNext)
        {
            FailNext = false;
            return WirelessDisplayResult.Failed();
        }

        IsProjecting = false;
        return WirelessDisplayResult.Disconnected();
    }
}
