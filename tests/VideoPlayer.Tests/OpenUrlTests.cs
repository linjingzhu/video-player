using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class OpenUrlTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/video.mp4")]
    [InlineData("rtsp://camera/1")]
    [InlineData("javascript:alert(1)")]
    public void Rejects_non_http_urls(string? url)
    {
        var result = OpenUrlRules.Validate(url);
        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Theory]
    [InlineData("file:///C:/video.mp4", "file:")]
    [InlineData("FILE://localhost/tmp/a.mkv", "file:")]
    [InlineData("rtmp://live.example/app/stream", "rtmp")]
    [InlineData("rtmps://live.example/app/stream", "rtmp")]
    [InlineData("https://example.com/video.mp4\nCookie: secret=1", "쿠키")]
    [InlineData("https://example.com/video.mp4 --http-header Referer: https://evil", "쿠키")]
    [InlineData("https://user:pass@example.com/video.mp4", "로그인")]
    public void Rejects_file_rtmp_cookies_headers_and_login(string url, string expected)
    {
        var result = OpenUrlRules.Validate(url);
        Assert.False(result.Success);
        Assert.Contains(expected, result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://example.com/video.mp4")]
    [InlineData("http://cdn.example/a.mkv")]
    [InlineData("  https://example.com/video.mp4?token=1  ")]
    public void Accepts_http_and_https_only(string url)
    {
        var result = OpenUrlRules.Validate(url);
        Assert.True(result.Success);
        Assert.Equal(url.Trim(), result.FullPath);
    }

    [Fact]
    public void Session_rejects_non_http_into_the_dashed_status_slot()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        var opened = session.OpenUrl("rtmp://live.example/app");

        Assert.False(opened.Success);
        Assert.False(opened.AddedToRecent);
        Assert.True(session.Shell.Status.Visible);
        Assert.True(session.Shell.Status.DashedSlot);
        Assert.Contains("rtmp", session.Shell.Status.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(session.Recent.Items);
        Assert.Null(session.Current);
    }

    [Fact]
    public void File_open_and_drop_still_reject_urls()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ok.mkv", [1]);
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);

        var opened = session.Open("https://example.com/video.mp4");
        Assert.False(opened.Success);

        var dropped = session.Drop(["https://example.com/video.mp4", video]);
        Assert.False(dropped[0].Success);
        Assert.True(dropped[1].Success);
        Assert.Equal(MediaSourceKind.LocalFile, session.SourceKind);
    }

    [Fact]
    public void Resume_key_is_the_exact_url_string_without_size()
    {
        const string url = "https://example.com/video.mp4";
        var identity = MediaIdentity.FromUrl(url);

        Assert.Equal(url, identity.Key);
        Assert.Equal(0, identity.Size);
        Assert.Equal(MediaSourceKind.HttpUrl, identity.Kind);
        Assert.NotEqual(ResumeKey.From(url, 0), identity.Key);
        Assert.Equal(url, ResumeKey.FromUrl("  https://example.com/video.mp4  "));
        Assert.NotEqual(
            MediaIdentity.FromUrl("https://example.com/video.mp4").Key,
            MediaIdentity.FromUrl("https://example.com/video.mp4?x=1").Key);
    }

    [Fact]
    public void Resume_by_url_restores_position_and_uses_last_ten_complete_rule()
    {
        using var workspace = new TempWorkspace();
        const string url = "https://example.com/show/S01E01.mkv";
        var engine = new FakeMediaEngine { Duration = 100 };
        var session = new PlaybackSession(engine, workspace.Data);

        Assert.True(session.OpenUrl(url).Success);
        session.SeekAbsolute(25);
        session.Checkpoint("pause");

        var stored = session.Resume.FindUrl(url);
        Assert.NotNull(stored);
        Assert.Equal(url, stored!.Key);
        Assert.Equal(url, stored.Path);
        Assert.Equal(0, stored.Size);
        Assert.Equal(25, stored.PositionSeconds);

        var reopened = new PlaybackSession(new FakeMediaEngine { Duration = 100 }, workspace.Data);
        reopened.OpenUrl(url);
        Assert.Equal(25, reopened.Engine.Position);
        Assert.Equal(url, reopened.Current!.Value.Path);
        Assert.Equal(MediaSourceKind.HttpUrl, reopened.SourceKind);

        var complete = CompletionPolicy.Checkpoint(MediaIdentity.FromUrl(url), 91, 100);
        Assert.True(complete.CurrentCompleted);
        Assert.Equal(0, complete.Current.PositionSeconds);
        Assert.Equal(url, complete.Current.Key);
    }

    [Fact]
    public void Resume_store_persists_https_and_still_rejects_other_remotes()
    {
        var store = new ResumeStore();
        store.Apply(CompletionPolicy.Checkpoint(MediaIdentity.FromUrl("https://ok.example/a.mp4"), 12, 80));
        var json = store.ToJson();
        var loaded = ResumeStore.FromJson(json);
        Assert.NotNull(loaded.FindUrl("https://ok.example/a.mp4"));

        var tampered = json.Replace(
            "\"path\": \"https://ok.example/a.mp4\"",
            "\"path\": \"rtmp://evil.example/a.mp4\"",
            StringComparison.Ordinal);
        Assert.Empty(ResumeStore.FromJson(tampered).Entries);
    }

    [Fact]
    public void File_only_features_stay_off_for_url_sources()
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

        Assert.True(session.CanUseSeriesTree);
        Assert.True(session.CanAutoNext);
        Assert.True(session.CanCapture);
        Assert.True(session.CanClipSave);
        Assert.True(session.Shell.Series.Enabled);
        Assert.True(session.Shell.FileOnly.Capture);
        Assert.True(session.Shell.FileOnly.ClipSave);

        const string url = "https://example.com/video.mp4";
        Assert.True(session.OpenUrl(url).Success);
        Assert.Equal(MediaSourceKind.HttpUrl, session.SourceKind);
        Assert.False(session.CanUseSeriesTree);
        Assert.False(session.CanAutoNext);
        Assert.False(session.CanCapture);
        Assert.False(session.CanClipSave);
        Assert.False(session.Shell.Series.Enabled);
        Assert.False(session.Shell.FileOnly.SeriesTree);
        Assert.False(session.Shell.FileOnly.AutoNext);
        Assert.False(session.Shell.FileOnly.Capture);
        Assert.False(session.Shell.FileOnly.ClipSave);
        Assert.False(FileOnlyFeatures.Allows(session.SourceKind, FileOnlyFeature.SeriesTree));
        Assert.False(FileOnlyFeatures.Allows(session.SourceKind, FileOnlyFeature.AutoNext));
        Assert.False(FileOnlyFeatures.Allows(session.SourceKind, FileOnlyFeature.Capture));
        Assert.False(FileOnlyFeatures.Allows(session.SourceKind, FileOnlyFeature.ClipSave));

        engine.Seek(50);
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        session.Tick(t0);
        Assert.False(session.AutoNextOffer.Pending);
        Assert.False(session.Shell.NextEpisode.ShowCta);
        session.Tick(t0.AddSeconds(4));
        Assert.Equal(url, session.Current!.Value.Path);

        session.PlayNextEpisode();
        Assert.Equal(url, session.Current.Value.Path);
        Assert.False(session.Shell.Transport.HasNext);
        Assert.False(session.Shell.Transport.HasPrevious);
    }

    [Fact]
    public void Unsupported_url_codec_is_named_and_not_added_to_recent()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine { ForcedUnsupportedCodec = "prores" };
        var session = new PlaybackSession(engine, workspace.Data);
        var opened = session.OpenUrl("https://example.com/raw.mov");

        Assert.False(opened.Success);
        Assert.False(opened.AddedToRecent);
        Assert.Contains("PRORES", opened.Status);
        Assert.Contains("미지원", opened.Status);
        Assert.True(session.Shell.Status.Visible);
        Assert.Empty(session.Recent.Items);
        Assert.Null(session.Current);
    }

    [Fact]
    public void Successful_url_is_not_added_to_recent()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        var opened = session.OpenUrl("https://example.com/video.mp4");
        Assert.True(opened.Success);
        Assert.False(opened.AddedToRecent);
        Assert.Empty(session.Recent.Items);
    }

    [Fact]
    public void File_menu_has_open_url_and_transport_does_not()
    {
        Assert.Equal(new[] { "열기...", "URL 열기", "폴더 열기", "종료" }, UiCopy.FileMenuItems);
        Assert.Equal("URL 열기", UiCopy.OpenUrl);
        Assert.Equal("https://", UiCopy.OpenUrlPlaceholder);
        Assert.Equal("예: https://example.com/video.mp4", UiCopy.OpenUrlExample);
        Assert.Equal("http(s)만", UiCopy.OpenUrlHttpOnly);
        Assert.Equal("열기", UiCopy.OpenUrlAction);
        Assert.DoesNotContain(
            PlayerShell.Boot().Transport.Order,
            control => control.ToString().Contains("Url", StringComparison.OrdinalIgnoreCase));
    }
}
