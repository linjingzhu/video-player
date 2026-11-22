using VideoPlayer.Core.Playback;

namespace VideoPlayer.Tests;

public class StageVolumeTests
{
    [Fact]
    public void Wheel_and_arrows_step_five_percent_and_do_not_reuse_skip_or_mute_keys()
    {
        Assert.Equal(0.75, StageVolume.Nudge(0.70, +1), 3);
        Assert.Equal(0.65, StageVolume.Nudge(0.70, -1), 3);
        Assert.Equal(1, StageVolume.Nudge(0.98, +1), 3);
        Assert.Equal(0, StageVolume.Nudge(0.02, -1), 3);
        Assert.Equal("볼륨 70", StageVolume.Osd(0.70));
        Assert.Equal("볼륨 0", StageVolume.Osd(double.NaN));
        Assert.Equal(0.05, StageVolume.Step);

        Assert.Empty(StageVolume.Collisions);
        Assert.False(StageVolume.ClashesWithExisting(StageVolume.UpKey));
        Assert.False(StageVolume.ClashesWithExisting(StageVolume.DownKey));
        Assert.True(StageVolume.ClashesWithExisting("Left"));
        Assert.True(StageVolume.ClashesWithExisting("Right"));
        Assert.True(StageVolume.ClashesWithExisting("M"));
        Assert.True(StageVolume.ClashesWithExisting("I"));
        Assert.True(StageVolume.ClashesWithExisting("O"));
    }

    [Fact]
    public void Stage_nudge_updates_the_level_and_osd_without_touching_the_seek_bar()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.70);
        session.NudgeVolumeBySteps(+1);
        Assert.Equal(0.75, session.Engine.Volume, 3);
        Assert.Equal("볼륨 75", session.Shell.OverlaySkip);

        var xaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml"));
        var code = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml.cs"));
        var seekAt = xaml.IndexOf("x:Name=\"SeekSlider\"", StringComparison.Ordinal);
        var seekEnd = xaml.IndexOf("/>", seekAt, StringComparison.Ordinal);
        var seek = xaml[seekAt..seekEnd];
        Assert.DoesNotContain("MouseWheel", seek, StringComparison.Ordinal);
        Assert.Contains("OnVideoHostMouseWheel", code, StringComparison.Ordinal);
        Assert.Contains("case Key.Up when StageVolumeKeyAllowed()", code, StringComparison.Ordinal);
        Assert.Contains("case Key.Down when StageVolumeKeyAllowed()", code, StringComparison.Ordinal);
        Assert.Contains("case Key.Left:", code, StringComparison.Ordinal);
        Assert.Contains("_session.SkipBack()", code, StringComparison.Ordinal);
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
}
