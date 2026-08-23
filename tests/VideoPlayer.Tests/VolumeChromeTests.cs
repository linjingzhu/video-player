using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class VolumeChromeTests
{
    [Fact]
    public void Transport_keeps_speaker_with_horizontal_slider_and_numeric()
    {
        var shell = PlayerShell.Boot();
        Assert.True(shell.Transport.HorizontalVolumeSlider);
        Assert.True(shell.Volume.HorizontalSliderOnTransport);
        Assert.True(shell.Volume.SpeakerOnTransport);
        Assert.False(shell.Volume.VerticalPopover);
        Assert.True(shell.Volume.PercentBesideSlider);
        Assert.False(shell.Volume.PercentOnTop);
        Assert.False(shell.Volume.WhiteThumb);
        Assert.False(shell.Volume.SquareThumb);
        Assert.True(shell.Volume.RoundThumb);
        Assert.True(shell.Volume.AccentOnFilledTrackOnly);
        Assert.True(shell.Volume.WheelOverSpeakerChangesVolume);
        Assert.False(shell.Volume.WheelOverPopoverChangesVolume);
        Assert.True(shell.Volume.WheelOverSliderChangesVolume);
        Assert.True(shell.Volume.WheelShowsPercent);
        Assert.True(shell.Volume.CompactDensity);
        Assert.False(shell.Volume.ClickAgainCloses);
        Assert.False(shell.Volume.ClickOutsideCloses);
        Assert.False(shell.Volume.SpeakerRightClickMutes);
        Assert.True(shell.Volume.SpeakerClickMutes);
        Assert.False(shell.Volume.SpeakerClickTogglesPopover);
        Assert.Equal(12, VolumeChrome.PercentSize);
        Assert.Equal(SeriesOn.BodySize, VolumeChrome.PercentSize);
        Assert.Equal(88, VolumeChrome.SliderWidth);
        Assert.Equal(4, VolumeChrome.TrackHeight);
        Assert.Equal(new[] { 4, 8, 12, 16 }, VolumeChrome.SpacingScale);
        Assert.DoesNotContain(10, VolumeChrome.SpacingScale);
        Assert.DoesNotContain(20, VolumeChrome.SpacingScale);
        Assert.DoesNotContain(24, VolumeChrome.SpacingScale);
        Assert.Equal(40, SkinA.TransportHeightPx);
        Assert.Equal(28, SkinA.SidebarRailWidthPx);
        Assert.Contains(TransportControl.Volume, shell.Transport.Order);
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
            shell.Transport.Order);
        Assert.Contains("Hamburger", Enum.GetNames<TransportControl>());
        Assert.True(shell.NextEpisode.OverlayOnly);
        Assert.True(shell.NextEpisode.EndRegionOnly);
        Assert.False(shell.NextEpisode.OnTransport);
    }

    [Fact]
    public void Mute_is_keyboard_m_speaker_click_or_volume_zero()
    {
        var chrome = PlayerShell.Boot().Volume;
        Assert.Equal(new[] { "M" }, VolumeChrome.MuteKeys);
        Assert.True(VolumeChrome.IsMuteKey("M"));
        Assert.True(VolumeChrome.IsMuteKey("m"));
        Assert.False(VolumeChrome.IsMuteKey("C"));
        Assert.True(chrome.SpeakerClickMutes);
        Assert.False(chrome.SpeakerRightClickMutes);
        Assert.False(chrome.SpeakerClickTogglesPopover);
        Assert.True(chrome.VolumeZeroMutes);

        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.72);
        Assert.Equal(72, session.Shell.Volume.Percent);
        Assert.Equal("72", session.Shell.Volume.PercentText);
        Assert.False(session.Shell.Volume.Muted);

        Assert.True(session.HandleHotkey("M"));
        Assert.True(session.Shell.Volume.Muted);
        Assert.Equal(0, session.Engine.Volume);
        Assert.Equal(0, session.Shell.Volume.Percent);

        Assert.True(session.HandleHotkey("m"));
        Assert.False(session.Shell.Volume.Muted);
        Assert.Equal(0.72, session.Engine.Volume, 3);
        Assert.Equal(72, session.Shell.Volume.Percent);

        session.SetVolume(0);
        Assert.True(session.Shell.Volume.Muted);
        Assert.Equal(0, session.Engine.Volume);

        session.HandleHotkey("M");
        Assert.False(session.Shell.Volume.Muted);
        Assert.Equal(0.72, session.Engine.Volume, 3);

        Assert.False(session.HandleHotkey("C"));
        Assert.False(session.Shell.Volume.Muted);
        Assert.Equal(0.72, session.Engine.Volume, 3);
    }

    [Fact]
    public void Speaker_click_mutes_and_does_not_open_a_popover()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.55);
        Assert.False(session.Shell.Volume.PopoverOpen);

        session.ToggleMute();
        Assert.False(session.Shell.Volume.PopoverOpen);
        Assert.True(session.Shell.Volume.Muted);
        Assert.Equal(0, session.Engine.Volume);
        Assert.Equal(0, session.Shell.Volume.Percent);

        session.ToggleMute();
        Assert.False(session.Shell.Volume.PopoverOpen);
        Assert.False(session.Shell.Volume.Muted);
        Assert.Equal(0.55, session.Engine.Volume, 3);

        session.ToggleVolumePopover();
        Assert.False(session.Shell.Volume.PopoverOpen);
        Assert.False(session.Shell.Volume.Muted);

        session.CloseVolumePopover();
        Assert.False(session.Shell.Volume.PopoverOpen);
    }

    [Fact]
    public void Click_outside_has_no_popover_to_close()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.40);
        session.ToggleVolumePopover();
        Assert.False(session.Shell.Volume.PopoverOpen);

        session.CloseVolumePopover();
        Assert.False(session.Shell.Volume.PopoverOpen);
        Assert.False(session.Shell.Volume.Muted);
        Assert.Equal(0.40, session.Engine.Volume, 3);
    }

    [Fact]
    public void Right_click_does_not_mute_or_open_the_popover()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.72);
        session.SpeakerRightClick();
        Assert.False(session.Shell.Volume.Muted);
        Assert.False(session.Shell.Volume.PopoverOpen);
        Assert.Equal(0.72, session.Engine.Volume, 3);
        Assert.Equal(72, session.Shell.Volume.Percent);

        session.ToggleVolumePopover();
        session.SpeakerRightClick();
        Assert.False(session.Shell.Volume.PopoverOpen);
        Assert.False(session.Shell.Volume.Muted);
        Assert.Equal(0.72, session.Engine.Volume, 3);
    }

    [Fact]
    public void Wheel_steps_change_volume_and_show_percent()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.50);
        session.NudgeVolumeFromWheel(0.05);
        Assert.False(session.Shell.Volume.PopoverOpen);
        Assert.Equal(0.55, session.Engine.Volume, 3);
        Assert.Equal(55, session.Shell.Volume.Percent);
        Assert.Equal("55", session.Shell.Volume.PercentText);
        session.NudgeVolumeFromWheel(-0.05);
        Assert.False(session.Shell.Volume.PopoverOpen);
        Assert.Equal(0.50, session.Engine.Volume, 3);
        Assert.Equal(50, session.Shell.Volume.Percent);
    }

    [Fact]
    public void Last_volume_persists_to_appdata_and_restores_without_speed()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.42);
        session.SetSpeed(1.5);
        session.RememberVolume();

        var settingsPath = Path.Combine(workspace.Data, AppSettings.FileName);
        Assert.True(File.Exists(settingsPath));
        var json = File.ReadAllText(settingsPath);
        Assert.Contains("\"volume\":", json, StringComparison.Ordinal);
        Assert.Contains("\"muted\":", json, StringComparison.Ordinal);
        Assert.DoesNotContain("speed", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("volume", session.Resume.ToJson(), StringComparison.Ordinal);

        var loaded = AppSettings.FromJson(json);
        Assert.Equal(0.42, loaded.Volume, 3);
        Assert.False(loaded.Muted);

        var reopened = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Equal(0.42, reopened.Engine.Volume, 3);
        Assert.False(reopened.Shell.Volume.Muted);
        Assert.Equal(42, reopened.Shell.Volume.Percent);
        Assert.Equal(1.0, reopened.Speed);
        Assert.Equal(1.0, reopened.Engine.Speed);
    }

    [Fact]
    public void Muted_and_level_round_trip_on_reopen()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.72);
        session.ToggleMute();
        Assert.True(session.Shell.Volume.Muted);
        Assert.Equal(0, session.Engine.Volume);
        session.RememberVolume();

        var loaded = AppSettings.FromJson(File.ReadAllText(Path.Combine(workspace.Data, AppSettings.FileName)));
        Assert.Equal(0.72, loaded.Volume, 3);
        Assert.True(loaded.Muted);

        var reopened = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.True(reopened.Shell.Volume.Muted);
        Assert.Equal(0, reopened.Engine.Volume);
        Assert.Equal(1.0, reopened.Speed);

        reopened.ToggleMute();
        Assert.False(reopened.Shell.Volume.Muted);
        Assert.Equal(0.72, reopened.Engine.Volume, 3);
        Assert.Equal(72, reopened.Shell.Volume.Percent);
    }

    [Fact]
    public void Missing_or_invalid_volume_json_uses_full_unmuted()
    {
        Assert.Equal(1.0, AppSettings.FromJson(null).Volume);
        Assert.False(AppSettings.FromJson(null).Muted);
        Assert.Equal(1.0, AppSettings.FromJson("").Volume);
        Assert.Equal(1.0, AppSettings.FromJson("{ not json").Volume);
        Assert.Equal(1.0, AppSettings.FromJson("""{"other": 5}""").Volume);
        Assert.Equal(0, AppSettings.FromJson("""{"volume": 0}""").Volume);
        Assert.True(AppSettings.FromJson("""{"volume": 0}""").Muted);
        Assert.Equal(1.0, AppSettings.FromJson("""{"volume": 1.4}""").Volume);
        Assert.Equal(0.55, AppSettings.FromJson("""{"volume": 0.55}""").Volume, 3);
        Assert.True(AppSettings.FromJson("""{"volume": 0.4, "muted": true}""").Muted);
        Assert.Equal(0.4, AppSettings.FromJson("""{"volume": 0.4, "muted": true}""").Volume, 3);
        Assert.Equal("volume", AppSettings.VolumeKey);
        Assert.Equal("muted", AppSettings.MutedKey);
    }
}
