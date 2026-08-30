using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class JumpIntervalTests
{
    [Fact]
    public void Default_is_ten_and_sheet_copy_matches_lock()
    {
        var settings = new AppSettings();
        var sheet = PlayerShell.Boot().Jump;
        Assert.Equal(10, settings.JumpSeconds);
        Assert.Equal(10, JumpInterval.DefaultSeconds);
        Assert.Equal("jumpSeconds", AppSettings.JumpSecondsKey);
        Assert.Equal("-10초", UiCopy.SkipBack);
        Assert.Equal("+10초", UiCopy.SkipForward);
        Assert.Equal("점프 초", UiCopy.JumpSeconds);
        Assert.Equal("앞뒤 같은 값", UiCopy.JumpSecondsSameValue);
        Assert.Equal("1–60 정수", UiCopy.JumpSecondsHint);
        Assert.Equal("기본 10 · 전역 · AppData", UiCopy.JumpSecondsFooter);
        Assert.Equal("취소", UiCopy.JumpSecondsCancel);
        Assert.Equal("확인", UiCopy.JumpSecondsConfirm);
        Assert.Equal("−", UiCopy.JumpSecondsMinus);
        Assert.Equal("+", UiCopy.JumpSecondsPlus);
        Assert.Equal("-10초", PlayerShell.Boot().Transport.SkipBackLabel);
        Assert.Equal("+10초", PlayerShell.Boot().Transport.SkipForwardLabel);
        Assert.Equal("-10초", PlayerShell.Boot().OsdSkipBackLabel);
        Assert.Equal("+10초", PlayerShell.Boot().OsdSkipForwardLabel);
        Assert.False(sheet.Open);
        Assert.Equal(10, sheet.Draft);
        Assert.Equal("10", sheet.ValueText);
        Assert.Equal("점프 초", sheet.Title);
        Assert.Equal("앞뒤 같은 값", sheet.SameValueLabel);
        Assert.Equal("1–60 정수", sheet.Hint);
        Assert.Equal("기본 10 · 전역 · AppData", sheet.Footer);
        Assert.Equal("퀵메뉴 ±10", sheet.QuickMenuPreview);
        Assert.Equal("OSD ±10", sheet.OsdPreview);
        Assert.Equal("화살표 ±10", sheet.ArrowPreview);
        Assert.True(sheet.SameValueForwardBack);
        Assert.False(sheet.SeparateForwardBackFields);
        Assert.True(sheet.OpensFromViewMenuOnly);
        Assert.False(sheet.HasTransportControl);
        Assert.False(SeriesOn.JumpSecondsOnTransport);
        Assert.False(SeriesOn.SkipPlusMinusOnTransport);
        Assert.False(PlayerShell.Boot().Transport.SkipLabelsOnBar);
        Assert.Equal(SeriesOn.Accent, sheet.AccentColor);
        Assert.Equal("#C6FF00", sheet.AccentColor);
        Assert.Equal(SeriesOn.Panel, sheet.PanelColor);
        Assert.Contains("점프 초", UiCopy.ViewMenuItems);
        Assert.Equal(UiCopy.ViewMenuItems, UiCopy.ViewMenuItemsFor(10));
        Assert.Equal("-25초", UiCopy.ViewMenuItemsFor(25)[0]);
        Assert.Equal("+25초", UiCopy.ViewMenuItemsFor(25)[1]);
        Assert.Equal("점프 초", UiCopy.ViewMenuItemsFor(25)[2]);
        Assert.DoesNotContain("JumpSeconds", Enum.GetNames<TransportControl>());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(60, 60)]
    [InlineData(10, 10)]
    [InlineData(0, 10)]
    [InlineData(-3, 10)]
    [InlineData(61, 10)]
    [InlineData(100, 10)]
    public void Jump_seconds_clamp_to_one_through_sixty(int input, int expected)
        => Assert.Equal(expected, JumpInterval.Clamp(input));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(1, 1)]
    [InlineData(60, 60)]
    [InlineData(61, 60)]
    [InlineData(100, 60)]
    public void Sheet_stepper_stays_on_the_one_through_sixty_edge(int input, int expected)
        => Assert.Equal(expected, JumpInterval.ClampDraft(input));

    [Fact]
    public void Missing_or_invalid_json_uses_default()
    {
        Assert.Equal(10, AppSettings.FromJson(null).JumpSeconds);
        Assert.Equal(10, AppSettings.FromJson("").JumpSeconds);
        Assert.Equal(10, AppSettings.FromJson("{ not json").JumpSeconds);
        Assert.Equal(10, AppSettings.FromJson("""{"other": 5}""").JumpSeconds);
        Assert.Equal(10, AppSettings.FromJson("""{"jumpSeconds": 0}""").JumpSeconds);
        Assert.Equal(10, AppSettings.FromJson("""{"jumpSeconds": 99}""").JumpSeconds);
        Assert.Equal(15, AppSettings.FromJson("""{"jumpSeconds": 15}""").JumpSeconds);
    }

    [Fact]
    public void Setting_is_global_appdata_not_per_title()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine { Duration = 200 }, workspace.Data);
        Assert.Equal(10, session.JumpSeconds);

        session.SetJumpSeconds(20);
        var settingsPath = Path.Combine(workspace.Data, AppSettings.FileName);
        Assert.True(File.Exists(settingsPath));
        Assert.Contains("\"jumpSeconds\": 20", File.ReadAllText(settingsPath), StringComparison.Ordinal);
        Assert.DoesNotContain("jumpSeconds", session.Resume.ToJson(), StringComparison.Ordinal);

        var reopened = new PlaybackSession(new FakeMediaEngine { Duration = 200 }, workspace.Data);
        Assert.Equal(20, reopened.JumpSeconds);
        Assert.Equal("-20초", reopened.Shell.Transport.SkipBackLabel);
        Assert.Equal("+20초", reopened.Shell.Transport.SkipForwardLabel);
        Assert.Equal("-20초", reopened.Shell.OsdSkipBackLabel);
        Assert.Equal("+20초", reopened.Shell.OsdSkipForwardLabel);
        Assert.Equal("-20초", UiCopy.ViewMenuItemsFor(reopened.JumpSeconds)[0]);
        Assert.Equal("+20초", UiCopy.ViewMenuItemsFor(reopened.JumpSeconds)[1]);
    }

    [Fact]
    public void SetJumpSeconds_applies_to_the_next_skip_without_reopen()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ep.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 200 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(80);

        Assert.Equal(10, session.SetJumpSeconds(10));
        session.SkipForward();
        Assert.Equal(90, engine.Position);
        Assert.Equal("+10초", session.Shell.OverlaySkip);

        Assert.Equal(25, session.SetJumpSeconds(25));
        session.SkipBack();
        Assert.Equal(65, engine.Position);
        Assert.Equal("-25초", session.Shell.Transport.SkipBackLabel);
        Assert.Equal("+25초", session.Shell.Transport.SkipForwardLabel);
        Assert.Equal("-25초", session.Shell.OsdSkipBackLabel);
        Assert.Equal("-25초", session.Shell.OverlaySkip);
    }

    [Fact]
    public void Confirm_persists_and_updates_skip_labels_cancel_does_not()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ep.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 200 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(80);

        session.OpenJumpSecondsSheet();
        Assert.True(session.Shell.Jump.Open);
        Assert.Equal(10, session.Shell.Jump.Draft);
        session.NudgeJumpSecondsDraft(5);
        Assert.Equal(15, session.Shell.Jump.Draft);
        Assert.Equal("퀵메뉴 ±15", session.Shell.Jump.QuickMenuPreview);
        Assert.Equal("OSD ±15", session.Shell.Jump.OsdPreview);
        Assert.Equal("화살표 ±15", session.Shell.Jump.ArrowPreview);
        Assert.Equal(10, session.JumpSeconds);

        session.CloseJumpSecondsSheet();
        Assert.False(session.Shell.Jump.Open);
        Assert.Equal(10, session.JumpSeconds);
        Assert.Equal("-10초", session.Shell.Transport.SkipBackLabel);

        session.OpenJumpSecondsSheet();
        session.NudgeJumpSecondsDraft(-20);
        Assert.Equal(1, session.Shell.Jump.Draft);
        session.NudgeJumpSecondsDraft(100);
        Assert.Equal(60, session.Shell.Jump.Draft);
        session.NudgeJumpSecondsDraft(-45);
        Assert.Equal(15, session.ConfirmJumpSeconds());
        Assert.False(session.Shell.Jump.Open);
        Assert.Equal(15, session.JumpSeconds);
        Assert.Equal("-15초", session.Shell.Transport.SkipBackLabel);
        Assert.Equal("+15초", session.Shell.Transport.SkipForwardLabel);
        Assert.Equal("-15초", session.Shell.OsdSkipBackLabel);
        Assert.Equal("+15초", session.Shell.OsdSkipForwardLabel);

        var settingsPath = Path.Combine(workspace.Data, AppSettings.FileName);
        Assert.Contains("\"jumpSeconds\": 15", File.ReadAllText(settingsPath), StringComparison.Ordinal);

        session.SkipForward();
        Assert.Equal(95, engine.Position);

        var reopened = new PlaybackSession(new FakeMediaEngine { Duration = 200 }, workspace.Data);
        Assert.Equal(15, reopened.JumpSeconds);
        Assert.Equal("+15초", reopened.Shell.Transport.SkipForwardLabel);
        reopened.Open(video);
        reopened.SeekAbsolute(40);
        reopened.SkipBack();
        Assert.Equal(25, reopened.Engine.Position);
        Assert.Equal("-15초", reopened.Shell.OverlaySkip);
    }
}
