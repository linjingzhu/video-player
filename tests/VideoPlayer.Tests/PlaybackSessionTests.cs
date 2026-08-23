using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class PlaybackSessionTests
{
    [Fact]
    public void Shell_boots_with_wireframe_menus_and_transport()
    {
        var shell = PlayerShell.Boot();
        Assert.Equal(UiCopy.AppTitle, shell.Title);
        Assert.Equal(new[] { "파일", "재생", "시리즈", "보기", "도움" }, shell.Menus);
        Assert.Equal("최근 / 시리즈", shell.Sidebar.Title);
        Assert.Contains(UiCopy.ContinueWatching, shell.Sidebar.Items);
        Assert.Equal("-10초", shell.Transport.SkipBackLabel);
        Assert.Equal("+10초", shell.Transport.SkipForwardLabel);
        Assert.Equal("다음 화 >", shell.Fullscreen.NextEpisodeLabel);
        Assert.Equal("폴더 열기", shell.Series.OpenFolderLabel);
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
        Assert.Contains("H.264", opened.Status);
        Assert.Contains("AAC", opened.Status);

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
        Assert.Contains("소프트웨어", session.Shell.Status.Text);
        Assert.DoesNotContain("실패하여 중지", session.Shell.Status.Text);
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
