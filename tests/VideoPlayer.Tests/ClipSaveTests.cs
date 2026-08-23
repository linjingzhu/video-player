using VideoPlayer.Core.Clip;
using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class ClipSaveTests
{
    [Fact]
    public void Sheet_v4_lock_copy_tokens_and_formats()
    {
        var sheet = PlayerShell.Boot().Clip;
        Assert.False(sheet.Open);
        Assert.Equal(ClipFormat.StreamCopy, sheet.Format);
        Assert.Equal(
            new[] { ClipFormat.StreamCopy, ClipFormat.Webp, ClipFormat.Gif },
            sheet.Formats);
        Assert.Equal("구간 저장", sheet.Title);
        Assert.Equal("시작", sheet.StartLabel);
        Assert.Equal("끝", sheet.EndLabel);
        Assert.Equal("현재를 시작", sheet.SetStartFromNowLabel);
        Assert.Equal("현재를 끝", sheet.SetEndFromNowLabel);
        Assert.True(sheet.HasSheetCurrentMarks);
        Assert.True(sheet.HasKeyboardIoMarks);
        Assert.False(sheet.HasVideoDragSelect);
        Assert.False(ClipSave.HasVideoDragSelect);
        Assert.True(ClipSave.HasSheetCurrentMarks);
        Assert.True(ClipSave.HasKeyboardIoMarks);
        Assert.True(sheet.HasSheetRangeHandles);
        Assert.True(ClipSave.HasSheetRangeHandles);
        Assert.False(sheet.HasEmptySeekRangeDrag);
        Assert.False(ClipSave.HasEmptySeekRangeDrag);
        Assert.Equal(8, sheet.HandleSizePx);
        Assert.Equal(ClipSave.HandleSizePx, sheet.HandleSizePx);
        Assert.False(sheet.ShowStartHandle);
        Assert.False(sheet.ShowEndHandle);
        Assert.Equal("", sheet.StartHandleLetter);
        Assert.Equal("", sheet.EndHandleLetter);
        Assert.Equal("길이", sheet.DurationLabel);
        Assert.Equal("형식", sheet.FormatLabel);
        Assert.Equal("원본복사", ClipFormats.Label(ClipFormat.StreamCopy));
        Assert.Equal("webp", ClipFormats.Label(ClipFormat.Webp));
        Assert.Equal("gif", ClipFormats.Label(ClipFormat.Gif));
        Assert.Equal("fps", sheet.FpsLabel);
        Assert.Equal("원본", sheet.FpsText);
        Assert.Equal("핑퐁", sheet.PingPongLabel);
        Assert.Equal("팔레트", sheet.PaletteLabel);
        Assert.Equal("256색", sheet.PaletteValue);
        Assert.Equal("원본복사에서는 꺼짐", sheet.EncodingLockHint);
        Assert.Equal("키프레임 단위로 저장됩니다.", sheet.KeyframeNotice);
        Assert.Equal("폴더", sheet.FolderFieldLabel);
        Assert.Equal("변경", sheet.ChangeFolderLabel);
        Assert.Equal("저장", sheet.SaveLabel);
        Assert.Equal("취소", sheet.CancelLabel);
        Assert.Equal(@"Videos\구간", sheet.FolderLabel);
        Assert.Equal(@"Videos\구간", ClipSave.DefaultFolderLabel);
        Assert.Equal("#0E0E0E", sheet.PanelColor);
        Assert.Equal("#FFFFFF", sheet.SaveColor);
        Assert.Equal(SkinA.Panel, sheet.PanelColor);
        Assert.Equal(SkinA.Accent, sheet.SaveColor);
        Assert.Equal("#C6FF00", sheet.TickColor);
        Assert.Equal(SeriesOn.Accent, sheet.TickColor);
        Assert.Equal(ClipTickKind.Square, sheet.TickKind);
        Assert.Equal(2, sheet.TickSizePx);
        Assert.Equal(SkinA.IoTickSizePx, sheet.TickSizePx);
        Assert.False(sheet.RenderIoLetters);
        Assert.Equal("", sheet.InLetter);
        Assert.Equal("", sheet.OutLetter);
        Assert.False(sheet.HasPaletteControl);
        Assert.False(sheet.HasRecordButton);
        Assert.False(sheet.PingPong);
        Assert.False(ClipSave.DefaultPingPong);
        Assert.Null(sheet.Fps);
        Assert.Equal("원본", sheet.FpsText);
        Assert.True(SkinA.NoMockCaptionSentences);
        Assert.DoesNotContain("duration", sheet.DurationLabel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clip save", sheet.Title, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("captureFolder", AppSettings.ClipFolderKey, StringComparison.Ordinal);
        Assert.Equal("clipFolder", AppSettings.ClipFolderKey);
    }

    [Fact]
    public void Transport_has_no_record_button()
    {
        var shell = PlayerShell.Boot();
        Assert.False(shell.Transport.HasRecordButton);
        Assert.False(shell.Clip.HasRecordButton);
        Assert.False(ClipSave.HasRecordButton);
        Assert.Equal(9, shell.Transport.Order.Count);
        Assert.DoesNotContain("Record", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("Capture", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("Camera", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("MarkIn", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("MarkOut", Enum.GetNames<TransportControl>());
        Assert.True(shell.Clip.HasSheetCurrentMarks);
        Assert.False(shell.Clip.HasVideoDragSelect);
    }

    [Fact]
    public void Sheet_current_actions_set_in_and_out_to_playback_time()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("show.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 80 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.OpenClipSheet();
        Assert.True(session.Shell.Clip.Open);
        Assert.True(session.Shell.Clip.CanMarkCurrent);
        Assert.True(session.Shell.Clip.HasSheetCurrentMarks);
        Assert.Equal(0, session.Shell.Clip.InMark);
        Assert.Equal(80, session.Shell.Clip.OutMark);
        Assert.True(session.Shell.Clip.ShowStartHandle);
        Assert.True(session.Shell.Clip.ShowEndHandle);

        session.SeekAbsolute(12);
        session.SetInMark();
        Assert.Equal(12, session.Shell.Clip.InMark);
        Assert.Equal(12, session.Shell.Clip.StartSeconds);
        Assert.True(session.Shell.Clip.ShowInTick);

        session.SeekAbsolute(28);
        session.SetOutMark();
        Assert.Equal(28, session.Shell.Clip.OutMark);
        Assert.Equal(28, session.Shell.Clip.EndSeconds);
        Assert.True(session.Shell.Clip.ShowOutTick);
        Assert.Equal("00:00:16", session.Shell.Clip.DurationText);
        Assert.True(session.Shell.Clip.CanSave);
        Assert.Equal(ClipTickKind.Square, session.Shell.Clip.TickKind);
        Assert.False(session.Shell.Clip.HasVideoDragSelect);
    }

    [Fact]
    public void Sheet_open_defaults_playhead_to_media_end()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("show.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 100 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(15);
        Assert.False(session.Shell.Clip.ShowStartHandle);
        session.OpenClipSheet();

        var clip = session.Shell.Clip;
        Assert.Equal(15, clip.InMark);
        Assert.Equal(100, clip.OutMark);
        Assert.Equal(15, clip.StartSeconds);
        Assert.Equal(100, clip.EndSeconds);
        Assert.Equal(85, clip.ClipDurationSeconds);
        Assert.True(clip.ShowStartHandle);
        Assert.True(clip.ShowEndHandle);
        Assert.True(clip.ShowInTick);
        Assert.True(clip.ShowOutTick);
        Assert.Equal(ClipTickKind.Square, clip.TickKind);
        Assert.Equal(8, clip.HandleSizePx);
        Assert.Equal("", clip.StartHandleLetter);
        Assert.False(clip.HasEmptySeekRangeDrag);
        Assert.True(clip.CanSave);
        Assert.Equal("show_000015-000140.mkv", clip.PreviewFileName);

        session.CloseClipSheet();
        Assert.False(session.Shell.Clip.ShowStartHandle);
        Assert.True(session.Shell.Clip.ShowInTick);
        Assert.Equal(15, session.Shell.Clip.InMark);
    }

    [Fact]
    public void Sheet_open_fills_missing_end_and_keeps_existing_in()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("show.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 60 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(10);
        session.SetInMark();
        session.SeekAbsolute(25);
        session.OpenClipSheet();
        Assert.Equal(10, session.Shell.Clip.InMark);
        Assert.Equal(60, session.Shell.Clip.OutMark);
    }

    [Fact]
    public void Handles_and_sheet_actions_move_the_same_two_points()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("show.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 80 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(12);
        session.OpenClipSheet();
        Assert.Equal(12, session.Shell.Clip.StartSeconds);
        Assert.Equal(80, session.Shell.Clip.EndSeconds);

        session.MoveClipHandle(ClipHandle.Start, 20);
        session.MoveClipHandle(ClipHandle.End, 44);
        Assert.Equal(20, session.Shell.Clip.InMark);
        Assert.Equal(44, session.Shell.Clip.OutMark);
        Assert.Equal(20, session.Shell.Clip.StartSeconds);
        Assert.Equal(44, session.Shell.Clip.EndSeconds);

        session.SeekAbsolute(18);
        session.SetInMark();
        Assert.Equal(18, session.Shell.Clip.InMark);
        Assert.Equal(44, session.Shell.Clip.OutMark);

        session.SeekAbsolute(50);
        session.SetOutMark();
        Assert.Equal(18, session.Shell.Clip.StartSeconds);
        Assert.Equal(50, session.Shell.Clip.EndSeconds);

        session.CloseClipSheet();
        session.MoveClipHandle(ClipHandle.End, 70);
        Assert.Equal(50, session.Shell.Clip.OutMark);
        Assert.False(session.Shell.Clip.ShowStartHandle);
    }

    [Fact]
    public void Empty_seek_does_not_create_a_range()
    {
        Assert.False(ClipSave.HasEmptySeekRangeDrag);
        Assert.False(PlayerShell.Boot().Clip.HasEmptySeekRangeDrag);
        Assert.Equal(
            (12d, 40d),
            ClipSave.SheetOpenRange(null, null, 12, 40));
        Assert.Equal(
            (5d, 40d),
            ClipSave.SheetOpenRange(5, null, 12, 40));
        Assert.Equal(
            (12d, 30d),
            ClipSave.SheetOpenRange(null, 30, 12, 40));
        Assert.Equal(
            (5d, 30d),
            ClipSave.SheetOpenRange(5, 30, 12, 40));
        Assert.Equal(0, ClipSave.TimeFromSeekX(7, 100, 8, 40));
        Assert.InRange(ClipSave.HandleLeft(0, 100, 8), 6.9, 7.1);
    }

    [Fact]
    public void Save_exports_the_open_sheet_range_only()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("show.mkv", [1]);
        var dest = Path.Combine(workspace.Root, "구간");
        var engine = new FakeMediaEngine { Duration = 100 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(15);
        session.SetClipFolder(dest);
        session.OpenClipSheet();
        var runner = new FakeClipProcessRunner();

        var result = session.RunClipSave(runner);

        Assert.True(result.Saved);
        Assert.Equal("show_000015-000140.mkv", Path.GetFileName(result.Path));
        Assert.Contains("-ss", runner.LastArguments);
        Assert.Equal("15", runner.LastArguments[runner.LastArguments.IndexOf("-ss") + 1]);
        Assert.Contains("-t", runner.LastArguments);
        Assert.Equal("85", runner.LastArguments[runner.LastArguments.IndexOf("-t") + 1]);
    }

    [Fact]
    public void Keyboard_io_marks_still_work_without_opening_the_sheet()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("marks.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 50 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        Assert.False(session.Shell.Clip.Open);
        session.SeekAbsolute(8);
        session.SetInMark();
        session.SeekAbsolute(19);
        session.SetOutMark();
        Assert.Equal(8, session.Shell.Clip.InMark);
        Assert.Equal(19, session.Shell.Clip.OutMark);
        Assert.True(session.Shell.Clip.ShowInTick);
        Assert.True(session.Shell.Clip.ShowOutTick);
        Assert.True(session.Shell.Clip.HasKeyboardIoMarks);

        session.OpenClipSheet();
        Assert.Equal(8, session.Shell.Clip.InMark);
        Assert.Equal(19, session.Shell.Clip.OutMark);
        Assert.True(session.Shell.Clip.ShowStartHandle);
        Assert.True(session.Shell.Clip.ShowEndHandle);
        session.SeekAbsolute(22);
        session.SetInMark();
        Assert.Equal(22, session.Shell.Clip.InMark);
        Assert.Equal(19, session.Shell.Clip.OutMark);
        Assert.True(session.Shell.Clip.Open);
    }

    [Fact]
    public void Url_source_does_not_open_clip_save()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine { Duration = 40 };
        var session = new PlaybackSession(engine, workspace.Data);
        Assert.True(session.OpenUrl("https://example.com/video.mp4").Success);
        Assert.False(session.CanClipSave);
        session.OpenClipSheet();
        Assert.False(session.Shell.Clip.Open);
        Assert.False(session.Shell.Clip.ShowStartHandle);
        session.MoveClipHandle(ClipHandle.Start, 10);
        Assert.Null(session.Shell.Clip.InMark);
        Assert.False(session.Shell.FileOnly.ClipSave);
    }

    [Fact]
    public void Sheet_extra_controls_are_current_marks_not_video_drag()
    {
        var mainXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml"));
        var codeBehind = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml.cs"));
        Assert.Contains("Content=\"현재를 시작\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"현재를 끝\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ClipMarkStartButton\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ClipMarkEndButton\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("case Key.I:", codeBehind, StringComparison.Ordinal);
        Assert.Contains("case Key.O:", codeBehind, StringComparison.Ordinal);
        Assert.Contains("_session.SetInMark();", codeBehind, StringComparison.Ordinal);
        Assert.Contains("_session.SetOutMark();", codeBehind, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"InTick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"OutTick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"StartHandle\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"EndHandle\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("ClipHandle_DragDelta", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("I/O", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptySeekRange", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateRangeFromSeek", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("DragSelect", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("DragSelect", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("VideoDrag", codeBehind, StringComparison.Ordinal);
        Assert.Contains("MouseLeftButtonDown=\"Video_Click\"", mainXaml, StringComparison.Ordinal);
        var videoClick = SliceBetween(codeBehind, "private void Video_Click", "private void SeekSlider_Committed");
        Assert.Contains("_session.PlayPause();", videoClick, StringComparison.Ordinal);
        Assert.DoesNotContain("SetInMark", videoClick, StringComparison.Ordinal);
        Assert.DoesNotContain("SetOutMark", videoClick, StringComparison.Ordinal);
    }

    [Fact]
    public void Stream_copy_keeps_the_source_extension()
    {
        Assert.Equal("show_000010-000020.mp4",
            ClipSave.FileName("show", 10, 20, ClipFormat.StreamCopy, @"C:\Videos\show.mp4"));
        Assert.Equal("clip_000000-000002.mkv",
            ClipSave.FileName("clip", 0, 2, ClipFormat.StreamCopy, "clip.mkv"));
        Assert.Equal("clip_000000-000002.mov",
            ClipSave.FileName("clip", 0, 2, ClipFormat.StreamCopy, "clip.MOV"));
        Assert.Equal("clip_000000-000002.webp",
            ClipSave.FileName("clip", 0, 2, ClipFormat.Webp, "clip.mkv"));
        Assert.Equal("clip_000000-000002.gif",
            ClipSave.FileName("clip", 0, 2, ClipFormat.Gif, "clip.mp4"));
    }

    [Fact]
    public void Save_is_disabled_when_end_is_before_start_or_under_one_second()
    {
        Assert.False(ClipSave.IsValidRange(20, 10));
        Assert.False(ClipSave.CanSave(true, 20, 10));
        Assert.False(ClipSave.CanSave(true, 10, 10.5));
        Assert.True(ClipSave.CanSave(true, 10, 11));
        Assert.False(ClipSave.CanSave(false, 10, 20));

        using var workspace = new TempWorkspace();
        var video = workspace.File("range.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 40 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(20);
        session.SetInMark();
        session.SeekAbsolute(10);
        session.SetOutMark();
        session.OpenClipSheet();
        Assert.False(session.Shell.Clip.CanSave);
        Assert.True(session.Shell.Clip.EndSeconds < session.Shell.Clip.StartSeconds);

        var result = session.RunClipSave(new FakeClipProcessRunner());
        Assert.False(result.Saved);
        Assert.Equal(UiCopy.ClipTooShort, result.Banner);
        Assert.True(session.Shell.Clip.Open);
    }

    [Fact]
    public void Stream_copy_locks_fps_pingpong_and_shows_keyframe_notice()
    {
        var sheet = PlayerShell.Boot().Clip;
        Assert.Equal(ClipFormat.StreamCopy, sheet.Format);
        Assert.False(sheet.FpsEnabled);
        Assert.False(sheet.PingPongEnabled);
        Assert.False(sheet.PaletteNoticeVisible);
        Assert.True(sheet.KeyframeNoticeVisible);
        Assert.True(sheet.EncodingLockHintVisible);
        Assert.False(ClipSave.EncodingEnabled(ClipFormat.StreamCopy));
        Assert.Null(ClipSave.EffectiveFps(ClipFormat.StreamCopy, 15));
        Assert.False(ClipSave.EffectivePingPong(ClipFormat.StreamCopy, true));
    }

    [Theory]
    [InlineData(ClipFormat.Webp)]
    [InlineData(ClipFormat.Gif)]
    public void Animated_formats_enable_fps_and_pingpong(ClipFormat format)
    {
        var sheet = PlayerShell.Boot().Clip;
        sheet.Format = format;
        Assert.True(sheet.FpsEnabled);
        Assert.True(sheet.PingPongEnabled);
        Assert.False(sheet.KeyframeNoticeVisible);
        Assert.False(sheet.EncodingLockHintVisible);
        Assert.Equal(format == ClipFormat.Gif, sheet.PaletteNoticeVisible);
        Assert.True(ClipSave.EncodingEnabled(format));
        Assert.Equal(15, ClipSave.EffectiveFps(format, 15));
        Assert.True(ClipSave.EffectivePingPong(format, true));
    }

    [Fact]
    public void Gif_palette_is_notice_only()
    {
        var sheet = PlayerShell.Boot().Clip;
        sheet.Format = ClipFormat.Gif;
        Assert.True(sheet.PaletteNoticeVisible);
        Assert.False(sheet.HasPaletteControl);
        Assert.Equal(256, ClipSave.GifPaletteColors);
        Assert.Equal("256색", UiCopy.ClipPaletteValue);
        Assert.Equal("팔레트", UiCopy.ClipPalette);
    }

    [Theory]
    [InlineData(null, 1, 15)]
    [InlineData(null, -1, null)]
    [InlineData(15, -1, 14)]
    [InlineData(1, -1, null)]
    [InlineData(60, 1, 60)]
    [InlineData(0, 0, 1)]
    public void Fps_is_source_or_one_through_sixty(int? current, int delta, int? expected)
    {
        if (delta == 0)
        {
            Assert.Equal(expected, ClipSave.ClampFps(current));
            return;
        }

        Assert.Equal(expected, ClipSave.NudgeFps(current, delta));
    }

    [Fact]
    public void Filename_is_stem_hhmmss_range_and_extension()
    {
        Assert.Equal(
            "드라마_001204-001420.gif",
            ClipSave.FileName("드라마", 12 * 60 + 4, 14 * 60 + 20, ClipFormat.Gif, "show.mkv"));
        Assert.Equal(
            "드라마_S02E03_001204-001420.mkv",
            ClipSave.FileName("드라마_S02E03", 12 * 60 + 4, 14 * 60 + 20, ClipFormat.StreamCopy, "드라마_S02E03.mkv"));
        Assert.Equal(
            "lighthouse_000000-000001.webp",
            ClipSave.FileName("lighthouse", 0, 1, ClipFormat.Webp, "lighthouse.mp4"));
        Assert.Equal("safe_name_000010-000020.mp4",
            ClipSave.FileName("safe/name", 10, 20, ClipFormat.StreamCopy, "movie.mp4"));
        Assert.Equal("gif", ClipFormats.Extension(ClipFormat.Gif, "a.mkv"));
        Assert.Equal("webp", ClipFormats.Extension(ClipFormat.Webp, "a.mkv"));
        Assert.Equal("mkv", ClipFormats.Extension(ClipFormat.StreamCopy, "a.mkv"));
    }

    [Theory]
    [InlineData(0.99, false)]
    [InlineData(1.0, true)]
    [InlineData(2.16, true)]
    public void Minimum_duration_is_one_second(double seconds, bool expected)
        => Assert.Equal(expected, ClipSave.IsLongEnough(seconds));

    [Fact]
    public void Stream_copy_args_ignore_fps_and_pingpong()
    {
        var job = new ClipJob("show.mkv", "show", "/tmp/out", 724, 860, ClipFormat.StreamCopy, 15, true);
        var args = ClipSave.BuildArguments(job, "/tmp/out/show_001204-001420.mkv");
        Assert.Contains("-c", args);
        Assert.Contains("copy", args);
        Assert.DoesNotContain("-vf", args);
        Assert.DoesNotContain(args, value => value.Contains("fps=", StringComparison.Ordinal));
        Assert.DoesNotContain(args, value => value.Contains("reverse", StringComparison.Ordinal));
        Assert.DoesNotContain(args, value => value.Contains("palette", StringComparison.Ordinal));
        Assert.Null(ClipSave.VideoFilter(job));
    }

    [Fact]
    public void Webp_reencodes_with_fps_and_pingpong()
    {
        var job = new ClipJob("show.mkv", "show", "/tmp/out", 10, 20, ClipFormat.Webp, 15, true);
        var args = ClipSave.BuildArguments(job, "/tmp/out/show_000010-000020.webp");
        Assert.Contains("-an", args);
        Assert.Contains("libwebp", args);
        Assert.Contains("-vf", args);
        var filter = args[args.ToList().IndexOf("-vf") + 1];
        Assert.Contains("fps=15", filter, StringComparison.Ordinal);
        Assert.Contains("reverse", filter, StringComparison.Ordinal);
        Assert.DoesNotContain("palettegen", filter, StringComparison.Ordinal);
        Assert.DoesNotContain("copy", args);
    }

    [Fact]
    public void Gif_reencodes_with_fps_and_pingpong_like_webp()
    {
        var job = new ClipJob("show.mkv", "show", "/tmp/out", 10, 20, ClipFormat.Gif, 15, true);
        var args = ClipSave.BuildArguments(job, "/tmp/out/show_000010-000020.gif");
        Assert.Contains("-an", args);
        Assert.Contains("-vf", args);
        Assert.DoesNotContain("copy", args);
        Assert.DoesNotContain("libwebp", args);
        var filter = args[args.ToList().IndexOf("-vf") + 1];
        Assert.Contains("fps=15", filter, StringComparison.Ordinal);
        Assert.Contains("reverse", filter, StringComparison.Ordinal);
        Assert.Contains("palettegen=max_colors=256", filter, StringComparison.Ordinal);
        Assert.True(ClipSave.EncodingEnabled(ClipFormat.Gif));
        Assert.True(ClipSave.EffectivePingPong(ClipFormat.Gif, true));
        Assert.Equal(15, ClipSave.EffectiveFps(ClipFormat.Gif, 15));
    }

    [Fact]
    public void Gif_uses_256_color_palette_without_a_control()
    {
        var job = new ClipJob("show.mkv", "show", "/tmp/out", 10, 20, ClipFormat.Gif, 15, false);
        var filter = ClipSave.VideoFilter(job);
        Assert.NotNull(filter);
        Assert.Contains("fps=15", filter, StringComparison.Ordinal);
        Assert.Contains("palettegen=max_colors=256", filter, StringComparison.Ordinal);
        Assert.DoesNotContain("reverse", filter, StringComparison.Ordinal);
        Assert.False(ClipSave.HasPaletteControl);
    }

    [Fact]
    public void Io_marks_and_tick_ratios_have_no_letters()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("드라마.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 42 * 60 + 10 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(12 * 60 + 4);
        session.SetInMark();
        session.SeekAbsolute(14 * 60 + 20);
        session.SetOutMark();
        session.OpenClipSheet();

        var clip = session.Shell.Clip;
        Assert.Equal(12 * 60 + 4, clip.InMark);
        Assert.Equal(14 * 60 + 20, clip.OutMark);
        Assert.Equal("00:12:04", clip.StartText);
        Assert.Equal("00:14:20", clip.EndText);
        Assert.Equal("00:02:16", clip.DurationText);
        Assert.True(clip.ShowInTick);
        Assert.True(clip.ShowOutTick);
        Assert.True(clip.ShowStartHandle);
        Assert.True(clip.ShowEndHandle);
        Assert.Equal("", clip.StartHandleLetter);
        Assert.Equal("", clip.EndHandleLetter);
        Assert.False(clip.HasEmptySeekRangeDrag);
        Assert.False(clip.RenderIoLetters);
        Assert.Equal("", clip.InLetter);
        Assert.Equal("", clip.OutLetter);
        Assert.Equal(ClipTickKind.Square, clip.TickKind);
        Assert.Equal(2, clip.TickSizePx);
        Assert.Equal(0, ClipSave.TickRatio(null, engine.Duration));
        Assert.InRange(ClipSave.TickRatio(clip.InMark, engine.Duration), 0.28, 0.30);
        Assert.True(clip.CanSave);
        Assert.Equal("드라마_001204-001420.mkv", clip.PreviewFileName);
    }

    [Fact]
    public void Folder_defaults_to_videos_clip_not_pictures_or_capture()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Equal(@"Videos\구간", session.Shell.Clip.FolderLabel);
        Assert.Equal(ClipSave.DefaultFolderPath(), session.Shell.Clip.FolderPath);
        Assert.DoesNotContain("Pictures", session.Shell.Clip.FolderLabel, StringComparison.Ordinal);
        Assert.DoesNotContain("captureFolder", session.Settings.ToJson(), StringComparison.Ordinal);

        var dest = Path.Combine(workspace.Root, "Exports");
        Directory.CreateDirectory(dest);
        Assert.True(session.SetClipFolder(dest));
        var json = File.ReadAllText(Path.Combine(workspace.Data, AppSettings.FileName));
        Assert.Contains("clipFolder", json, StringComparison.Ordinal);
        Assert.DoesNotContain("captureFolder", json, StringComparison.Ordinal);
        Assert.Equal("Exports", session.Shell.Clip.FolderLabel);

        var reopened = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Equal(Path.GetFullPath(dest), Path.GetFullPath(reopened.Shell.Clip.FolderPath));
    }

    [Fact]
    public void Remote_clip_folder_is_rejected()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.False(session.SetClipFolder("https://example.com/clips"));
        Assert.Equal(@"Videos\구간", session.Shell.Clip.FolderLabel);
    }

    [Fact]
    public void Save_writes_gif_preview_name_through_runner()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("드라마.mkv", [1]);
        var dest = Path.Combine(workspace.Root, "구간");
        var engine = new FakeMediaEngine { Duration = 100 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(12);
        session.SetInMark();
        session.SeekAbsolute(20);
        session.SetOutMark();
        session.SetClipFolder(dest);
        session.SetClipFormat(ClipFormat.Gif);
        session.OpenClipSheet();
        var runner = new FakeClipProcessRunner();

        var result = session.RunClipSave(runner);

        Assert.True(result.Saved);
        Assert.False(session.Shell.Clip.Open);
        Assert.Equal("드라마_000012-000020.gif", Path.GetFileName(result.Path));
        Assert.Contains("-vf", runner.LastArguments);
        Assert.Contains(runner.LastArguments, value => value.Contains("palettegen=max_colors=256", StringComparison.Ordinal));
        Assert.True(File.Exists(result.Path!));
    }

    [Fact]
    public void Short_range_and_missing_media_stay_on_the_sheet()
    {
        using var workspace = new TempWorkspace();
        var empty = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        empty.OpenClipSheet();
        var missing = empty.RunClipSave(new FakeClipProcessRunner());
        Assert.False(missing.Saved);
        Assert.Equal(UiCopy.ClipNoMedia, missing.Banner);
        Assert.True(empty.Shell.Clip.Open);

        var video = workspace.File("short.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 30 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(10);
        session.SetInMark();
        session.SeekAbsolute(10.4);
        session.SetOutMark();
        session.OpenClipSheet();
        Assert.False(session.Shell.Clip.CanSave);
        var tooShort = session.RunClipSave(new FakeClipProcessRunner());
        Assert.False(tooShort.Saved);
        Assert.Equal(UiCopy.ClipTooShort, tooShort.Banner);
        Assert.True(session.Shell.Clip.Open);
    }

    [Fact]
    public void Missing_ffmpeg_keeps_the_sheet_open()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("show.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 40 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SetInMark();
        session.SeekAbsolute(12);
        session.SetOutMark();
        session.SetClipFolder(Path.Combine(workspace.Root, "out"));
        session.OpenClipSheet();
        var result = session.RunClipSave(new FakeClipProcessRunner { Executable = null });
        Assert.False(result.Saved);
        Assert.Equal(UiCopy.ClipFfmpegMissing, result.Banner);
        Assert.True(session.Shell.Clip.Open);
    }

    private static string ReadRepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(relative);
    }

    private static string SliceBetween(string text, string start, string end)
    {
        var from = text.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, start);
        var until = text.IndexOf(end, from, StringComparison.Ordinal);
        Assert.True(until > from, end);
        return text[from..until];
    }
}

internal sealed class FakeClipProcessRunner : IClipProcessRunner
{
    public string? Executable { get; set; } = "ffmpeg";
    public bool Succeed { get; set; } = true;
    public List<string> LastArguments { get; } = [];

    public ClipProcessResult Run(string executable, IReadOnlyList<string> arguments)
    {
        _ = executable;
        LastArguments.Clear();
        LastArguments.AddRange(arguments);
        var output = arguments.Count > 0 ? arguments[^1] : null;
        if (Succeed && output is not null)
        {
            var dir = Path.GetDirectoryName(output);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(output, [1]);
        }

        return Succeed
            ? new ClipProcessResult(true, 0, "")
            : new ClipProcessResult(false, 1, "fail");
    }
}
