using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;
using VideoPlayer.Core.Subtitles;

namespace VideoPlayer.Tests;

public class DualSubtitleSheetTests
{
    [Fact]
    public void Sheet_copy_and_tokens_match_confirmed_dual_subs()
    {
        var sheet = PlayerShell.Boot().Subtitles;
        Assert.False(sheet.Open);
        Assert.Equal("자막", sheet.Title);
        Assert.Equal(UiCopy.Subtitles, sheet.Title);
        Assert.DoesNotContain("subtitle", sheet.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("보조 자막 (상단)", sheet.SecondaryHeading);
        Assert.Equal("주 자막 (하단)", sheet.PrimaryHeading);
        Assert.Equal("꺼짐", sheet.OffLabel);
        Assert.Equal("주 · 상단에 보조", sheet.Footer);
        Assert.True(sheet.PrimaryIsBottom);
        Assert.True(sheet.SecondaryIsTop);
        Assert.False(sheet.CcOpensSheet);
        Assert.True(sheet.CcNeverOpensSheet);
        Assert.True(sheet.OpensFromViewMenuOnly);
        Assert.False(sheet.CcHasLongPress);
        Assert.False(sheet.HasDelaySheet);
        Assert.True(sheet.SecondaryNeverAutoOn);
        Assert.Equal("#0E0E0E", sheet.PanelColor);
        Assert.Equal("#050505", sheet.BackgroundColor);
        Assert.Equal("#FFFFFF", sheet.AccentColor);
        Assert.Equal(SkinA.Panel, sheet.PanelColor);
        Assert.Equal(4, sheet.PanelRadius);
        Assert.True(SkinA.NoMockCaptionSentences);
        Assert.Equal("CC", UiCopy.Captions);
    }

    [Fact]
    public void Existing_sidecar_rules_still_load_primary()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("드라마.mkv", [1]);
        File.WriteAllText(Path.Combine(workspace.Root, "드라마.ko.srt"), """
            1
            00:00:00,000 --> 00:00:02,000
            한글
            """);
        File.WriteAllText(Path.Combine(workspace.Root, "드라마.en.srt"), """
            1
            00:00:00,000 --> 00:00:02,000
            Hello
            """);
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.Open(video);
        session.Tick(DateTimeOffset.UtcNow);

        Assert.Equal("한글", session.Shell.OverlaySubtitle);
        Assert.Equal("", session.Shell.OverlaySecondarySubtitle);
        Assert.Contains(session.Shell.Subtitles.AvailablePaths, path => path.EndsWith("드라마.ko.srt", StringComparison.Ordinal));
        Assert.Contains(session.Shell.Subtitles.AvailablePaths, path => path.EndsWith("드라마.en.srt", StringComparison.Ordinal));
        Assert.True(SubtitleLocator.IsEnglishSidecar(session.Shell.Subtitles.SuggestedSecondaryPath));
        Assert.Null(session.Shell.Subtitles.SecondaryPath);
        Assert.Equal("드라마.en.srt", session.Shell.Subtitles.SecondaryRows.First(row => row.Suggested).Label);
    }

    [Fact]
    public void Primary_prefers_ko_srt_then_existing_autoload()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("드라마.mkv", [1]);
        File.WriteAllText(Path.Combine(workspace.Root, "드라마.srt"), """
            1
            00:00:00,000 --> 00:00:02,000
            기본
            """);
        File.WriteAllText(Path.Combine(workspace.Root, "드라마.ko.srt"), """
            1
            00:00:00,000 --> 00:00:02,000
            한글
            """);
        File.WriteAllText(Path.Combine(workspace.Root, "드라마.en.srt"), """
            1
            00:00:00,000 --> 00:00:02,000
            Hello
            """);
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.Open(video);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.Equal("한글", session.Shell.OverlaySubtitle);
        Assert.EndsWith("드라마.ko.srt", session.Shell.Subtitles.PrimaryPath, StringComparison.Ordinal);
        Assert.Null(session.Shell.Subtitles.SecondaryPath);
        Assert.True(session.Shell.Subtitles.SecondaryNeverAutoOn);
    }

    [Fact]
    public void Stem_srt_and_smi_remain_primary_sidecars()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("S01E01.mkv", [1]);
        File.WriteAllText(Path.Combine(workspace.Root, "S01E01.srt"), "1\n00:00:00,000 --> 00:00:01,000\n안녕\n");
        File.WriteAllText(Path.Combine(workspace.Root, "S01E01.smi"), "<SAMI><BODY><SYNC Start=0>안녕</BODY>");
        var found = SubtitleLocator.FindSidecars(video);
        Assert.Equal(2, found.Count);
        Assert.DoesNotContain(found, path => path.Contains(".en.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(SubtitleLocator.SidecarFileNames("S01E01"), name => name == "S01E01.srt");
        Assert.Contains(SubtitleLocator.SidecarFileNames("S01E01"), name => name == "S01E01.smi");
        Assert.Contains(SubtitleLocator.SidecarFileNames("S01E01"), name => name == "S01E01.ko.srt");
    }

    [Fact]
    public void View_menu_opens_sheet_cc_toggles_primary_only()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("드라마.mkv", [1]);
        File.WriteAllText(Path.Combine(workspace.Root, "드라마.ko.srt"), """
            1
            00:00:00,000 --> 00:00:02,000
            주자막
            """);
        File.WriteAllText(Path.Combine(workspace.Root, "드라마.en.srt"), """
            1
            00:00:00,000 --> 00:00:02,000
            top line
            """);
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.Open(video);
        session.OpenSubtitleSheet();
        Assert.True(session.Shell.Subtitles.Open);
        Assert.Equal("자막", session.Shell.Subtitles.Title);

        session.SelectSecondarySubtitle(session.Shell.Subtitles.SuggestedSecondaryPath);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.Equal("주자막", session.Shell.OverlaySubtitle);
        Assert.Equal("top line", session.Shell.OverlaySecondarySubtitle);

        var sheetWasOpen = session.Shell.Subtitles.Open;
        session.ToggleCaptions();
        session.Tick(DateTimeOffset.UtcNow);
        Assert.False(session.Shell.Transport.CaptionsOn);
        Assert.Equal("", session.Shell.OverlaySubtitle);
        Assert.Equal("top line", session.Shell.OverlaySecondarySubtitle);
        Assert.Equal(sheetWasOpen, session.Shell.Subtitles.Open);
        Assert.False(session.Shell.Subtitles.CcOpensSheet);

        session.ToggleCaptions();
        session.Tick(DateTimeOffset.UtcNow);
        Assert.Equal("주자막", session.Shell.OverlaySubtitle);
        Assert.Equal("top line", session.Shell.OverlaySecondarySubtitle);
    }

    [Fact]
    public void Off_rows_clear_the_matching_track()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("clip.mkv", [1]);
        File.WriteAllText(Path.Combine(workspace.Root, "clip.srt"), """
            1
            00:00:00,000 --> 00:00:02,000
            아래
            """);
        File.WriteAllText(Path.Combine(workspace.Root, "clip.en.srt"), """
            1
            00:00:00,000 --> 00:00:02,000
            above
            """);
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.Open(video);
        session.SelectSecondarySubtitle(session.Shell.Subtitles.SuggestedSecondaryPath);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.Equal("아래", session.Shell.OverlaySubtitle);
        Assert.Equal("above", session.Shell.OverlaySecondarySubtitle);

        session.SelectPrimarySubtitle(null);
        session.SelectSecondarySubtitle(null);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.Equal("", session.Shell.OverlaySubtitle);
        Assert.Equal("", session.Shell.OverlaySecondarySubtitle);
        Assert.True(session.Shell.Subtitles.PrimaryRows[0].Selected);
        Assert.True(session.Shell.Subtitles.SecondaryRows[0].Selected);
        Assert.Equal("꺼짐", session.Shell.Subtitles.PrimaryRows[0].Label);
    }

    [Fact]
    public void Transport_cc_control_is_unchanged_and_does_not_open_sheet()
    {
        var order = PlayerShell.Boot().Transport.Order;
        Assert.Equal(TransportControl.Captions, order[8]);
        Assert.DoesNotContain("SubtitleSheet", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("Delay", Enum.GetNames<TransportControl>());
        Assert.False(PlayerShell.Boot().Subtitles.HasDelaySheet);
        Assert.False(PlayerShell.Boot().Subtitles.CcHasLongPress);
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.False(session.Shell.Subtitles.Open);
        session.ToggleCaptions();
        Assert.False(session.Shell.Subtitles.Open);
        Assert.False(session.Shell.Transport.CaptionsOn);
        session.OpenSubtitleSheet();
        Assert.True(session.Shell.Subtitles.Open);
        session.ToggleCaptions();
        Assert.True(session.Shell.Subtitles.Open);
        Assert.True(session.Shell.Subtitles.CcNeverOpensSheet);
        Assert.True(session.Shell.Subtitles.OpensFromViewMenuOnly);
    }
}
