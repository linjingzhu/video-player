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
    public string OverlayTime { get; set; } = "00:00:00 / 00:00:00";
    public string OverlaySubtitle { get; set; } = UiCopy.SubtitlePlaceholder;
    public bool IsPaused { get; set; } = true;
    public bool ChromeVisible { get; set; } = true;

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
    public List<string> Items { get; } = [UiCopy.ContinueWatching];
}

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
        return span.ToString(span.TotalHours >= 1 ? @"hh\:mm\:ss" : @"hh\:mm\:ss");
    }
}

public sealed class StatusBarState
{
    public string Text { get; set; } = "소프트웨어 · 대기";
    public string SeriesSummary { get; set; } = "";
}

public sealed class FullscreenChrome
{
    public string Title { get; set; } = UiCopy.AppTitle;
    public string NextEpisodeLabel { get; } = UiCopy.NextEpisode;
    public bool Visible { get; set; } = true;
    public bool AlwaysOnTop { get; set; }
}

public sealed class SeriesPanelState
{
    public string OpenFolderLabel { get; } = UiCopy.OpenFolder;
    public string AddToPlaylistLabel { get; } = UiCopy.AddToPlaylist;
    public string SortLabel { get; set; } = UiCopy.SortByEpisode;
    public SeriesSortMode SortMode { get; set; } = SeriesSortMode.Episode;
    public List<string> Tree { get; set; } = [];
    public List<SeriesRow> Rows { get; set; } = [];
}

public sealed record SeriesRow(string Episode, string FileName, string Duration, string Progress, bool IsCurrent);
