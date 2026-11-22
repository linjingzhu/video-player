using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class YouTubeTests
{
    private const string Id = "abcdeFGHI_1";

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=abcdeFGHI_1")]
    [InlineData("https://youtube.com/watch?v=abcdeFGHI_1")]
    [InlineData("https://m.youtube.com/watch?v=abcdeFGHI_1")]
    [InlineData("https://music.youtube.com/watch?v=abcdeFGHI_1")]
    [InlineData("https://youtu.be/abcdeFGHI_1")]
    [InlineData("https://www.youtube.com/shorts/abcdeFGHI_1")]
    [InlineData("https://m.youtube.com/shorts/abcdeFGHI_1")]
    [InlineData("https://www.youtube.com/watch?list=PLzzzzzzzzzz&v=abcdeFGHI_1")]
    public void Detects_youtube_links_and_returns_the_video_id(string url)
    {
        Assert.True(YouTubeUrl.IsYouTube(url));
        Assert.Equal(Id, YouTubeUrl.TryGetVideoId(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://example.com/watch?v=abcdeFGHI_1")]
    [InlineData("https://www.youtube.com/playlist?list=PLzzzzzzzzzz")]
    [InlineData("https://music.youtube.com/playlist?list=PLzzzzzzzzzz")]
    [InlineData("https://www.youtube.com/embed/abcdeFGHI_1")]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("https://youtu.be/")]
    public void Non_youtube_returns_null(string? url)
        => Assert.Null(YouTubeUrl.TryGetVideoId(url));

    [Fact]
    public void List_query_is_not_part_of_the_video_id()
    {
        const string url = "https://www.youtube.com/watch?v=abcdeFGHI_1&list=PLzzzzzzzzzz";
        var id = YouTubeUrl.TryGetVideoId(url);
        Assert.Equal(Id, id);
        Assert.DoesNotContain("list", id, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Yt_dlp_is_resolved_beside_the_executable_then_on_path()
    {
        var exeDir = Path.Combine("app", "Ieseo");
        var pathDir = Path.Combine("tools", "bin");
        var otherDir = Path.Combine("other", "bin");
        var beside = Path.Combine(exeDir, YtDlpLocator.ExecutableName);
        var onPath = Path.Combine(pathDir, YtDlpLocator.ExecutableName);
        var calls = new List<string>();

        bool Exists(string path)
        {
            calls.Add(path);
            return false;
        }

        Assert.Null(YtDlpLocator.Resolve(exeDir, [pathDir, "", otherDir], Exists));
        Assert.Equal([beside, onPath, Path.Combine(otherDir, YtDlpLocator.ExecutableName)], calls);

        calls.Clear();
        Assert.Equal(beside, YtDlpLocator.Resolve(exeDir, [pathDir], path => path == beside));
        Assert.Equal(onPath, YtDlpLocator.Resolve(exeDir, [otherDir, pathDir], path => path == onPath));
        Assert.Null(YtDlpLocator.Resolve(exeDir, [pathDir], _ => false));
    }

    [Fact]
    public void Resume_key_is_the_video_id_and_plain_urls_keep_the_url()
    {
        const string watch = "https://www.youtube.com/watch?v=abcdeFGHI_1&list=PLzzzzzzzzzz";
        const string shortLink = "https://youtu.be/abcdeFGHI_1?si=tracking";
        const string shorts = "https://www.youtube.com/shorts/abcdeFGHI_1";
        const string music = "https://music.youtube.com/watch?v=abcdeFGHI_1&list=PLzzzzzzzzzz";
        const string plain = "https://example.com/video.mp4";

        Assert.Equal(Id, ResumeKey.FromUrl(watch));
        Assert.Equal(Id, ResumeKey.FromUrl(shortLink));
        Assert.Equal(Id, MediaIdentity.FromUrl(shorts).Key);
        Assert.Equal(Id, ResumeKey.FromUrl(music));
        Assert.Equal(watch, MediaIdentity.FromUrl(watch).Path);
        Assert.NotEqual(Id, ResumeKey.FromUrl("https://youtu.be/abcdeFGHI_2"));
        Assert.Equal(plain, ResumeKey.FromUrl(plain));
        Assert.Equal(plain, MediaIdentity.FromUrl("  " + plain + "  ").Key);
    }

    [Fact]
    public void Mpv_options_enable_ytdl_without_cookies()
    {
        const string path = @"C:\Player\yt-dlp.exe";
        var options = YouTubeMpvOptions.For(path);

        Assert.Equal(
            [
                ("ytdl", "yes"),
                ("script-opts", "ytdl_hook-ytdl_path=" + path),
                ("ytdl-raw-options", "no-playlist=")
            ],
            options);
        Assert.DoesNotContain(options, option =>
            option.Name.Contains("cookie", StringComparison.OrdinalIgnoreCase)
            || option.Value.Contains("cookie", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Missing_yt_dlp_stays_in_the_failure_slot_and_does_not_open()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine();
        var session = new PlaybackSession(engine, workspace.Data, ytdlpResolver: () => null);
        var opened = session.OpenUrl("https://youtu.be/" + Id);

        Assert.False(opened.Success);
        Assert.Equal(0, engine.OpenCallCount);
        Assert.Equal(UiCopy.YtDlpRequired, session.Shell.Status.Text);
        Assert.True(session.Shell.Status.Visible);
        Assert.True(session.Shell.Status.DashedSlot);
        Assert.True(StatusText.IsConfirmedFailureLine(session.Shell.Status.Text));
        Assert.Null(session.Current);
        Assert.Empty(session.Recent.Items);
        Assert.DoesNotContain("불러오는", session.Shell.Status.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Youtube_streams_without_recent_capture_or_save_and_titles_from_media_title()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine { Duration = 100 };
        var session = new PlaybackSession(engine, workspace.Data, ytdlpResolver: () => @"C:\Player\yt-dlp.exe");
        const string url = "https://www.youtube.com/watch?v=abcdeFGHI_1&list=PLzzzzzzzzzz";

        Assert.True(session.OpenUrl(url).Success);
        Assert.Equal(@"C:\Player\yt-dlp.exe", engine.LastYtDlpPath);
        Assert.Equal(UiCopy.YouTubeLoadingTitle, session.TitleCaption);
        Assert.DoesNotContain("불러오는", session.Shell.Status.Text, StringComparison.Ordinal);
        Assert.Empty(session.Recent.Items);
        Assert.False(session.CanCapture);
        Assert.False(session.CanClipSave);
        Assert.False(session.CanSaveAs);
        session.OpenCaptureSheet();
        session.OpenClipSheet();
        Assert.False(session.Shell.Capture.Open);
        Assert.False(session.Shell.Clip.Open);

        engine.MediaTitle = url;
        Assert.Equal(UiCopy.YouTubeLoadingTitle, session.TitleCaption);
        engine.MediaTitle = "영상 제목";
        Assert.Equal("영상 제목", session.TitleCaption);
        Assert.DoesNotContain("불러오는", session.Shell.Status.Text, StringComparison.Ordinal);

        var menu = RecentFilesMenu.Build(
            [new RecentItem(url, 0, "yt", DateTimeOffset.UnixEpoch)],
            _ => true);
        Assert.Empty(menu);
    }

    [Fact]
    public void Youtube_load_failure_uses_the_failure_slot()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine { FailOpen = true };
        var session = new PlaybackSession(engine, workspace.Data, ytdlpResolver: () => "yt-dlp.exe");
        var opened = session.OpenUrl("https://www.youtube.com/shorts/" + Id);

        Assert.False(opened.Success);
        Assert.Equal(1, engine.OpenCallCount);
        Assert.StartsWith(UiCopy.PlaybackFailed, session.Shell.Status.Text);
        Assert.Contains(UiCopy.YouTubeLoadFailed, session.Shell.Status.Text);
        Assert.Empty(session.Recent.Items);

        engine.FailOpen = false;
        engine.ReportYouTubeFailureOnPump = true;
        Assert.True(session.OpenUrl("https://music.youtube.com/watch?v=" + Id).Success);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.Contains(UiCopy.YouTubeLoadFailed, session.Shell.Status.Text);
        Assert.DoesNotContain(UiCopy.YouTubeLoadingTitle, session.Shell.Status.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Resume_position_follows_the_video_id_across_url_shapes()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine { Duration = 100 };
        var session = new PlaybackSession(engine, workspace.Data, ytdlpResolver: () => "yt-dlp.exe");
        Assert.True(session.OpenUrl("https://youtu.be/" + Id).Success);
        session.SeekAbsolute(25);
        session.Checkpoint("pause");

        var stored = session.Resume.FindUrl("https://www.youtube.com/watch?v=" + Id + "&list=PLzzzzzzzzzz");
        Assert.NotNull(stored);
        Assert.Equal(Id, stored!.Key);
        Assert.Equal(25, stored.PositionSeconds);

        var again = new PlaybackSession(new FakeMediaEngine { Duration = 100 }, workspace.Data, ytdlpResolver: () => "yt-dlp.exe");
        again.OpenUrl("https://m.youtube.com/shorts/" + Id);
        Assert.Equal(25, again.Engine.Position);
        again.OpenUrl("https://youtu.be/abcdeFGHI_2");
        Assert.Equal(0, again.Engine.Position);

        var json = session.Resume.ToJson();
        Assert.NotNull(ResumeStore.FromJson(json).FindUrl("https://music.youtube.com/watch?v=" + Id));
        var tampered = json.Replace("\"key\": \"" + Id + "\"", "\"key\": \"zzzzzzzzzzz\"", StringComparison.Ordinal);
        Assert.Null(ResumeStore.FromJson(tampered).FindUrl("https://youtu.be/" + Id));
    }

    [Fact]
    public void Plain_http_save_as_stays_available()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data, ytdlpResolver: () => null);
        Assert.True(session.OpenUrl("https://example.com/video.mp4").Success);
        Assert.True(session.CanSaveAs);
        Assert.False(session.IsYouTube);
        Assert.Empty(session.Recent.Items);
    }
}
