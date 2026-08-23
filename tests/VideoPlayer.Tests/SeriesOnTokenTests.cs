using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class SeriesOnTokenTests
{
    [Fact]
    public void Accent_and_density_match_serieson_lock()
    {
        Assert.Equal("#C6FF00", SeriesOn.Accent);
        Assert.Equal("#050505", SeriesOn.Background);
        Assert.Equal("#0E0E0E", SeriesOn.Elevated);
        Assert.Equal("#FFFFFF", SeriesOn.Text);
        Assert.Equal("#8A8A8A", SeriesOn.Secondary);
        Assert.True(SeriesOn.TitleIsAccent);
        Assert.True(SeriesOn.VolumeFillIsAccent);
        Assert.True(SeriesOn.TimecodeIsAccent);
        Assert.True(SeriesOn.VolumeThumbIsRound);
        Assert.True(SeriesOn.PlayTriangleIsWhite);
        Assert.True(SeriesOn.StopButtonExists);
        Assert.True(SeriesOn.HasClear);
        Assert.True(SeriesOn.ClearIsTextLabel);
        Assert.False(SeriesOn.ClearUsesEjectIcon);
        Assert.True(SeriesOn.ClearImmediatelyRightOfStop);
        Assert.True(SeriesOn.ClearNeverMarksComplete);
        Assert.True(SeriesOn.ClearSavesCurrentPosition);
        Assert.True(SeriesOn.ClearAppliesToUrl);
        Assert.Equal("지우기", UiCopy.Clear);
        Assert.False(SeriesOn.SkipPlusMinusOnTransport);
        Assert.True(SeriesOn.HorizontalVolumeSlider);
        Assert.False(SeriesOn.VerticalVolumePopover);
        Assert.False(SeriesOn.HasFileViewMenuBar);
        Assert.True(SeriesOn.QuickMenuIsView);
        Assert.True(SeriesOn.FileCommandsInHamburger);
        Assert.True(SeriesOn.FileCommandsInQuickMenu);
        Assert.True(SeriesOn.HamburgerIsView);
        Assert.False(SeriesOn.CaptionsOnBar);
        Assert.False(SeriesOn.FullscreenOnBar);
        Assert.True(SeriesOn.EnterTogglesFullscreen);
        Assert.True(SeriesOn.F11TogglesFullscreen);
        Assert.True(SeriesOn.FullscreenTransportIsOverlay);
        Assert.True(SeriesOn.WindowedTransportIsDocked);
        Assert.Equal(0.80, SeriesOn.FullscreenTransportOpacity);
        Assert.Equal(3, SeriesOn.FullscreenIdleHideSeconds);
        Assert.True(SeriesOn.IoMarksAreSquares);
        Assert.Equal(4, SeriesOn.ButtonPadding);
        Assert.Equal(2, SeriesOn.IoMarkSizePx);
        Assert.True(SeriesOn.HasWindowControls);
        Assert.False(SeriesOn.HasCastIcon);
        Assert.False(SeriesOn.HasHdrIcon);
        Assert.False(SeriesOn.HasEjectIcon);
        Assert.False(SeriesOn.HasBrandWordmark);
        Assert.False(SeriesOn.HasMenuPipe);
        Assert.True(SeriesOn.ChromeIsSolid);
        Assert.False(SeriesOn.ChromeHasBlur);
        Assert.False(SeriesOn.ChromeHasWhiteOverlay);
        Assert.Equal("#050505", SeriesOn.ChromeFill);
        Assert.Equal("#222222", SeriesOn.Divider);
        Assert.Equal(0.40, SeriesOn.DividerOpacity);
        Assert.Equal(1, SeriesOn.TransportSeparatorPx);
        Assert.Equal(40, SeriesOn.TransportHeightPx);
        Assert.Equal(1, SeriesOn.TransportSeparatorPx);
        Assert.Equal(88, SeriesOn.VolumeSliderWidthPx);
        Assert.Equal(16, SeriesOn.HeaderTitleSize);
        Assert.Equal(12, SeriesOn.BodySize);
        Assert.Equal(new[] { 4, 8, 12, 16 }, SeriesOn.SpacingScale);
        Assert.Equal(SkinA.TransportHeightPx, SeriesOn.TransportHeightPx);
        Assert.Equal(SkinA.SidebarRailWidthPx, SeriesOn.SidebarRailWidthPx);
        Assert.NotEqual(SkinA.Accent, SeriesOn.Accent);
        Assert.Equal("#FFFFFF", SkinC.Accent);
        Assert.Equal(SkinA.Accent, SkinC.Accent);
    }

    [Fact]
    public void Header_is_title_quickmenu_and_window_controls()
    {
        var shell = PlayerShell.Boot();
        Assert.True(shell.HasHeaderUi);
        Assert.Equal("이어서", shell.Header.Title);
        Assert.Equal("이어서", shell.Title);
        Assert.Equal("이어서", UiCopy.AppTitle);
        Assert.Equal("퀵메뉴", shell.Header.QuickMenuLabel);
        Assert.True(shell.Header.QuickMenuIsView);
        Assert.True(shell.Header.HasWindowControls);
        Assert.False(shell.Header.HasFileViewMenuBar);
        Assert.True(shell.Header.FileCommandsInHamburger);
        Assert.True(shell.Header.FileCommandsInQuickMenu);
        Assert.True(shell.Header.HamburgerIsView);
        Assert.False(shell.Header.HasMenuPipe);
        Assert.True(shell.Header.ChromeIsSolid);
        Assert.Equal(new[] { "퀵메뉴" }, shell.Menus);
        Assert.Equal(UiCopy.ViewMenuItems, new[]
        {
            "-10초",
            "+10초",
            "이전 화",
            "다음 화",
            "시리즈",
            "사이드바",
            "자막",
            "여기까지 스킵",
            "건너뛰기 자동",
            "CC",
            "전체화면",
            "다음 화 자동 재생",
            "1.0x",
            "캡처",
            "구간 저장"
        });
        Assert.Equal(new[] { "열기...", "URL 열기", "폴더 열기", "다른 이름으로 저장", "종료" }, UiCopy.FileMenuItems);
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
        Assert.Contains("Stop", Enum.GetNames<TransportControl>());
        Assert.Contains("Clear", Enum.GetNames<TransportControl>());
        Assert.Contains("Hamburger", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("Eject", Enum.GetNames<TransportControl>());
        Assert.True(PlayerShell.Boot().Transport.HasStop);
        Assert.True(PlayerShell.Boot().Transport.HasClear);
        Assert.True(PlayerShell.Boot().Transport.ClearIsTextLabel);
        Assert.True(PlayerShell.Boot().Transport.ClearImmediatelyRightOfStop);
        Assert.True(PlayerShell.Boot().Transport.ClearNeverMarksComplete);
        Assert.True(PlayerShell.Boot().Transport.ClearAppliesToUrl);
        Assert.Equal("지우기", PlayerShell.Boot().Transport.ClearLabel);
        Assert.Equal(TransportControl.Clear, order[order.ToList().IndexOf(TransportControl.Stop) + 1]);
        Assert.True(PlayerShell.Boot().StageEmpty);
        Assert.True(PlayerShell.Boot().Transport.TimeOnBar);
        Assert.False(PlayerShell.Boot().Transport.CaptionsOnBar);
        Assert.False(PlayerShell.Boot().Transport.FullscreenOnBar);
        Assert.False(PlayerShell.Boot().Transport.SkipLabelsOnBar);
        Assert.False(PlayerShell.Boot().Transport.SpeedOnBar);
        Assert.False(PlayerShell.Boot().Transport.PreviousOnBar);
        Assert.False(PlayerShell.Boot().Transport.NextOnBar);
        Assert.False(PlayerShell.Boot().Transport.HasCastIcon);
        Assert.False(PlayerShell.Boot().Transport.HasHdrIcon);
        Assert.False(PlayerShell.Boot().Transport.HasEjectIcon);
        Assert.Equal("-10초", PlayerShell.Boot().Transport.SkipBackLabel);
        Assert.Equal("+10초", PlayerShell.Boot().Transport.SkipForwardLabel);
    }

    [Fact]
    public void Main_window_markup_matches_serieson_chrome()
    {
        var mainXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml"));
        var appXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "App.xaml"));

        Assert.Contains("Title=\"이어서\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"이어서\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("이어서", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("영상 플레이어", mainXaml, StringComparison.Ordinal);
        Assert.Contains("퀵메뉴", mainXaml, StringComparison.Ordinal);
        Assert.Contains("QuickMenuDivider", mainXaml, StringComparison.Ordinal);
        Assert.Contains("SeriesOnChromeBrush", mainXaml, StringComparison.Ordinal);
        Assert.Contains("SeriesOnDividerBrush", mainXaml, StringComparison.Ordinal);
        Assert.Contains("SeriesOnAccentBrush", mainXaml, StringComparison.Ordinal);
        Assert.Contains("C6FF00", appXaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Stop_Click", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Clear_Click", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"지우기\"", mainXaml, StringComparison.Ordinal);
        var stopClick = mainXaml.IndexOf("Click=\"Stop_Click\"", StringComparison.Ordinal);
        var clearButton = mainXaml.IndexOf("x:Name=\"ClearButton\"", StringComparison.Ordinal);
        Assert.True(stopClick >= 0 && clearButton > stopClick);
        var betweenStopAndClear = mainXaml[stopClick..clearButton];
        Assert.Equal(1, CountOccurrences(betweenStopAndClear, "Click=\""));
        var nextButton = mainXaml.IndexOf("<Button", clearButton + 1, StringComparison.Ordinal);
        var clearBlock = nextButton > clearButton ? mainXaml[clearButton..nextButton] : mainXaml[clearButton..];
        Assert.Contains("Content=\"지우기\"", clearBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("<Path", clearBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("Eject", clearBlock, StringComparison.Ordinal);
        Assert.Contains("EmptyStageCover", mainXaml, StringComparison.Ordinal);
        Assert.Contains("SeriesOnClearButton", mainXaml, StringComparison.Ordinal);
        Assert.Contains("SeriesOnVolumeSlider", mainXaml, StringComparison.Ordinal);
        Assert.Contains("SeriesOnIconButton", mainXaml, StringComparison.Ordinal);
        Assert.Contains("SeriesOnSeekSlider", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"TransportDockSlot\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"TransportBar\" VerticalAlignment=\"Bottom\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"TransportBar\" DockPanel.Dock=\"Bottom\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("SeriesOnFullscreenTransportBrush", appXaml, StringComparison.Ordinal);
        Assert.Contains("CC050505", appXaml, StringComparison.OrdinalIgnoreCase);
        var codeBehind = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml.cs"));
        Assert.Contains("case Key.Enter", codeBehind, StringComparison.Ordinal);
        Assert.Contains("case Key.F11", codeBehind, StringComparison.Ordinal);
        Assert.Contains("Padding\" Value=\"4\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"2\" Height=\"2\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"InTick\" Width=\"6\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Ellipse x:Name=\"InTick\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"CaptionsButton\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"FullscreenButton\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Ellipse", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("E10600", mainXaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SkinAPlayTriangleBrush", mainXaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"보기\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"열기...\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"MainMenu\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"파일\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"보기\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"|\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Fill=\"#33FFFFFF\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"-10초\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"+10초\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"-10초\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"+10초\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SeriesOnChromeBrush\" Color=\"#FF050505\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SeriesOnDividerBrush\" Color=\"#66222222\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"CaptionBar\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"TransportBar\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("VolumePopover", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("VerticalChromeSlider", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Orientation=\"Vertical\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SpaceX", mainXaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("xAI", mainXaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Grok", mainXaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cast", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("HDR", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Eject", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("불러오는 중", mainXaml, StringComparison.Ordinal);
        Assert.Equal("불러오는 중", UiCopy.Loading);
    }

    [Fact]
    public void Stop_seeks_to_zero_and_pauses_without_closing()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ep.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 80 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(40);
        Assert.False(engine.IsPaused);

        session.Stop();
        Assert.True(engine.IsOpen);
        Assert.True(engine.IsPaused);
        Assert.Equal(0, engine.Position);
        Assert.True(session.Shell.IsPaused);
    }

    [Fact]
    public void Clear_unloads_persists_resume_and_empties_the_stage()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ep.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 80 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(40);
        Assert.False(session.Shell.StageEmpty);
        Assert.NotNull(session.Current);

        session.Clear();
        Assert.False(engine.IsOpen);
        Assert.True(engine.IsPaused);
        Assert.Equal(0, engine.Position);
        Assert.Null(session.Current);
        Assert.True(session.Shell.StageEmpty);
        Assert.Equal("00:00:00 / 00:00:00", session.Shell.OverlayTime);
        Assert.Equal("", session.Shell.OverlaySubtitle);
        Assert.False(session.Shell.NextEpisode.ShowCta);
        Assert.False(session.Shell.Skip.Visible);
        var saved = session.Resume.Find(video, new FileInfo(video).Length);
        Assert.NotNull(saved);
        Assert.Equal(40, saved!.PositionSeconds);
        Assert.False(saved.Completed);
        Assert.NotNull(session.Resume.Continue);
        Assert.Equal(video, session.Resume.Continue!.Path);

        var reopened = new PlaybackSession(new FakeMediaEngine { Duration = 80 }, workspace.Data);
        reopened.Open(video);
        Assert.Equal(40, reopened.Engine.Position);
        Assert.False(reopened.Shell.StageEmpty);
    }

    [Fact]
    public void Clear_in_last_ten_seconds_keeps_position_and_does_not_mark_complete()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ep.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 100 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(91);

        session.Clear();
        var saved = session.Resume.Find(video, new FileInfo(video).Length);
        Assert.NotNull(saved);
        Assert.Equal(91, saved!.PositionSeconds);
        Assert.False(saved.Completed);
        Assert.NotNull(session.Resume.Continue);
        Assert.Equal(video, session.Resume.Continue!.Path);

        var reopened = new PlaybackSession(new FakeMediaEngine { Duration = 100 }, workspace.Data);
        reopened.Open(video);
        Assert.Equal(91, reopened.Engine.Position);
        Assert.False(reopened.Resume.Find(video, new FileInfo(video).Length)!.Completed);
    }

    [Fact]
    public void Clear_on_url_saves_current_position_and_never_marks_complete()
    {
        using var workspace = new TempWorkspace();
        const string url = "https://example.com/show/S01E01.mkv";
        var engine = new FakeMediaEngine { Duration = 100 };
        var session = new PlaybackSession(engine, workspace.Data);
        Assert.True(session.OpenUrl(url).Success);
        session.SeekAbsolute(95);
        Assert.Equal(MediaSourceKind.HttpUrl, session.SourceKind);

        session.Clear();
        Assert.False(engine.IsOpen);
        Assert.Null(session.Current);
        Assert.True(session.Shell.StageEmpty);
        var saved = session.Resume.FindUrl(url);
        Assert.NotNull(saved);
        Assert.Equal(url, saved!.Key);
        Assert.Equal(95, saved.PositionSeconds);
        Assert.False(saved.Completed);
        Assert.NotNull(session.Resume.Continue);
        Assert.Equal(url, session.Resume.Continue!.Path);

        var reopened = new PlaybackSession(new FakeMediaEngine { Duration = 100 }, workspace.Data);
        reopened.OpenUrl(url);
        Assert.Equal(95, reopened.Engine.Position);
        Assert.False(reopened.Resume.FindUrl(url)!.Completed);
    }

    [Fact]
    public void Series_c_and_next_episode_cta_stay()
    {
        var shell = PlayerShell.Boot();
        Assert.True(shell.NextEpisode.OverlayOnly);
        Assert.True(shell.NextEpisode.EndRegionOnly);
        Assert.False(shell.NextEpisode.OnTransport);
        Assert.Equal("다음 화 >", shell.NextEpisode.Label);
        Assert.Equal("#FFFFFF", SkinC.Accent);
        Assert.True(shell.Series.Enabled);
        Assert.False(shell.Series.PlaylistButton);
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

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
