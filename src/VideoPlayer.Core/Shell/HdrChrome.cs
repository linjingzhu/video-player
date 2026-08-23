using VideoPlayer.Core.Playback;

namespace VideoPlayer.Core.Shell;

/// <summary>
/// HDR copy lock: 보기 items are HDR 자동 / HDR 끄기 on the existing menu.
/// No submenu panel, badge, Cast, 퀵메뉴, or two-column settings.
/// </summary>
public sealed class HdrChrome
{
    public HdrMode Mode { get; set; } = HdrPassThrough.Default;
    public IReadOnlyList<string> ViewItems { get; } = UiCopy.HdrChoices;
    public bool OpensFromViewMenuOnly { get; } = true;
    public bool AddedToExistingViewMenu { get; } = true;
    public bool HasSubmenu { get; } = false;
    public bool HasTwoColumnSettingsPanel { get; } = false;
    public bool HasSettingsLeftRail { get; } = false;
    public bool HasQuickMenu { get; } = false;
    public bool HasBadgeOnTransport { get; } = false;
    public bool HasCast { get; } = false;
    public bool HasMiracast { get; } = false;
    public bool PassThroughWhenDisplaySupports => HdrPassThrough.IsPassThrough(Mode);
}
