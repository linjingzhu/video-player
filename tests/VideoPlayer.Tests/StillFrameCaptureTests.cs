using VideoPlayer.Core.Capture;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class StillFrameCaptureTests
{
    [Fact]
    public void Sheet_defaults_and_tokens_match_confirmed_capture()
    {
        var sheet = PlayerShell.Boot().Capture;
        Assert.False(sheet.Open);
        Assert.Equal(1, sheet.Count);
        Assert.Equal(1, sheet.IntervalFrames);
        Assert.Equal(CaptureFormat.Png, sheet.Format);
        Assert.Equal(new[] { CaptureFormat.Png, CaptureFormat.Jpg, CaptureFormat.Webp }, sheet.Formats);
        Assert.Equal("Pictures", StillFrameCapture.PicturesLabel);
        Assert.Equal("Pictures", sheet.FolderLabel);
        Assert.Equal("1-999", sheet.CountRange);
        Assert.Equal("1프레임", sheet.IntervalText);
        Assert.Equal("캡처 (still frames, not video)", sheet.Title);
        Assert.Equal("현재 위치부터 · 캡처 중 일시정지", sheet.Footer);
        Assert.Equal("시작", sheet.StartLabel);
        Assert.Equal("취소", sheet.CancelLabel);
        Assert.Equal("변경", sheet.ChangeFolderLabel);
        Assert.Equal("#141418", sheet.PanelColor);
        Assert.Equal("#0A84FF", sheet.StartColor);
        Assert.Equal(SkinA.Panel, sheet.PanelColor);
        Assert.Equal(SkinA.Accent, sheet.StartColor);
        Assert.Equal(12, sheet.PanelRadius);
        Assert.Equal(999, sheet.StartRadius);
        Assert.True(sheet.StillFramesOnly);
        Assert.False(sheet.HasCameraOnTransport);
        Assert.True(SkinA.NoEmoji);
        Assert.Equal("캡처", UiCopy.Capture);
        Assert.Equal("Ctrl+Shift+C", UiCopy.CaptureShortcut);
        Assert.Equal(90, StillFrameCapture.JpegQuality);
        Assert.Equal(80, StillFrameCapture.WebpQuality);
    }

    [Fact]
    public void Transport_has_no_camera_or_capture_control()
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
        Assert.Equal(10, order.Count);
        Assert.False(Enum.GetNames<TransportControl>().Contains("Capture"));
        Assert.False(Enum.GetNames<TransportControl>().Contains("Camera"));
        Assert.False(PlayerShell.Boot().Capture.HasCameraOnTransport);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(999, 999)]
    [InlineData(1000, 999)]
    [InlineData(-4, 1)]
    public void Count_clamps_to_one_through_999(int input, int expected)
        => Assert.Equal(expected, StillFrameCapture.ClampCount(input));

    [Fact]
    public void Confirm_starts_at_sixty()
    {
        Assert.False(StillFrameCapture.NeedsConfirm(1));
        Assert.False(StillFrameCapture.NeedsConfirm(59));
        Assert.True(StillFrameCapture.NeedsConfirm(60));
        Assert.True(StillFrameCapture.NeedsConfirm(999));
        Assert.True(PlayerShell.Boot().Capture is { Count: 1, NeedsConfirm: false });
        var sheet = PlayerShell.Boot().Capture;
        sheet.Count = 60;
        Assert.True(sheet.NeedsConfirm);
        Assert.Equal("60장 이상을 캡처합니다. 계속할까요?", UiCopy.CaptureConfirm);
    }

    [Fact]
    public void Filename_is_stem_hhmmss_index_and_extension()
    {
        var at = TimeSpan.Parse("00:12:04");
        Assert.Equal("lighthouse_001204_0001.png", StillFrameCapture.FileName("lighthouse", at, 1, CaptureFormat.Png));
        Assert.Equal("lighthouse_001204_0002.jpg", StillFrameCapture.FileName("lighthouse", at, 2, CaptureFormat.Jpg));
        Assert.Equal("show_000000_0001.webp", StillFrameCapture.FileName("show", TimeSpan.Zero, 1, CaptureFormat.Webp));
        Assert.Equal("safe_name_000010_0001.png", StillFrameCapture.FileName("safe/name", TimeSpan.FromSeconds(10), 1, CaptureFormat.Png));
        Assert.Equal("png", CaptureFormats.Extension(CaptureFormat.Png));
        Assert.Equal("jpg", CaptureFormats.Extension(CaptureFormat.Jpg));
        Assert.Equal("webp", CaptureFormats.Extension(CaptureFormat.Webp));
        Assert.Equal(CaptureFormat.Png, CaptureFormats.Parse(null));
        Assert.Equal(90, CaptureFormats.Quality(CaptureFormat.Jpg));
        Assert.Equal(80, CaptureFormats.Quality(CaptureFormat.Webp));
    }

    [Fact]
    public void Folder_label_is_pictures_until_changed()
    {
        Assert.Equal("Pictures", StillFrameCapture.FolderLabel(StillFrameCapture.DefaultFolderPath()));
        using var workspace = new TempWorkspace();
        var other = Path.Combine(workspace.Root, "Stills");
        Directory.CreateDirectory(other);
        Assert.Equal("Stills", StillFrameCapture.FolderLabel(other));
    }

    [Fact]
    public void Capture_pauses_and_starts_from_current_position()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("lighthouse.mkv", [1]);
        var dest = Path.Combine(workspace.Root, "out");
        var engine = new FakeMediaEngine { Duration = 120 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        Assert.False(engine.IsPaused);
        session.SeekAbsolute(12 * 60 + 4);
        session.SetCaptureFolder(dest);
        session.Shell.Capture.Count = 1;
        session.Shell.Capture.Format = CaptureFormat.Png;

        var result = session.RunStillCapture();

        Assert.True(result.Paused);
        Assert.True(engine.IsPaused);
        Assert.False(session.Shell.Capture.Open);
        Assert.Equal(1, result.Saved);
        Assert.False(result.HitEnd);
        Assert.Equal("", result.Banner);
        Assert.Single(engine.CapturedPaths);
        Assert.Equal("lighthouse_001204_0001.png", Path.GetFileName(engine.CapturedPaths[0]));
        Assert.True(File.Exists(engine.CapturedPaths[0]));
        Assert.Equal(12 * 60 + 4, engine.Position, 3);
    }

    [Fact]
    public void Interval_steps_n_frames_between_stills()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("clip.mp4", [1]);
        var dest = Path.Combine(workspace.Root, "frames");
        var engine = new FakeMediaEngine { Duration = 10, FrameDuration = 0.1 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(1.0);
        session.SetCaptureFolder(dest);
        session.Shell.Capture.Count = 3;
        session.Shell.Capture.IntervalFrames = 2;
        session.Shell.Capture.Format = CaptureFormat.Jpg;

        var result = session.RunStillCapture();

        Assert.Equal(3, result.Saved);
        Assert.Equal(1.0 + (2 * 0.1) + (2 * 0.1), engine.Position, 6);
        Assert.Equal("clip_000001_0001.jpg", Path.GetFileName(result.Files[0]));
        Assert.Equal("clip_000001_0002.jpg", Path.GetFileName(result.Files[1]));
        Assert.Equal("clip_000001_0003.jpg", Path.GetFileName(result.Files[2]));
        Assert.All(result.Files, path => Assert.EndsWith(".jpg", path, StringComparison.Ordinal));
    }

    [Fact]
    public void Eof_saves_what_is_possible_and_shows_info_banner()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("end.mkv", [1]);
        var dest = Path.Combine(workspace.Root, "tail");
        var engine = new FakeMediaEngine { Duration = 1.0, FrameDuration = 0.25 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(0.5);
        session.SetCaptureFolder(dest);
        session.Shell.Capture.Count = 8;
        session.OpenCaptureSheet();
        Assert.True(session.Shell.Capture.Open);

        var result = session.RunStillCapture();

        Assert.True(result.HitEnd);
        Assert.True(result.Saved < 8);
        Assert.True(result.Saved >= 1);
        Assert.Equal(CaptureBannerKind.Info, result.BannerKind);
        Assert.Equal(string.Format(UiCopy.CaptureEofBanner, result.Saved, 8), result.Banner);
        Assert.True(session.Shell.CaptureBanner.Visible);
        Assert.Equal(CaptureBannerKind.Info, session.Shell.CaptureBanner.Kind);
        Assert.Contains("파일 끝", session.Shell.CaptureBanner.Text, StringComparison.Ordinal);
        Assert.False(session.Shell.Capture.Open);
        Assert.True(engine.IsPaused);
    }

    [Fact]
    public void Screenshot_failure_keeps_saved_frames_and_failure_banner()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("fail.mkv", [1]);
        var dest = Path.Combine(workspace.Root, "partial");
        var engine = new FakeMediaEngine { Duration = 20 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SetCaptureFolder(dest);
        session.Shell.Capture.Count = 3;
        engine.FailScreenshot = true;

        var result = session.RunStillCapture();

        Assert.Equal(0, result.Saved);
        Assert.Equal(CaptureBannerKind.Failure, result.BannerKind);
        Assert.Equal(UiCopy.CaptureSaveFailed, result.Banner);
        Assert.True(session.Shell.CaptureBanner.Visible);
        Assert.Equal(CaptureBannerKind.Failure, session.Shell.CaptureBanner.Kind);
    }

    [Fact]
    public void No_media_shows_failure_banner()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.OpenCaptureSheet();
        var result = session.RunStillCapture();
        Assert.Equal(0, result.Saved);
        Assert.Equal(UiCopy.CaptureNoMedia, result.Banner);
        Assert.Equal(CaptureBannerKind.Failure, session.Shell.CaptureBanner.Kind);
        Assert.True(session.Shell.Capture.Open);
    }

    [Fact]
    public void Remote_folder_is_rejected()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.False(session.SetCaptureFolder("https://example.com/stills"));
        Assert.Equal("Pictures", session.Shell.Capture.FolderLabel);
    }

    [Fact]
    public void Tick_does_not_auto_next_while_capturing()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var first = Path.Combine(season, "S01E01.mkv");
        var second = Path.Combine(season, "S01E02.mkv");
        File.WriteAllBytes(first, [1]);
        File.WriteAllBytes(second, [2]);
        var engine = new FakeMediaEngine { Duration = 1.0, FrameDuration = 0.05 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.OpenSeriesFolder(workspace.Root);
        session.Open(first);
        engine.Seek(1.0);
        session.SetCaptureFolder(Path.Combine(workspace.Root, "cap"));
        session.Shell.Capture.Count = 2;

        var result = session.RunStillCapture();
        Assert.True(result.Saved >= 1);
        Assert.EndsWith("S01E01.mkv", session.Current!.Value.Path);
        session.Tick(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        Assert.EndsWith("S01E01.mkv", session.Current.Value.Path);
    }
}
