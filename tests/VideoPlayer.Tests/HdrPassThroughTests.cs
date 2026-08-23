using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class HdrPassThroughTests
{
    [Fact]
    public void Default_is_auto_passthrough_on_the_d3d11_libmpv_path()
    {
        Assert.Equal(HdrMode.Auto, HdrPassThrough.Default);
        Assert.True(HdrPassThrough.IsPassThrough(HdrMode.Auto));
        Assert.False(HdrPassThrough.IsPassThrough(HdrMode.Off));
        Assert.Equal("d3d11", HdrPassThrough.GpuApi);
        Assert.Equal("gpu", HdrPassThrough.Vo);
        Assert.Equal("d3d11va", HdrPassThrough.Hwdec);
        Assert.Equal(HardwareDecodeAttempt.D3D11VA, new HardwareDecodePolicy().Attempts[0]);

        var auto = HdrPassThrough.Options(HdrMode.Auto);
        Assert.Contains(auto, option => option is { Name: "gpu-api", Value: "d3d11" });
        Assert.Contains(auto, option => option is { Name: "vo", Value: "gpu" });
        Assert.Contains(auto, option => option is { Name: "target-colorspace-hint", Value: "yes" });
        Assert.Contains(auto, option => option is { Name: "target-trc", Value: "auto" });
        Assert.Contains(auto, option => option is { Name: "target-prim", Value: "auto" });

        var off = HdrPassThrough.Options(HdrMode.Off);
        Assert.Contains(off, option => option is { Name: "gpu-api", Value: "d3d11" });
        Assert.Contains(off, option => option is { Name: "target-colorspace-hint", Value: "no" });
        Assert.Contains(off, option => option is { Name: "target-trc", Value: "srgb" });
        Assert.Contains(off, option => option is { Name: "target-prim", Value: "bt.709" });
    }

    [Fact]
    public void View_menu_is_hdr_auto_and_hdr_off_with_no_badge_panel_or_cast()
    {
        var shell = PlayerShell.Boot();
        Assert.Equal("HDR", UiCopy.Hdr);
        Assert.Equal("HDR 자동", UiCopy.HdrAuto);
        Assert.Equal("HDR 끄기", UiCopy.HdrOff);
        Assert.Equal(new[] { "HDR 자동", "HDR 끄기" }, UiCopy.HdrChoices);
        Assert.Equal(UiCopy.HdrChoices, shell.Hdr.ViewItems);
        Assert.True(shell.Hdr.OpensFromViewMenuOnly);
        Assert.True(shell.Hdr.AddedToExistingViewMenu);
        Assert.False(shell.Hdr.HasSubmenu);
        Assert.False(shell.Hdr.HasTwoColumnSettingsPanel);
        Assert.False(shell.Hdr.HasSettingsLeftRail);
        Assert.False(shell.Hdr.HasQuickMenu);
        Assert.Equal(HdrMode.Auto, shell.Hdr.Mode);
        Assert.True(shell.Hdr.PassThroughWhenDisplaySupports);
        Assert.False(shell.Hdr.HasBadgeOnTransport);
        Assert.False(shell.Transport.HasHdrBadge);
        Assert.Equal("-10초", shell.Transport.SkipBackLabel);
        Assert.Equal("+10초", shell.Transport.SkipForwardLabel);
        Assert.False(shell.HasCast);
        Assert.False(shell.HasMiracast);
        Assert.False(shell.Hdr.HasCast);
        Assert.False(shell.Hdr.HasMiracast);
        Assert.DoesNotContain("Hdr", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("Cast", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("Miracast", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("QuickMenu", Enum.GetNames<TransportControl>());
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
        Assert.Equal(new[] { "파일", "보기" }, shell.Menus);
    }

    [Theory]
    [InlineData(null, HdrMode.Auto)]
    [InlineData("", HdrMode.Auto)]
    [InlineData("auto", HdrMode.Auto)]
    [InlineData("AUTO", HdrMode.Auto)]
    [InlineData("자동", HdrMode.Auto)]
    [InlineData("HDR 자동", HdrMode.Auto)]
    [InlineData("yes", HdrMode.Auto)]
    [InlineData("force", HdrMode.Auto)]
    [InlineData("off", HdrMode.Off)]
    [InlineData("OFF", HdrMode.Off)]
    [InlineData("끄기", HdrMode.Off)]
    [InlineData("HDR 끄기", HdrMode.Off)]
    [InlineData("no", HdrMode.Off)]
    public void Parse_unknown_or_empty_falls_back_to_auto(string? value, HdrMode expected)
        => Assert.Equal(expected, HdrPassThrough.Parse(value));

    [Fact]
    public void Missing_or_invalid_settings_json_uses_auto()
    {
        Assert.Equal(HdrMode.Auto, new AppSettings().Hdr);
        Assert.Equal("hdr", AppSettings.HdrKey);
        Assert.Equal(HdrMode.Auto, AppSettings.FromJson(null).Hdr);
        Assert.Equal(HdrMode.Auto, AppSettings.FromJson("").Hdr);
        Assert.Equal(HdrMode.Auto, AppSettings.FromJson("{ not json").Hdr);
        Assert.Equal(HdrMode.Auto, AppSettings.FromJson("""{"other": 1}""").Hdr);
        Assert.Equal(HdrMode.Auto, AppSettings.FromJson("""{"hdr":"force"}""").Hdr);
        Assert.Equal(HdrMode.Off, AppSettings.FromJson("""{"hdr":"off"}""").Hdr);
        Assert.Equal(HdrMode.Off, AppSettings.FromJson("""{"hdr":"끄기"}""").Hdr);
        Assert.Equal(HdrMode.Off, AppSettings.FromJson("""{"hdr":"HDR 끄기"}""").Hdr);

        var settings = new AppSettings();
        Assert.Equal(HdrMode.Off, settings.SetHdr(HdrMode.Off));
        Assert.Contains("\"hdr\": \"off\"", settings.ToJson(), StringComparison.Ordinal);
        Assert.Equal(HdrMode.Auto, settings.SetHdr((HdrMode)99));
        Assert.Contains("\"hdr\": \"auto\"", settings.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public void Session_defaults_to_auto_and_applies_off_live_without_a_transport_badge()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("hdr.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 40 };
        var session = new PlaybackSession(engine, workspace.Data);

        Assert.Equal(HdrMode.Auto, session.HdrMode);
        Assert.Equal(HdrMode.Auto, engine.HdrMode);
        Assert.True(session.Shell.Hdr.PassThroughWhenDisplaySupports);
        Assert.False(session.Shell.Transport.HasHdrBadge);

        session.Open(video);
        Assert.False(session.Shell.Status.Visible);
        Assert.DoesNotContain("HDR", session.Shell.Status.Text, StringComparison.Ordinal);

        Assert.Equal(HdrMode.Off, session.SetHdrMode(HdrMode.Off));
        Assert.Equal(HdrMode.Off, session.HdrMode);
        Assert.Equal(HdrMode.Off, engine.HdrMode);
        Assert.False(session.Shell.Hdr.PassThroughWhenDisplaySupports);
        Assert.False(session.Shell.Hdr.HasBadgeOnTransport);

        var settingsPath = Path.Combine(workspace.Data, AppSettings.FileName);
        Assert.True(File.Exists(settingsPath));
        Assert.Contains("\"hdr\": \"off\"", File.ReadAllText(settingsPath), StringComparison.Ordinal);
        Assert.DoesNotContain("hdr", session.Resume.ToJson(), StringComparison.Ordinal);

        var reopened = new PlaybackSession(new FakeMediaEngine { Duration = 40 }, workspace.Data);
        Assert.Equal(HdrMode.Off, reopened.HdrMode);
        Assert.Equal(HdrMode.Off, reopened.Engine.HdrMode);
        Assert.False(reopened.Shell.Hdr.PassThroughWhenDisplaySupports);
        Assert.False(reopened.Shell.HasCast);
        Assert.False(reopened.Shell.HasMiracast);
    }
}
