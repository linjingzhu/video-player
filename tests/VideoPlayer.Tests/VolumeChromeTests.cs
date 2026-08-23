using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class VolumeChromeTests
{
    [Fact]
    public void Transport_keeps_speaker_and_drops_the_horizontal_slider()
    {
        var shell = PlayerShell.Boot();
        Assert.False(shell.Transport.HorizontalVolumeSlider);
        Assert.False(shell.Volume.HorizontalSliderOnTransport);
        Assert.True(shell.Volume.SpeakerOnTransport);
        Assert.True(shell.Volume.VerticalPopover);
        Assert.True(shell.Volume.PercentOnTop);
        Assert.True(shell.Volume.WhiteThumb);
        Assert.True(shell.Volume.SquareThumb);
        Assert.True(shell.Volume.AccentOnFilledTrackOnly);
        Assert.True(shell.Volume.WheelOverSpeakerChangesVolume);
        Assert.True(shell.Volume.WheelOverPopoverChangesVolume);
        Assert.True(shell.Volume.WheelShowsPercent);
        Assert.True(shell.Volume.CompactDensity);
        Assert.True(shell.Volume.ClickAgainCloses);
        Assert.True(shell.Volume.ClickOutsideCloses);
        Assert.False(shell.Volume.SpeakerRightClickMutes);
        Assert.Equal(12, VolumeChrome.PercentSize);
        Assert.Equal(SkinA.BodySize, VolumeChrome.PercentSize);
        Assert.Equal(8, VolumeChrome.PanelPadding);
        Assert.Equal(4, VolumeChrome.PanelRadius);
        Assert.Equal(48, VolumeChrome.PanelWidth);
        Assert.Equal(80, VolumeChrome.SliderHeight);
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
            shell.Transport.Order);
        Assert.DoesNotContain("Hamburger", Enum.GetNames<TransportControl>());
        Assert.True(shell.NextEpisode.OverlayOnly);
        Assert.True(shell.NextEpisode.EndRegionOnly);
        Assert.False(shell.NextEpisode.OnTransport);
    }

    [Fact]
    public void Mute_is_keyboard_m_or_volume_zero_not_speaker_click()
    {
        var chrome = PlayerShell.Boot().Volume;
        Assert.Equal(new[] { "M" }, VolumeChrome.MuteKeys);
        Assert.True(VolumeChrome.IsMuteKey("M"));
        Assert.True(VolumeChrome.IsMuteKey("m"));
        Assert.False(VolumeChrome.IsMuteKey("C"));
        Assert.False(chrome.SpeakerClickMutes);
        Assert.False(chrome.SpeakerRightClickMutes);
        Assert.True(chrome.SpeakerClickTogglesPopover);
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
    public void Speaker_click_toggles_the_popover_and_does_not_mute()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.55);
        Assert.False(session.Shell.Volume.PopoverOpen);

        session.ToggleVolumePopover();
        Assert.True(session.Shell.Volume.PopoverOpen);
        Assert.False(session.Shell.Volume.Muted);
        Assert.Equal(0.55, session.Engine.Volume, 3);
        Assert.Equal(55, session.Shell.Volume.Percent);

        session.ToggleVolumePopover();
        Assert.False(session.Shell.Volume.PopoverOpen);
        Assert.False(session.Shell.Volume.Muted);
        Assert.Equal(0.55, session.Engine.Volume, 3);

        session.CloseVolumePopover();
        Assert.False(session.Shell.Volume.PopoverOpen);
    }

    [Fact]
    public void Click_outside_closes_the_popover_without_muting()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.40);
        session.ToggleVolumePopover();
        Assert.True(session.Shell.Volume.PopoverOpen);

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
        Assert.True(session.Shell.Volume.PopoverOpen);
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
        Assert.True(session.Shell.Volume.PopoverOpen);
        Assert.Equal(0.55, session.Engine.Volume, 3);
        Assert.Equal(55, session.Shell.Volume.Percent);
        Assert.Equal("55", session.Shell.Volume.PercentText);
        session.NudgeVolumeFromWheel(-0.05);
        Assert.True(session.Shell.Volume.PopoverOpen);
        Assert.Equal(0.50, session.Engine.Volume, 3);
        Assert.Equal(50, session.Shell.Volume.Percent);
    }
}
