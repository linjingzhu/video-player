using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class JumpIntervalTests
{
    [Fact]
    public void Default_is_ten_and_v1_chrome_stays_ten_seconds()
    {
        var settings = new AppSettings();
        Assert.Equal(10, settings.JumpSeconds);
        Assert.Equal("jumpSeconds", AppSettings.JumpSecondsKey);
        Assert.Equal("-10초", UiCopy.SkipBack);
        Assert.Equal("+10초", UiCopy.SkipForward);
        Assert.Equal("-10초", PlayerShell.Boot().Transport.SkipBackLabel);
        Assert.Equal("+10초", PlayerShell.Boot().Transport.SkipForwardLabel);
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
        Assert.Equal("-10초", reopened.Shell.Transport.SkipBackLabel);
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

        Assert.Equal(25, session.SetJumpSeconds(25));
        session.SkipBack();
        Assert.Equal(65, engine.Position);
        Assert.Equal("-10초", session.Shell.Transport.SkipBackLabel);
    }
}
