using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;
using VideoPlayer.Core.Subtitles;

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
        Assert.False(session.CanSaveAs);
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
        Assert.True(session.CanSaveAs);
        Assert.False(session.Shell.Series.Enabled);
        Assert.False(session.Shell.FileOnly.SeriesTree);
        Assert.False(session.Shell.FileOnly.AutoNext);
        Assert.False(session.Shell.FileOnly.Capture);
        Assert.False(session.Shell.FileOnly.ClipSave);
        Assert.False(session.Shell.FileOnly.SidecarAutoload);
        Assert.False(session.Shell.FileOnly.SecondaryEnglishSuggest);
        Assert.False(session.Shell.FileOnly.InventedSkipMarkers);
        Assert.False(session.CanAutoloadSidecars);
        Assert.False(session.CanSuggestSecondaryEnglish);
        Assert.False(session.InventsSkipMarkers);
        Assert.False(session.UsesIntroDb);
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
        Assert.Equal(new[] { "열기...", "URL 열기", "폴더 열기", "다른 이름으로 저장", "종료" }, UiCopy.FileMenuItems);
        Assert.Equal("URL 열기", UiCopy.OpenUrl);
        Assert.Equal("https://", UiCopy.OpenUrlPlaceholder);
        Assert.Equal("예: https://example.com/video.mp4", UiCopy.OpenUrlExample);
        Assert.Equal("http(s)만", UiCopy.OpenUrlHttpOnly);
        Assert.Equal("열기", UiCopy.OpenUrlAction);
        Assert.DoesNotContain(
            PlayerShell.Boot().Transport.Order,
            control => control.ToString().Contains("Url", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://")]
    [InlineData("not-a-url")]
    [InlineData("file:///C:/video.mp4")]
    [InlineData("rtmp://live.example/app")]
    [InlineData("ftp://example.com/a.mp4")]
    public void Invalid_url_disables_open_and_does_not_call_mpv(string? url)
    {
        var dialog = new OpenUrlDialogState();
        dialog.SetText(url);
        Assert.False(dialog.CanOpen);

        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine();
        var session = new PlaybackSession(engine, workspace.Data);
        var opened = session.OpenUrl(url);
        Assert.False(opened.Success);
        Assert.Equal(0, engine.OpenCallCount);
        Assert.Null(session.Current);
    }

    [Fact]
    public void Valid_http_url_enables_open_button_state()
    {
        var dialog = new OpenUrlDialogState();
        Assert.False(dialog.CanOpen);
        Assert.Equal("URL 열기", dialog.Title);
        Assert.Equal("https://", dialog.Placeholder);
        Assert.Equal("예: https://example.com/video.mp4", dialog.Example);
        Assert.Equal("http(s)만", dialog.HttpOnly);
        Assert.False(dialog.HasCookieAuthUi);
        Assert.False(dialog.HasDrmUi);
        Assert.False(dialog.HasPaidUnlockUi);
        Assert.False(dialog.HasHeaderUi);

        dialog.SetText("https://example.com/video.mp4");
        Assert.True(dialog.CanOpen);
        Assert.Equal("https://example.com/video.mp4", dialog.Text.Trim());
    }

    [Fact]
    public void Playback_network_failure_uses_dashed_banner_without_cookie_auth_ui()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine { FailOpen = true };
        var session = new PlaybackSession(engine, workspace.Data);
        var opened = session.OpenUrl("https://example.com/video.mp4");

        Assert.False(opened.Success);
        Assert.Equal(1, engine.OpenCallCount);
        Assert.True(session.Shell.Status.Visible);
        Assert.True(session.Shell.Status.DashedSlot);
        Assert.True(StatusText.IsConfirmedFailureLine(session.Shell.Status.Text));
        Assert.StartsWith("재생 실패", session.Shell.Status.Text);
        Assert.Contains("연결할 수 없습니다.", session.Shell.Status.Text);
        Assert.False(session.Shell.HasCookieAuthUi);
        Assert.False(session.Shell.HasDrmUi);
        Assert.False(session.Shell.HasPaidUnlockUi);
        Assert.False(session.Shell.HasHeaderUi);
        Assert.Empty(session.Recent.Items);
    }

    [Fact]
    public void Chapter_skip_uses_only_stream_chapters_and_never_invents_url_markers()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine { Duration = 120 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.OpenUrl("https://example.com/video.mp4");

        Assert.False(session.CanChapterSkip);
        Assert.False(session.SkipToNextChapter());
        Assert.False(session.UsesIntroDb);
        Assert.False(session.InventsSkipMarkers);
        Assert.False(UrlSkipPolicy.UsesIntroDb);
        Assert.False(UrlSkipPolicy.AllowsInventedMarkers(MediaSourceKind.HttpUrl));
        Assert.Empty(UrlSkipPolicy.ChaptersForSkip(MediaSourceKind.HttpUrl, []));

        engine.Chapters =
        [
            new MediaChapter("Intro", 0, 12),
            new MediaChapter("Main", 12, 100)
        ];
        session.Tick(DateTimeOffset.UtcNow);
        Assert.True(session.CanChapterSkip);
        Assert.True(session.SkipToNextChapter());
        Assert.Equal(12, engine.Position);
        Assert.True(session.SkipToPreviousChapter());
        Assert.Equal(0, engine.Position);
    }

    [Fact]
    public void Url_subtitles_are_embedded_or_user_picked_never_autoload_or_suggest_en()
    {
        using var workspace = new TempWorkspace();
        var sidecarDir = Path.Combine(workspace.Root, "beside");
        Directory.CreateDirectory(sidecarDir);
        File.WriteAllText(Path.Combine(sidecarDir, "video.en.srt"), """
            1
            00:00:00,000 --> 00:00:01,000
            English
            """);
        var picked = Path.Combine(workspace.Root, "picked.srt");
        File.WriteAllText(picked, """
            1
            00:00:00,000 --> 00:00:02,000
            사용자 자막
            """);

        var engine = new FakeMediaEngine
        {
            SubtitleTracks =
            [
                new MediaSubtitleTrack(1, "ko", "Korean", Embedded: true),
                new MediaSubtitleTrack(2, "en", "English", Embedded: false)
            ]
        };
        var session = new PlaybackSession(engine, workspace.Data);
        session.OpenUrl("https://example.com/show/video.mp4");

        Assert.Empty(session.Cues);
        Assert.Empty(SubtitleLocator.FindSidecars("https://example.com/show/video.mp4"));
        Assert.False(session.CanAutoloadSidecars);
        Assert.False(session.CanSuggestSecondaryEnglish);
        Assert.Null(SubtitleLocator.SuggestSecondary(MediaSourceKind.HttpUrl, [Path.Combine(sidecarDir, "video.en.srt")]));
        Assert.Equal(
            Path.Combine(sidecarDir, "video.en.srt"),
            SubtitleLocator.SuggestSecondary(MediaSourceKind.LocalFile, [Path.Combine(sidecarDir, "video.en.srt")]));
        Assert.Single(session.EmbeddedSubtitleTracks);
        Assert.Equal("ko", session.EmbeddedSubtitleTracks[0].Language);

        Assert.True(session.AttachUserSubtitle(picked));
        Assert.Equal(picked, session.UserSubtitlePath);
        Assert.NotEmpty(session.Cues);
        Assert.Equal("사용자 자막", session.Cues[0].Text);
    }
}
