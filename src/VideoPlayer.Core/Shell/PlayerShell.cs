using VideoPlayer.Core.Capture;
using VideoPlayer.Core.Clip;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Skip;
using VideoPlayer.Core.Subtitles;

namespace VideoPlayer.Core.Shell;

/// <summary>SeriesOn main shell. Series C page and next-episode end CTA stay.</summary>
public sealed class PlayerShell
{
    public string Title { get; init; } = UiCopy.AppTitle;
    public IReadOnlyList<string> Menus { get; init; } = UiCopy.MainMenus;
    public string MenuSeparator { get; } = UiCopy.MenuSeparator;
    public HeaderChrome Header { get; } = new();
    public ShellScreen Screen { get; set; } = ShellScreen.Main;
    public SidebarState Sidebar { get; } = new();
    public TransportState Transport { get; } = new();
    public StatusBarState Status { get; } = new();
    public FullscreenChrome Fullscreen { get; } = new();
    public SeriesPanelState Series { get; } = new();
    public FileOnlyFeatureState FileOnly { get; } = new();
    public NextEpisodeChrome NextEpisode { get; } = new();
    public OverlayTimeState OverlayClock { get; } = new();
    public CaptureSheetState Capture { get; } = new();
    public CaptureBannerState CaptureBanner { get; } = new();
    public SkipCapsuleState Skip { get; } = new();
    public SubtitleSheetState Subtitles { get; } = new();
    public ClipSheetState Clip { get; } = new();
    public ClipBannerState ClipBanner { get; } = new();
    public VolumeChrome Volume { get; } = new();
    public string OverlayTime { get; set; } = "00:00:00 / 00:00:00";
    public string OverlaySubtitle { get; set; } = "";
    public string OverlaySecondarySubtitle { get; set; } = "";
    public bool IsPaused { get; set; } = true;
    public bool ChromeVisible { get; set; } = true;
    public bool CenterPlayIcon { get; } = false;
    public bool StageEmpty { get; set; } = true;
    public bool VideoFullWidth { get; } = true;
    public bool VideoFullBleed { get; } = true;
    public bool NoLetterboxChrome { get; } = true;
    public bool HasCookieAuthUi { get; } = false;
    public bool HasDrmUi { get; } = false;
    public bool HasPaidUnlockUi { get; } = false;
    public bool HasHeaderUi { get; } = true;
    public bool HasSaveAsSheet { get; } = false;
    public bool SaveAsUsesOsDialog { get; } = true;

    public static PlayerShell Boot() => new();

    public void EnterFullscreen()
    {
        Screen = ShellScreen.Fullscreen;
        Fullscreen.Visible = true;
        ChromeVisible = true;
    }

    public void ExitFullscreen()
    {
        Screen = ShellScreen.Main;
        ChromeVisible = true;
    }

    public void ShowSeries() => Screen = ShellScreen.Series;
}

public static class ShellLayout
{
    public const int SidebarRailWidthPx = 28;
    public const int SidebarOpenPanelWidthPx = 240;
    public const int HeaderHeightPx = SeriesOn.HeaderHeightPx;
    public const int TransportHeightPx = 40;
    public const int TransportSeparatorPx = SeriesOn.TransportSeparatorPx;

    public static IReadOnlyList<TransportControl> TransportOrder { get; } =
    [
        TransportControl.Rewind,
        TransportControl.PlayPause,
        TransportControl.Stop,
        TransportControl.Clear,
        TransportControl.FastForward,
        TransportControl.Seek,
        TransportControl.Volume,
        TransportControl.Time,
        TransportControl.Hamburger
    ];
}

public enum TransportControl
{
    PreviousEpisode,
    SkipBack,
    Rewind,
    PlayPause,
    Stop,
    Clear,
    FastForward,
    SkipForward,
    NextEpisode,
    Seek,
    Volume,
    Speed,
    Time,
    Captions,
    Fullscreen,
    Hamburger
}

public sealed class HeaderChrome
{
    public string Title { get; } = UiCopy.AppTitle;
    public string QuickMenuLabel { get; } = UiCopy.QuickMenu;
    public bool QuickMenuIsView { get; } = true;
    public bool HasWindowControls { get; } = true;
    public bool HasFileViewMenuBar { get; } = false;
    public bool FileCommandsInHamburger { get; } = true;
    public bool FileCommandsInQuickMenu { get; } = true;
    public bool HamburgerIsView { get; } = true;
    public bool HasMenuPipe { get; } = false;
    public bool ChromeIsSolid { get; } = true;
    public int HeightPx { get; } = ShellLayout.HeaderHeightPx;
}

public sealed class SidebarState
{
    public string Title { get; } = UiCopy.SidebarTitle;
    public int RailWidthPx { get; } = ShellLayout.SidebarRailWidthPx;
    public int OpenPanelWidthPx { get; } = ShellLayout.SidebarOpenPanelWidthPx;
    public bool Open { get; set; }
    public SidebarResumeItem? Resume { get; set; }
    public List<SidebarSeriesItem> RecentSeries { get; } = [];

    public int ContentWidthPx => Open ? OpenPanelWidthPx : 0;
}

public sealed record SidebarResumeItem(string Label, string Path, long Size);

public sealed record SidebarSeriesItem(string Title, string FolderPath);

public sealed class TransportState
{
    public IReadOnlyList<TransportControl> Order { get; } = ShellLayout.TransportOrder;
    public string SkipBackLabel { get; } = UiCopy.SkipBack;
    public string SkipForwardLabel { get; } = UiCopy.SkipForward;
    public string PreviousIcon { get; } = UiCopy.PreviousEpisodeIcon;
    public string NextIcon { get; } = UiCopy.NextEpisodeIcon;
    public bool NextEpisodeTextOnBar { get; } = false;
    public bool NextEpisodeIconOnly { get; } = true;
    public bool PreviousOnBar { get; } = false;
    public bool NextOnBar { get; } = false;
    public bool SkipLabelsOnBar { get; } = false;
    public bool SpeedOnBar { get; } = false;
    public bool TimeOnBar { get; } = true;
    public bool CaptionsOnBar { get; } = false;
    public bool FullscreenOnBar { get; } = false;
    public bool HasStop { get; } = true;
    public bool HasClear { get; } = true;
    public string ClearLabel { get; } = UiCopy.Clear;
    public bool ClearIsTextLabel { get; } = true;
    public bool ClearImmediatelyRightOfStop { get; } = true;
    public bool ClearNeverMarksComplete { get; } = true;
    public bool ClearAppliesToUrl { get; } = true;
    public bool HasRecordButton { get; } = false;
    public bool HasCastIcon { get; } = false;
    public bool HasHdrIcon { get; } = false;
    public bool HasEjectIcon { get; } = false;
    public bool HorizontalVolumeSlider { get; } = true;
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public double Position { get; set; }
    public double Duration { get; set; }
    public double Volume { get; set; } = 1.0;
    public double Speed { get; set; } = 1.0;
    public bool CaptionsOn { get; set; } = true;

    public string PositionText => Format(Position);
    public string DurationText => Format(Duration);
    public string SpeedText => Playback.PlaybackSpeed.Format(Speed);

    public static string Format(double seconds)
    {
        if (double.IsNaN(seconds) || seconds < 0)
        {
            seconds = 0;
        }

        var span = TimeSpan.FromSeconds(seconds);
        return span.ToString(@"hh\:mm\:ss");
    }
}

public sealed class OverlayTimeState
{
    public bool AboveTransport { get; } = false;
    public OverlayAnchor Anchor { get; } = OverlayAnchor.BottomLeft;
    public bool OnTransport { get; } = true;
}

public enum OverlayAnchor
{
    BottomLeft,
    BottomCenter,
    BottomRight
}

public sealed class StatusBarState
{
    public string Text { get; set; } = "";
    public bool FailureOnly { get; } = true;
    public bool DashedSlot { get; } = true;
    public bool HideWhenIdle { get; } = true;
    public bool Visible => !string.IsNullOrWhiteSpace(Text);

    public void Clear() => Text = "";

    public void Fail(string message)
        => Text = string.IsNullOrWhiteSpace(message) ? "" : message.Trim();
}

public sealed class FullscreenChrome
{
    public string Title { get; set; } = UiCopy.AppTitle;
    public string NextEpisodeLabel { get; } = UiCopy.NextEpisode;
    public bool Visible { get; set; } = true;
    public bool AlwaysOnTopPin { get; } = false;
    public bool CenterPlayIcon { get; } = false;
    public bool NextEpisodeTextOnBar { get; } = false;
    public bool EndCtaIsOverlay { get; } = true;
    public bool TransportIsFloatingOverlay { get; } = true;
    public bool WindowedTransportIsDocked { get; } = true;
    public double TransportOverlayOpacity { get; } = SeriesOn.FullscreenTransportOpacity;
    public IReadOnlyList<string> ToggleKeys { get; } = ["Enter", "F11"];
    public int IdleHideSeconds { get; } = SeriesOn.FullscreenIdleHideSeconds;
}

public sealed class NextEpisodeChrome
{
    public bool ShowCta { get; set; }
    public bool AutoNextPending { get; set; }
    public bool OverlayOnly { get; } = true;
    public bool EndRegionOnly { get; } = true;
    public bool OnTransport { get; } = false;
    public bool SharesSkipCorner { get; } = true;
    public OverlayAnchor Anchor { get; } = OverlayAnchor.BottomRight;
    public string Label { get; set; } = UiCopy.NextEpisodeCta;
    public string CancelLabel { get; } = UiCopy.NextEpisodeCancel;
}

public sealed class SeriesPanelState
{
    public string OpenFolderLabel { get; } = UiCopy.OpenFolder;
    public string BackLabel { get; } = UiCopy.Back;
    public bool PlaylistButton { get; } = false;
    public bool Enabled { get; set; } = true;
    public SeriesDrillLevel Level { get; set; } = SeriesDrillLevel.Shows;
    public string Heading { get; set; } = "";
    public List<SeriesListItem> Items { get; set; } = [];
}

public sealed record SeriesListItem(
    string Episode,
    string Title,
    string Progress,
    string? Path,
    long Size,
    string Kind);
