namespace VideoPlayer.Core.Shell;

public sealed class PlayerShell
{
    public string Title { get; init; } = UiCopy.AppTitle;
    public IReadOnlyList<string> Menus { get; init; } = UiCopy.MainMenus;
    public ShellScreen Screen { get; set; } = ShellScreen.Main;
    public SidebarState Sidebar { get; } = new();
    public TransportState Transport { get; } = new();
    public StatusBarState Status { get; } = new();
    public FullscreenChrome Fullscreen { get; } = new();
    public SeriesPanelState Series { get; } = new();
    public NextEpisodeChrome NextEpisode { get; } = new();
    public string OverlayTime { get; set; } = "00:00:00 / 00:00:00";
    public string OverlaySubtitle { get; set; } = "";
    public bool IsPaused { get; set; } = true;
    public bool ChromeVisible { get; set; } = true;
    public bool CenterPlayIcon { get; } = false;

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

public sealed class SidebarState
{
    public string Title { get; } = UiCopy.SidebarTitle;
    public bool Open { get; set; }
    public SidebarResumeItem? Resume { get; set; }
    public List<SidebarSeriesItem> RecentSeries { get; } = [];
}

public sealed record SidebarResumeItem(string Label, string Path, long Size);

public sealed record SidebarSeriesItem(string Title, string FolderPath);

public sealed class TransportState
{
    public string SkipBackLabel { get; } = UiCopy.SkipBack;
    public string SkipForwardLabel { get; } = UiCopy.SkipForward;
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

public sealed class StatusBarState
{
    public string Text { get; set; } = "";
    public bool Visible => !string.IsNullOrWhiteSpace(Text);

    public void Clear() => Text = "";

    public void Fail(string message) => Text = message;
}

public sealed class FullscreenChrome
{
    public string Title { get; set; } = UiCopy.AppTitle;
    public string NextEpisodeLabel { get; } = UiCopy.NextEpisode;
    public bool Visible { get; set; } = true;
    public bool AlwaysOnTopPin { get; } = false;
}

public sealed class NextEpisodeChrome
{
    public bool ShowCta { get; set; }
    public bool AutoNextPending { get; set; }
    public string Label { get; set; } = UiCopy.NextEpisode;
    public string CancelLabel { get; } = UiCopy.NextEpisodeCancel;
}

public sealed class SeriesPanelState
{
    public string OpenFolderLabel { get; } = UiCopy.OpenFolder;
    public string BackLabel { get; } = UiCopy.Back;
    public bool PlaylistButton { get; } = false;
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
