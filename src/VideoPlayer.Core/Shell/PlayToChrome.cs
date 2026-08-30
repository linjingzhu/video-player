using VideoPlayer.Core.Playback;

namespace VideoPlayer.Core.Shell;

/// <summary>
/// CAST copy lock: one existing 보기 item. Idle 연결 장치로 재생 /
/// connected 연결 끄기. No badge, transport button, eject, or custom list.
/// </summary>
public sealed class PlayToChrome
{
    public bool IsConnected { get; set; }
    public string MenuLabel => MiracastProjection.MenuLabel(IsConnected);
    public IReadOnlyList<string> ViewItems { get; } = MiracastProjection.MenuLabels;
    public bool OpensFromViewMenuOnly { get; } = true;
    public bool AddedToExistingViewMenu { get; } = true;
    public bool HasBadgeOnTransport { get; } = false;
    public bool HasCastIcon { get; } = false;
    public bool HasEjectIcon { get; } = false;
    public bool HasCustomDeviceList { get; } = false;
    public bool UsesOsPicker { get; } = true;
    public bool UsesProjectionManager { get; } = true;
    public bool AllowsDlna { get; } = false;
    public bool AllowsChromecast { get; } = false;
    public bool AllowsAirPlay { get; } = false;
}
