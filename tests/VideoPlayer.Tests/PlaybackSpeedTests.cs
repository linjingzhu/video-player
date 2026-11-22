using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class PlaybackSpeedTests
{
    [Fact]
    public void Step_clamps_to_the_half_through_double_range_on_tenths()
    {
        Assert.Equal(1.2, PlaybackSpeed.Nudge(1.1, +1));
        Assert.Equal(0.9, PlaybackSpeed.Nudge(1.0, -1));
        Assert.Equal(2.0, PlaybackSpeed.Nudge(1.96, +1));
        Assert.Equal(0.5, PlaybackSpeed.Nudge(0.5, -1));
        Assert.Equal(1.0, PlaybackSpeed.Nudge(double.NaN, 0));
        Assert.Equal(1.35, PlaybackSpeed.Nudge(1.25, +1));
        Assert.Equal("배속 1.2x", PlaybackSpeed.OsdText(1.2));
        Assert.Equal("배속 1.0x", PlaybackSpeed.MenuHeader(1));
        Assert.Equal("1.25x", PlaybackSpeed.Format(1.25));
        Assert.Equal(new[] { 0.5, 1.0, 1.25, 1.5, 2.0 }, PlaybackSpeed.QuickMenuPresets);
        Assert.True(PlaybackSpeed.IsPresetSelected(1.25, 1.25));
        Assert.False(PlaybackSpeed.IsPresetSelected(1.2, 1.25));
    }

    [Fact]
    public void Plain_c_is_faster_v_is_captions_and_ctrl_shift_c_stays_capture()
    {
        Assert.DoesNotContain("V", SpeedKeys.PreviouslyBound);
        Assert.Equal("C", SpeedKeys.Faster);
        Assert.Equal("X", SpeedKeys.Slower);
        Assert.Equal("Z", SpeedKeys.Reset);
        Assert.Equal("V", SpeedKeys.Captions);
        Assert.Equal("V", UiCopy.CaptionsKey);

        Assert.Equal(SpeedKeys.Binding.Faster, SpeedKeys.Resolve("C", false, false, false));
        Assert.Equal(SpeedKeys.Binding.Capture, SpeedKeys.Resolve("C", true, true, false));
        Assert.Equal(SpeedKeys.Binding.None, SpeedKeys.Resolve("C", true, false, false));
        Assert.Equal(SpeedKeys.Binding.None, SpeedKeys.Resolve("C", false, true, false));
        Assert.Equal(SpeedKeys.Binding.Slower, SpeedKeys.Resolve("X", false, false, false));
        Assert.Equal(SpeedKeys.Binding.Reset, SpeedKeys.Resolve("Z", false, false, false));
        Assert.Equal(SpeedKeys.Binding.Captions, SpeedKeys.Resolve("V", false, false, false));
        Assert.Equal(SpeedKeys.Binding.None, SpeedKeys.Resolve("V", true, false, false));
        Assert.NotEqual(SpeedKeys.Binding.Faster, SpeedKeys.Resolve("c", true, true, false));
        Assert.False(VolumeChrome.IsMuteKey("V"));
        Assert.False(VolumeChrome.IsMuteKey("C"));

        var code = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml.cs"));
        var xaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml"));
        var readme = ReadRepoFile("README.md");
        Assert.Equal(1, Count(code, "case Key.V when"));
        Assert.Contains("_session.ToggleCaptions()", code, StringComparison.Ordinal);
        Assert.Contains("_session.NudgeSpeed(1)", code, StringComparison.Ordinal);
        Assert.Contains("e.Key == Key.C", code, StringComparison.Ordinal);
        Assert.Equal(2, Count(xaml, "InputGestureText=\"V\""));
        Assert.Equal(2, Count(xaml, "Click=\"Captions_Click\""));
        Assert.Contains("Plain **V**", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("case Key.V", code.Replace("case Key.V when", "", StringComparison.Ordinal), StringComparison.Ordinal);
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

    private static int Count(string text, string value)
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

    [Fact]
    public void Speed_resets_to_one_on_a_new_session_and_is_not_stored()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.NudgeSpeed(+2);
        Assert.Equal(1.2, session.Speed);
        Assert.Equal(1.2, session.Engine.Speed);
        Assert.Equal("배속 1.2x", session.Shell.OverlaySkip);
        session.ResetSpeed();
        Assert.Equal(1.0, session.Speed);
        Assert.Equal("배속 1.0x", session.Shell.OverlaySkip);
        session.SetSpeed(1.75, showOsd: true);
        Assert.Equal("1.75x", PlaybackSpeed.Format(1.75));

        var settingsPath = Path.Combine(workspace.Data, AppSettings.FileName);
        if (File.Exists(settingsPath))
        {
            Assert.DoesNotContain("speed", File.ReadAllText(settingsPath), StringComparison.OrdinalIgnoreCase);
        }

        var reopened = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Equal(1.0, reopened.Speed);
        Assert.Equal(1.0, reopened.Engine.Speed);
    }
}
