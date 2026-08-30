using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class MiracastProjectionTests
{
    [Fact]
    public void Idle_view_item_is_play_to_and_becomes_disconnect_while_projecting()
    {
        Assert.Equal("연결 장치로 재생", UiCopy.CastPlayTo);
        Assert.Equal("연결 끄기", UiCopy.CastDisconnect);
        Assert.Equal("장치에 연결할 수 없습니다.", UiCopy.CastFailed);
        Assert.Equal(new[] { "연결 장치로 재생", "연결 끄기" }, UiCopy.CastLabels);
        Assert.Equal(UiCopy.CastLabels, MiracastProjection.MenuLabels);
        Assert.Equal("연결 장치로 재생", MiracastProjection.MenuLabel(false));
        Assert.Equal("연결 끄기", MiracastProjection.MenuLabel(true));
        Assert.Contains("연결 장치로 재생", UiCopy.ViewMenuItems);
        Assert.DoesNotContain("연결 끄기", UiCopy.ViewMenuItems);

        var shell = PlayerShell.Boot();
        Assert.Equal("연결 장치로 재생", shell.PlayTo.MenuLabel);
        Assert.False(shell.PlayTo.IsConnected);
        Assert.True(shell.PlayTo.OpensFromViewMenuOnly);
        Assert.True(shell.PlayTo.AddedToExistingViewMenu);
        Assert.True(shell.PlayTo.UsesOsPicker);
        Assert.True(shell.PlayTo.UsesProjectionManager);
        Assert.False(shell.PlayTo.HasCustomDeviceList);
        Assert.False(shell.PlayTo.HasBadgeOnTransport);
        Assert.False(shell.PlayTo.HasCastIcon);
        Assert.False(shell.PlayTo.HasEjectIcon);
        Assert.False(shell.PlayTo.AllowsDlna);
        Assert.False(shell.PlayTo.AllowsChromecast);
        Assert.False(shell.PlayTo.AllowsAirPlay);
        Assert.False(shell.HasCast);
        Assert.False(shell.HasMiracast);
        Assert.False(shell.Transport.HasCastIcon);
        Assert.False(shell.Transport.HasEjectIcon);
        Assert.False(SeriesOn.HasCastIcon);
        Assert.DoesNotContain("Cast", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("Miracast", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("PlayTo", Enum.GetNames<TransportControl>());
        Assert.True(MiracastProjection.UsesOsPicker);
        Assert.True(MiracastProjection.UsesProjectionManager);
        Assert.False(MiracastProjection.UsesCustomDeviceList);
        Assert.False(MiracastProjection.AllowsDlna);
        Assert.False(MiracastProjection.AllowsChromecast);
        Assert.False(MiracastProjection.AllowsAirPlay);
        Assert.False(MiracastProjection.AddsTransportButton);
        Assert.False(MiracastProjection.AddsCaptionIcon);
        Assert.Equal("Windows.UI.ViewManagement.ProjectionManager", MiracastProjection.ProjectionManagerClass);
        Assert.Equal("ms-settings-connect:", MiracastProjection.ConnectPickerUri);
        Assert.Equal(15u, MiracastProjection.MiracastOutputTechnology);
        Assert.True(MiracastProjection.AllowsSource(MediaSourceKind.None));
        Assert.True(MiracastProjection.AllowsSource(MediaSourceKind.LocalFile));
        Assert.True(MiracastProjection.AllowsSource(MediaSourceKind.HttpUrl));
    }

    [Fact]
    public void Session_start_uses_host_and_flips_the_menu_label()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ep.mkv", [1]);
        var host = new FakeWirelessDisplayHost();
        var session = new PlaybackSession(new FakeMediaEngine { Duration = 40 }, workspace.Data, host);

        Assert.Equal("연결 장치로 재생", session.PlayToMenuLabel);
        Assert.False(session.IsProjecting);
        Assert.False(session.Shell.Status.Visible);

        Assert.True(session.Open(video).Success);
        var started = session.TogglePlayTo();
        Assert.True(started.Succeeded);
        Assert.Equal(WirelessDisplayKind.Connected, started.Kind);
        Assert.Equal(1, host.StartCalls);
        Assert.Equal(0, host.StopCalls);
        Assert.True(session.IsProjecting);
        Assert.Equal("연결 끄기", session.PlayToMenuLabel);
        Assert.False(session.Shell.Status.Visible);
        Assert.False(session.Shell.Transport.HasCastIcon);
    }

    [Fact]
    public void Session_stop_disconnects_and_restores_the_idle_label()
    {
        using var workspace = new TempWorkspace();
        var host = new FakeWirelessDisplayHost();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data, host);
        Assert.True(session.OpenUrl("https://example.com/a.mkv").Success);

        session.TogglePlayTo();
        Assert.Equal("연결 끄기", session.PlayToMenuLabel);

        var stopped = session.TogglePlayTo();
        Assert.True(stopped.Succeeded);
        Assert.Equal(WirelessDisplayKind.Disconnected, stopped.Kind);
        Assert.Equal(1, host.StopCalls);
        Assert.False(session.IsProjecting);
        Assert.Equal("연결 장치로 재생", session.PlayToMenuLabel);
        Assert.False(session.Shell.Status.Visible);
    }

    [Fact]
    public void Failure_uses_the_dashed_korean_banner_and_does_not_throw()
    {
        using var workspace = new TempWorkspace();
        var host = new FakeWirelessDisplayHost { FailNext = true };
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data, host);

        var failed = session.TogglePlayTo();
        Assert.True(failed.IsFailure);
        Assert.Equal(1, host.StartCalls);
        Assert.False(session.IsProjecting);
        Assert.Equal("연결 장치로 재생", session.PlayToMenuLabel);
        Assert.True(session.Shell.Status.Visible);
        Assert.Equal("장치에 연결할 수 없습니다.", session.Shell.Status.Text);
        Assert.True(StatusText.IsCastFailure(session.Shell.Status.Text));
        Assert.True(StatusText.IsConfirmedFailureLine(session.Shell.Status.Text));
    }

    [Fact]
    public void Thrown_host_is_caught_and_still_shows_the_failure_banner()
    {
        using var workspace = new TempWorkspace();
        var host = new FakeWirelessDisplayHost { ThrowNext = true };
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data, host);

        var failed = session.TogglePlayTo();
        Assert.True(failed.IsFailure);
        Assert.False(session.IsProjecting);
        Assert.Equal("연결 장치로 재생", session.PlayToMenuLabel);
        Assert.Equal(UiCopy.CastFailed, session.Shell.Status.Text);
    }

    [Fact]
    public void Missing_host_fails_without_a_real_tv_and_does_not_crash()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);

        var failed = session.TogglePlayTo();
        Assert.True(failed.IsFailure);
        Assert.False(session.IsProjecting);
        Assert.Equal("연결 장치로 재생", session.PlayToMenuLabel);
        Assert.Equal(UiCopy.CastFailed, session.Shell.Status.Text);
        Assert.False(session.Shell.HasCast);
        Assert.False(session.Shell.Transport.HasCastIcon);
    }

    [Fact]
    public void Cancelled_picker_keeps_the_idle_label_and_hides_a_prior_cast_banner()
    {
        using var workspace = new TempWorkspace();
        var host = new FakeWirelessDisplayHost { FailNext = true };
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data, host);
        session.TogglePlayTo();
        Assert.True(session.Shell.Status.Visible);

        host.CancelNext = true;
        var cancelled = session.TogglePlayTo();
        Assert.Equal(WirelessDisplayKind.Cancelled, cancelled.Kind);
        Assert.False(session.IsProjecting);
        Assert.Equal("연결 장치로 재생", session.PlayToMenuLabel);
        Assert.False(session.Shell.Status.Visible);
    }

    [Fact]
    public void Tick_follows_os_disconnect_without_a_menu_click()
    {
        using var workspace = new TempWorkspace();
        var host = new FakeWirelessDisplayHost();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data, host);
        session.TogglePlayTo();
        Assert.Equal("연결 끄기", session.PlayToMenuLabel);

        host.IsProjecting = false;
        session.Tick(DateTimeOffset.UtcNow);
        Assert.False(session.IsProjecting);
        Assert.Equal("연결 장치로 재생", session.PlayToMenuLabel);
    }

    [Fact]
    public void Successful_connect_does_not_clear_an_unrelated_failure_banner()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ok.mkv", [1]);
        var engine = new FakeMediaEngine { FailHardware = true };
        var host = new FakeWirelessDisplayHost();
        var session = new PlaybackSession(engine, workspace.Data, host);
        Assert.True(session.Open(video).Success);
        Assert.Contains("SW 폴백", session.Shell.Status.Text, StringComparison.Ordinal);

        var connected = session.TogglePlayTo();
        Assert.True(connected.Succeeded);
        Assert.True(session.IsProjecting);
        Assert.Equal("연결 끄기", session.PlayToMenuLabel);
        Assert.Contains("SW 폴백", session.Shell.Status.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Serieson_chrome_stays_lime_solid_stop_clear_and_horizontal_volume()
    {
        Assert.Equal("#C6FF00", SeriesOn.Accent);
        Assert.Equal("#050505", SeriesOn.Background);
        Assert.True(SeriesOn.StopButtonExists);
        Assert.Equal("지우기", UiCopy.Clear);
        Assert.True(SeriesOn.ClearIsTextLabel);
        Assert.False(SeriesOn.ClearUsesEjectIcon);
        Assert.True(SeriesOn.HorizontalVolumeSlider);
        Assert.False(SeriesOn.VerticalVolumePopover);
        Assert.False(SeriesOn.HasCastIcon);
        Assert.False(SeriesOn.HasEjectIcon);
        Assert.Equal(new[] { "퀵메뉴" }, PlayerShell.Boot().Menus);
    }

    [Fact]
    public void Main_window_adds_the_item_to_both_existing_menus_without_cast_chrome()
    {
        var mainXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml"));
        var codeBehind = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml.cs"));
        var host = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "Projection", "WindowsProjectionHost.cs"));
        var appProject = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "VideoPlayer.App.csproj"));

        Assert.Equal(2, CountOccurrences(mainXaml, "Header=\"연결 장치로 재생\""));
        Assert.Equal(2, CountOccurrences(mainXaml, "Tag=\"playTo\""));
        Assert.Equal(2, CountOccurrences(mainXaml, "Click=\"PlayTo_Click\""));
        Assert.DoesNotContain("Cast", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Eject", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"PlayToButton\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("DLNA", host, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Chromecast", host, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AirPlay", host, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Google.Cast", appProject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SharpCaster", appProject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dlna", appProject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ProjectionManager", host, StringComparison.Ordinal);
        Assert.Contains("ms-settings-connect:", host, StringComparison.Ordinal);
        Assert.Contains("WindowsProjectionHost", codeBehind, StringComparison.Ordinal);
        Assert.Contains("PlayTo_Click", codeBehind, StringComparison.Ordinal);
        Assert.Contains("SetMenuHeader(menu, \"playTo\"", codeBehind, StringComparison.Ordinal);
        Assert.Contains("C6FF00", ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "App.xaml")), StringComparison.OrdinalIgnoreCase);
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
