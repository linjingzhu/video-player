using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Series;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class PlayerShellLayoutTests
{
    [Fact]
    public void Av2_sidebar_is_a_closed_36px_rail()
    {
        var shell = PlayerShell.Boot();
        Assert.False(shell.Sidebar.Open);
        Assert.Equal(36, shell.Sidebar.RailWidthPx);
        Assert.Equal(36, ShellLayout.SidebarRailWidthPx);
        Assert.Equal(0, shell.Sidebar.ContentWidthPx);
        Assert.True(shell.VideoFullWidth);
        Assert.False(shell.CenterPlayIcon);

        shell.Sidebar.Open = true;
        Assert.Equal(240, shell.Sidebar.ContentWidthPx);
    }

    [Fact]
    public void Menus_are_file_pipe_view_only()
    {
        var shell = PlayerShell.Boot();
        Assert.Equal(new[] { "파일", "보기" }, shell.Menus);
        Assert.Equal("|", shell.MenuSeparator);
    }

    [Fact]
    public void Transport_is_prev_skip_play_skip_next_icon_seek_volume_speed_cc_fullscreen()
    {
        var order = PlayerShell.Boot().Transport.Order;
        Assert.Equal(
            new[]
            {
                TransportControl.PreviousEpisode,
                TransportControl.SkipBack,
                TransportControl.PlayPause,
                TransportControl.SkipForward,
                TransportControl.NextEpisode,
                TransportControl.Seek,
                TransportControl.Volume,
                TransportControl.Speed,
                TransportControl.Captions,
                TransportControl.Fullscreen
            },
            order);
        Assert.Equal("1.0x", UiCopy.SpeedDefault);
        Assert.Equal("CC", UiCopy.Captions);
        Assert.DoesNotContain(order, control => control == TransportControl.NextEpisode && PlayerShell.Boot().Transport.NextEpisodeTextOnBar);
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
        Assert.Equal("다음 화 >", shell.NextEpisode.Label);
        Assert.Equal("다음 화 >", UiCopy.NextEpisodeCta);
    }

    [Fact]
    public void Time_overlay_sits_above_transport_on_the_video()
    {
        var shell = PlayerShell.Boot();
        Assert.True(shell.OverlayClock.AboveTransport);
        Assert.Equal(OverlayAnchor.BottomLeft, shell.OverlayClock.Anchor);
        Assert.False(shell.Transport.TimeOnBar);
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
    }

    [Fact]
    public void Fullscreen_keeps_overlay_cta_and_drops_old_a_chrome()
    {
        var shell = PlayerShell.Boot();
        shell.EnterFullscreen();
        Assert.Equal(ShellScreen.Fullscreen, shell.Screen);
        Assert.False(shell.Fullscreen.AlwaysOnTopPin);
        Assert.False(shell.Fullscreen.CenterPlayIcon);
        Assert.False(shell.Fullscreen.NextEpisodeTextOnBar);
        Assert.True(shell.Fullscreen.EndCtaIsOverlay);
        Assert.False(shell.CenterPlayIcon);
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
