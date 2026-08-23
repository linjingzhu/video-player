using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class PlaybackSessionTests
{
    [Fact]
    public void Shell_boots_to_confirmed_p0_chrome()
    {
        var shell = PlayerShell.Boot();
        Assert.Equal(UiCopy.AppTitle, shell.Title);
        Assert.Equal(new[] { "퀵메뉴" }, shell.Menus);
        Assert.False(shell.Sidebar.Open);
        Assert.False(shell.CenterPlayIcon);
        Assert.False(shell.Fullscreen.AlwaysOnTopPin);
        Assert.False(shell.Series.PlaylistButton);
        Assert.False(shell.Status.Visible);
        Assert.True(shell.Status.FailureOnly);
        Assert.True(shell.Status.HideWhenIdle);
        Assert.True(shell.Status.DashedSlot);
        Assert.Equal("-10초", shell.Transport.SkipBackLabel);
        Assert.Equal("+10초", shell.Transport.SkipForwardLabel);
        Assert.False(shell.Transport.SkipLabelsOnBar);
        Assert.True(shell.Transport.NextEpisodeIconOnly);
        Assert.False(shell.Transport.NextEpisodeTextOnBar);
        Assert.True(shell.Transport.TimeOnBar);
        Assert.True(shell.HasHeaderUi);
        Assert.Equal("다음 화", shell.Fullscreen.NextEpisodeLabel);
        Assert.False(shell.Fullscreen.NextEpisodeTextOnBar);
        Assert.True(shell.Fullscreen.EndCtaIsOverlay);
        Assert.False(shell.Fullscreen.CenterPlayIcon);
        Assert.Equal(ShellScreen.Main, shell.Screen);
    }

    [Fact]
    public void Open_play_seek_speed_and_resume_round_trip()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("S01E01.mkv", [1, 2, 3, 4]);
        var engine = new FakeMediaEngine { Duration = 120 };
        var session = new PlaybackSession(engine, workspace.Data);

        var opened = session.Open(video);
        Assert.True(opened.Success);
        Assert.True(opened.AddedToRecent);
        Assert.True(opened.HardwareActive);
        Assert.False(session.Shell.Status.Visible);

        session.SeekRelative(10);
        Assert.Equal(10, engine.Position);
        session.SetSpeed(1.5);
        Assert.Equal(1.5, session.Speed);
        session.PlayPause();
        Assert.True(engine.IsPaused);

        var reopened = new PlaybackSession(new FakeMediaEngine { Duration = 120 }, workspace.Data);
        reopened.Open(video);
        Assert.Equal(10, reopened.Engine.Position);
    }

    [Fact]
    public void Hardware_failure_falls_back_to_software_and_keeps_playing()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("movie.mp4", [9]);
        var engine = new FakeMediaEngine { FailHardware = true };
        var session = new PlaybackSession(engine, workspace.Data);
        var opened = session.Open(video);

        Assert.True(opened.Success);
        Assert.False(engine.HardwareActive);
        Assert.True(session.Shell.Status.Visible);
        Assert.Contains("SW 폴백", session.Shell.Status.Text);
        var outcome = new HardwareDecodePolicy().OnHardwareFailed("h264", "aac");
        Assert.True(outcome.ContinuePlayback);
        Assert.Equal(DecodePath.Software, outcome.Path);
    }

    [Fact]
    public void Unsupported_codec_is_named_and_not_added_to_recent()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("raw.mov", [1]);
        var engine = new FakeMediaEngine { ForcedUnsupportedCodec = "prores" };
        var session = new PlaybackSession(engine, workspace.Data);
        var opened = session.Open(video);

        Assert.False(opened.Success);
        Assert.False(opened.AddedToRecent);
        Assert.Equal("PRORES", opened.UnsupportedCodecName);
        Assert.Contains("PRORES", opened.Status);
        Assert.Contains("미지원", opened.Status);
        Assert.True(session.Shell.Status.Visible);
        Assert.Empty(session.Recent.Items);
    }

    [Fact]
    public void Out_of_scope_container_is_not_recent()
    {
        using var workspace = new TempWorkspace();
        var iso = workspace.File("disc.iso", [1]);
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        var opened = session.Open(iso);
        Assert.False(opened.Success);
        Assert.False(string.IsNullOrWhiteSpace(opened.UnsupportedCodecName));
        Assert.Empty(session.Recent.Items);
    }

    [Fact]
    public void Drop_rejects_remote_and_opens_local()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ok.mkv", [1]);
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        var results = session.Drop(["https://example.com/x.mkv", video]);
        Assert.False(results[0].Success);
        Assert.True(results[1].Success);
    }

    [Fact]
    public void Speed_resets_to_one_on_new_session()
    {
        using var workspace = new TempWorkspace();
        var first = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        first.SetSpeed(2.0);
        var second = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Equal(1.0, second.Speed);
    }

    [Fact]
    public void Window_memory_sanitizes_and_persists_without_fullscreen()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.RememberWindow(new WindowBounds(40, 50, 1400, 900));
        var loaded = WindowMemory.FromJson(File.ReadAllText(Path.Combine(workspace.Data, "window.json")));
        Assert.Equal(1400, loaded.Bounds.Width);
        Assert.Equal(40, loaded.Bounds.X);
        Assert.Equal(WindowBounds.Default.Width, new WindowBounds(0, 0, -1, 10).Sanitize().Width);
    }

    [Fact]
    public void Fullscreen_chrome_hides_after_idle_but_stays_when_paused()
    {
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        Assert.True(FullscreenChromeController.ShouldShow(true, paused: true, start.AddSeconds(10), start));
        Assert.False(FullscreenChromeController.ShouldShow(true, paused: false, start.AddSeconds(4), start));
        Assert.True(FullscreenChromeController.ShouldShow(true, paused: false, start.AddSeconds(2), start));
        Assert.True(FullscreenChromeController.ShouldShow(false, paused: false, start.AddSeconds(30), start));
        Assert.Equal(TimeSpan.FromSeconds(3), FullscreenChromeController.IdleHide);
        Assert.Equal(3, SeriesOn.FullscreenIdleHideSeconds);
    }

    [Fact]
    public void Enter_and_f11_toggle_fullscreen_vs_windowed()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Equal(ShellScreen.Main, session.Shell.Screen);
        Assert.Equal(new[] { "Enter", "F11" }, session.Shell.Fullscreen.ToggleKeys);

        session.ToggleFullscreen();
        Assert.Equal(ShellScreen.Fullscreen, session.Shell.Screen);
        Assert.True(session.Shell.Fullscreen.TransportIsFloatingOverlay);
        Assert.Equal(0.80, session.Shell.Fullscreen.TransportOverlayOpacity);

        session.ToggleFullscreen();
        Assert.Equal(ShellScreen.Main, session.Shell.Screen);
        Assert.True(session.Shell.Fullscreen.WindowedTransportIsDocked);
    }

    [Fact]
    public void Sidecar_subtitle_loads_on_open()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ep.mkv", [1]);
        File.WriteAllText(Path.Combine(workspace.Root, "ep.srt"), """
            1
            00:00:00,000 --> 00:00:02,000
            자막 예시
            """);
        var engine = new FakeMediaEngine();
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        Assert.NotEmpty(session.Cues);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.Equal("자막 예시", session.Shell.OverlaySubtitle);
    }

    [Fact]
    public void Last_ten_seconds_does_not_auto_open_next_episode()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var first = Path.Combine(season, "S01E01.mkv");
        var second = Path.Combine(season, "S01E02.mkv");
        File.WriteAllBytes(first, [1]);
        File.WriteAllBytes(second, [2]);
        var engine = new FakeMediaEngine { Duration = 100 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.OpenSeriesFolder(workspace.Root);
        session.Open(first);
        engine.Seek(95);
        session.PlayPause();
        Assert.Equal(first, session.Current!.Value.Path);
        Assert.True(session.Resume.Find(first, new FileInfo(first).Length)!.Completed);
        Assert.Null(session.Resume.Continue);
    }

    [Fact]
    public void Natural_end_starts_three_second_cancel_then_advances()
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
        engine.Seek(50);
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        session.Tick(t0);
        Assert.True(session.AutoNextOffer.Pending);
        Assert.True(session.Shell.NextEpisode.ShowCta);
        Assert.Equal(first, session.Current!.Value.Path);

        session.Tick(t0.AddSeconds(2));
        Assert.Equal(first, session.Current.Value.Path);

        session.Tick(t0.AddSeconds(3));
        Assert.Equal(second, session.Current.Value.Path);
    }

    [Fact]
    public void Auto_next_can_be_cancelled()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        File.WriteAllBytes(Path.Combine(season, "S01E01.mkv"), [1]);
        File.WriteAllBytes(Path.Combine(season, "S01E02.mkv"), [2]);
        var engine = new FakeMediaEngine { Duration = 50 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.OpenSeriesFolder(workspace.Root);
        session.Open(Path.Combine(season, "S01E01.mkv"));
        engine.Seek(50);
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        session.Tick(t0);
        session.CancelAutoNext();
        session.Tick(t0.AddSeconds(4));
        Assert.EndsWith("S01E01.mkv", session.Current!.Value.Path);
    }
}

internal sealed class TempWorkspace : IDisposable
{
    public TempWorkspace()
    {
        Root = Directory.CreateTempSubdirectory("vp-test-").FullName;
        Data = Path.Combine(Root, "data");
        Directory.CreateDirectory(Data);
    }

    public string Root { get; }
    public string Data { get; }

    public string File(string name, byte[] bytes)
    {
        var path = Path.Combine(Root, name);
        System.IO.File.WriteAllBytes(path, bytes);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, true);
        }
        catch (IOException)
        {
        }
    }
}
