using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Series;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class PlayerShellLayoutTests
{
    [Fact]
    public void Av2_sidebar_is_a_closed_28px_rail()
    {
        var shell = PlayerShell.Boot();
        Assert.False(shell.Sidebar.Open);
        Assert.Equal(28, shell.Sidebar.RailWidthPx);
        Assert.Equal(28, ShellLayout.SidebarRailWidthPx);
        Assert.Equal(0, shell.Sidebar.ContentWidthPx);
        Assert.True(shell.VideoFullWidth);
        Assert.True(shell.VideoFullBleed);
        Assert.True(shell.NoLetterboxChrome);
        Assert.False(shell.CenterPlayIcon);
        Assert.Equal(40, ShellLayout.TransportHeightPx);
        Assert.Equal(SkinA.TransportHeightPx, ShellLayout.TransportHeightPx);

        shell.Sidebar.Open = true;
        Assert.Equal(240, shell.Sidebar.ContentWidthPx);
    }

    [Fact]
    public void Menus_are_quickmenu_as_view_with_file_under_hamburger()
    {
        var shell = PlayerShell.Boot();
        Assert.Equal(new[] { "퀵메뉴" }, shell.Menus);
        Assert.True(shell.HasHeaderUi);
        Assert.False(shell.Header.HasFileViewMenuBar);
        Assert.True(shell.Header.QuickMenuIsView);
        Assert.True(shell.Header.FileCommandsInHamburger);
        Assert.Equal(new[] { "열기...", "URL 열기", "폴더 열기", "다른 이름으로 저장", "종료" }, UiCopy.FileMenuItems);
        Assert.Contains("URL 열기", UiCopy.FileMenuItems);
        Assert.Contains("다른 이름으로 저장", UiCopy.FileMenuItems);
        Assert.Contains("-10초", UiCopy.ViewMenuItems);
        Assert.Contains("+10초", UiCopy.ViewMenuItems);
    }

    [Fact]
    public void Transport_is_rewind_play_stop_clear_ff_seek_volume_time_hamburger()
    {
        var order = PlayerShell.Boot().Transport.Order;
        Assert.Equal(
            new[]
            {
                TransportControl.Rewind,
                TransportControl.PlayPause,
                TransportControl.Stop,
                TransportControl.Clear,
                TransportControl.FastForward,
                TransportControl.Seek,
                TransportControl.Volume,
                TransportControl.Time,
                TransportControl.Hamburger
            },
            order);
        Assert.Equal("1.0x", UiCopy.SpeedDefault);
        Assert.Equal("CC", UiCopy.Captions);
        Assert.Equal("지우기", UiCopy.Clear);
        Assert.True(PlayerShell.Boot().Transport.HasClear);
        Assert.True(PlayerShell.Boot().Transport.ClearImmediatelyRightOfStop);
        Assert.True(PlayerShell.Boot().Transport.ClearNeverMarksComplete);
        Assert.False(PlayerShell.Boot().Transport.HasEjectIcon);
        Assert.False(PlayerShell.Boot().Transport.CaptionsOnBar);
        Assert.False(PlayerShell.Boot().Transport.FullscreenOnBar);
        Assert.Contains("CC", UiCopy.ViewMenuItems);
        Assert.False(PlayerShell.Boot().Transport.HasRecordButton);
        Assert.DoesNotContain("Record", Enum.GetNames<TransportControl>());
        Assert.False(PlayerShell.Boot().Transport.NextEpisodeTextOnBar);
        Assert.False(PlayerShell.Boot().Capture.HasCameraOnTransport);
        Assert.DoesNotContain("Capture", Enum.GetNames<TransportControl>());
        Assert.True(PlayerShell.Boot().Transport.HasStop);
        Assert.False(PlayerShell.Boot().Transport.SkipLabelsOnBar);
    }

    [Fact]
    public void Next_episode_cta_is_overlay_end_region_only_not_on_transport()
    {
        var shell = PlayerShell.Boot();
        Assert.True(shell.NextEpisode.OverlayOnly);
        Assert.True(shell.NextEpisode.EndRegionOnly);
        Assert.False(shell.NextEpisode.OnTransport);
        Assert.False(shell.Transport.NextEpisodeTextOnBar);
        Assert.True(shell.Transport.NextEpisodeIconOnly);
        Assert.Equal(UiCopy.NextEpisodeIcon, shell.Transport.NextIcon);
        Assert.NotEqual(UiCopy.NextEpisode, shell.Transport.NextIcon);
        Assert.Equal("다음 화 >", shell.NextEpisode.Label);
        Assert.Equal("다음 화 >", UiCopy.NextEpisodeCta);
    }

    [Fact]
    public void Time_sits_on_the_transport_bar()
    {
        var shell = PlayerShell.Boot();
        Assert.False(shell.OverlayClock.AboveTransport);
        Assert.True(shell.OverlayClock.OnTransport);
        Assert.True(shell.Transport.TimeOnBar);
        Assert.Equal("00:00:00 / 00:00:00", shell.OverlayTime);
    }

    [Fact]
    public void Status_bar_is_hidden_until_a_failure_line()
    {
        var status = PlayerShell.Boot().Status;
        Assert.False(status.Visible);
        Assert.True(status.HideWhenIdle);
        Assert.True(status.FailureOnly);
        Assert.True(status.DashedSlot);
        Assert.Equal("", status.Text);

        status.Fail($"{UiCopy.Unsupported} · PRORES");
        Assert.True(status.Visible);
        Assert.StartsWith("미지원", status.Text);

        status.Clear();
        Assert.False(status.Visible);
        Assert.Equal("SW 폴백", UiCopy.SoftwareFallback);

        status.Fail("   ");
        Assert.False(status.Visible);
        Assert.Equal("", StatusText.Format(DecodePath.Hardware, "h264", "aac"));
        Assert.True(StatusText.IsConfirmedFailureLine(StatusText.Unsupported("prores")));
        Assert.True(StatusText.IsConfirmedFailureLine(StatusText.SoftwareFallback("h264", "aac")));
    }

    [Fact]
    public void Successful_open_clears_prior_failure_so_the_idle_bar_stays_hidden()
    {
        using var workspace = new TempWorkspace();
        var iso = workspace.File("disc.iso", [1]);
        var ok = workspace.File("ok.mkv", [1]);
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);

        session.Open(iso);
        Assert.True(session.Shell.Status.Visible);
        Assert.StartsWith("미지원", session.Shell.Status.Text);
        Assert.True(StatusText.IsConfirmedFailureLine(session.Shell.Status.Text));

        session.Open(ok);
        Assert.False(session.Shell.Status.Visible);
        Assert.Equal("", session.Shell.Status.Text);
    }

    [Fact]
    public void Fullscreen_b_is_overlay_cta_without_pin_or_center_play()
    {
        var shell = PlayerShell.Boot();
        shell.EnterFullscreen();
        Assert.Equal(ShellScreen.Fullscreen, shell.Screen);
        Assert.False(shell.Fullscreen.AlwaysOnTopPin);
        Assert.False(shell.Fullscreen.CenterPlayIcon);
        Assert.False(shell.Fullscreen.NextEpisodeTextOnBar);
        Assert.True(shell.Fullscreen.EndCtaIsOverlay);
        Assert.True(shell.Fullscreen.TransportIsFloatingOverlay);
        Assert.True(shell.Fullscreen.WindowedTransportIsDocked);
        Assert.Equal(0.80, shell.Fullscreen.TransportOverlayOpacity);
        Assert.Equal(16, shell.Fullscreen.TransportInsetPx);
        Assert.Equal(4, shell.Fullscreen.TransportRadiusPx);
        Assert.Equal(new[] { "Enter", "F11" }, shell.Fullscreen.ToggleKeys);
        Assert.Equal(3, shell.Fullscreen.IdleHideSeconds);
        Assert.True(shell.Fullscreen.StageDoubleClickToggles);
        Assert.False(shell.Fullscreen.TransportDoubleClickToggles);
        Assert.False(shell.Fullscreen.MenuDoubleClickToggles);
        Assert.Equal(TimeSpan.FromSeconds(3), FullscreenChromeController.IdleHide);
        Assert.True(shell.NextEpisode.OverlayOnly);
        Assert.False(shell.NextEpisode.OnTransport);
        Assert.False(shell.CenterPlayIcon);
        Assert.True(shell.VideoFullBleed);
        Assert.True(shell.NoLetterboxChrome);
    }

    [Fact]
    public void Open_sidebar_exposes_one_resume_and_recent_series()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var first = Path.Combine(season, "S01E01.mkv");
        File.WriteAllBytes(first, [1]);
        var engine = new FakeMediaEngine { Duration = 80 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.OpenSeriesFolder(workspace.Root);
        session.Open(first);
        session.SeekAbsolute(20);
        session.Checkpoint("pause");
        session.ToggleSidebar();

        Assert.True(session.Shell.Sidebar.Open);
        Assert.NotNull(session.Shell.Sidebar.Resume);
        Assert.Equal(first, session.Shell.Sidebar.Resume!.Path);
        Assert.Single(session.Shell.Sidebar.RecentSeries);
    }

    [Fact]
    public void Previous_and_next_icons_walk_the_flat_episode_list()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var first = Path.Combine(season, "S01E01.mkv");
        var second = Path.Combine(season, "S01E02.mkv");
        File.WriteAllBytes(first, [1]);
        File.WriteAllBytes(second, [2]);
        var engine = new FakeMediaEngine { Duration = 50 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.OpenSeriesFolder(workspace.Root);
        session.Open(first);
        session.Tick(DateTimeOffset.UtcNow);

        Assert.False(session.Shell.Transport.HasPrevious);
        Assert.True(session.Shell.Transport.HasNext);
        Assert.True(session.Shell.Transport.NextEpisodeIconOnly);

        session.PlayNextEpisode();
        Assert.Equal(second, session.Current!.Value.Path);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.True(session.Shell.Transport.HasPrevious);
        Assert.False(session.Shell.Transport.HasNext);

        session.PlayPreviousEpisode();
        Assert.Equal(first, session.Current!.Value.Path);
    }

    [Fact]
    public void Scanner_previous_episode_mirrors_next()
    {
        var episodes = new[]
        {
            new SeriesEpisode("/a/S01E01.mkv", 1, "S01E01.mkv", EpisodeParser.Parse("S01E01.mkv"), "S01"),
            new SeriesEpisode("/a/S01E02.mkv", 2, "S01E02.mkv", EpisodeParser.Parse("S01E02.mkv"), "S01")
        };
        Assert.Equal("/a/S01E02.mkv", SeriesScanner.NextEpisode(episodes, "/a/S01E01.mkv")!.Value.Path);
        Assert.Equal("/a/S01E01.mkv", SeriesScanner.PreviousEpisode(episodes, "/a/S01E02.mkv")!.Value.Path);
        Assert.Null(SeriesScanner.PreviousEpisode(episodes, "/a/S01E01.mkv"));
        Assert.Null(SeriesScanner.NextEpisode(episodes, "/a/S01E02.mkv"));
    }
}
