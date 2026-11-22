using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class OpenFailureWatchTests
{
    [Fact]
    public void Settle_window_stays_short_and_hwdec_names_match_libmpv()
    {
        Assert.InRange(OpenFailureWatch.SettleWindow, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(2));
        Assert.Equal("d3d11va", OpenFailureWatch.HwdecOption(HardwareDecodeAttempt.D3D11VA));
        Assert.Equal("dxva2", OpenFailureWatch.HwdecOption(HardwareDecodeAttempt.Dxva2));
        Assert.Equal("no", OpenFailureWatch.HwdecOption(HardwareDecodeAttempt.None));
        Assert.True(OpenFailureWatch.IsHardwareDecoder("d3d11va"));
        Assert.True(OpenFailureWatch.IsHardwareDecoder("dxva2-copy"));
        Assert.False(OpenFailureWatch.IsHardwareDecoder("no"));
        Assert.False(OpenFailureWatch.IsHardwareDecoder("  "));
    }

    [Fact]
    public void Hardware_no_after_load_steps_d3d11va_then_dxva2_then_software()
    {
        var state = OpenWatchState.Begin(preferHardware: true);
        state = Loaded(state);
        var dxva = OpenFailureWatch.Reduce(state, Hw("no"), httpUrl: false);
        Assert.Equal(OpenWatchAction.Retry, dxva.Action);
        Assert.Equal(HardwareDecodeAttempt.Dxva2, dxva.State.Attempt);
        Assert.False(dxva.State.FileLoaded);

        state = Loaded(dxva.State);
        var software = OpenFailureWatch.Reduce(state, Hw("no"), httpUrl: false);
        Assert.Equal(OpenWatchAction.Retry, software.Action);
        Assert.Equal(HardwareDecodeAttempt.None, software.State.Attempt);

        state = Loaded(software.State);
        var playing = OpenFailureWatch.Reduce(state, Hw("no"), httpUrl: false);
        Assert.Equal(OpenWatchAction.PlayingSoftware, playing.Action);
        Assert.False(playing.State.HardwareActive);
        Assert.True(playing.State.Settled);
        Assert.False(playing.State.Failed);

        var late = OpenFailureWatch.Reduce(Loaded(OpenWatchState.Begin(true)), Hw("no"), httpUrl: false);
        Assert.Equal(OpenWatchAction.Retry, late.Action);
        Assert.Equal(HardwareDecodeAttempt.Dxva2, late.State.Attempt);
    }

    [Fact]
    public void Hwdec_d3d11va_after_file_loaded_keeps_hardware()
    {
        var state = Loaded(OpenWatchState.Begin(true));
        var decision = OpenFailureWatch.Reduce(state, Hw("d3d11va"), false);
        Assert.Equal(OpenWatchAction.PlayingHardware, decision.Action);
        Assert.True(decision.State.HardwareActive);
        Assert.False(decision.State.Pending);

        var again = OpenFailureWatch.Reduce(decision.State, Hw("d3d11va"), false);
        Assert.Equal(OpenWatchAction.Wait, again.Action);
        Assert.True(again.State.HardwareActive);
    }

    [Fact]
    public void Hwdec_before_file_loaded_or_blank_does_not_settle()
    {
        var state = OpenWatchState.Begin(true);
        var early = OpenFailureWatch.Reduce(state, Hw("no"), false);
        Assert.Equal(OpenWatchAction.Wait, early.Action);
        Assert.Equal(HardwareDecodeAttempt.D3D11VA, early.State.Attempt);
        Assert.True(early.State.Pending);

        var loaded = Loaded(state);
        var blank = OpenFailureWatch.Reduce(loaded, Hw("  "), false);
        Assert.True(blank.State.Pending);
        Assert.Equal(OpenWatchAction.Wait, blank.Action);
    }

    [Fact]
    public void Definite_open_error_fails_without_a_hardware_retry()
    {
        var state = OpenWatchState.Begin(true);
        var local = OpenFailureWatch.Reduce(
            state,
            new OpenSignal(OpenSignalKind.EndFile, OpenSignal.EndError, OpenSignal.ErrorLoadingFailed),
            httpUrl: false);
        Assert.Equal(OpenWatchAction.Failed, local.Action);
        Assert.Equal(HardwareDecodeAttempt.D3D11VA, local.State.Attempt);
        Assert.Contains("재생 실패", local.State.FailureStatus, StringComparison.Ordinal);
        Assert.Contains("불러오지 못했습니다", local.State.FailureStatus, StringComparison.Ordinal);

        var http = OpenFailureWatch.Reduce(
            state,
            new OpenSignal(OpenSignalKind.EndFile, OpenSignal.EndError, OpenSignal.ErrorLoadingFailed),
            httpUrl: true);
        Assert.Equal(StatusText.PlaybackFailed(), http.State.FailureStatus);
        Assert.Contains(UiCopy.NetworkFailed, http.State.FailureStatus, StringComparison.Ordinal);

        var ignored = OpenFailureWatch.Reduce(local.State, Hw("d3d11va"), false);
        Assert.True(ignored.State.Failed);
        Assert.Equal(OpenWatchAction.Wait, ignored.Action);
    }

    [Fact]
    public void Decoder_error_on_hardware_retries_then_software_failure_stops()
    {
        var state = Loaded(OpenWatchState.Begin(true));
        var dxva = OpenFailureWatch.Reduce(
            state,
            new OpenSignal(OpenSignalKind.EndFile, OpenSignal.EndError, -20),
            false);
        Assert.Equal(OpenWatchAction.Retry, dxva.Action);
        Assert.Equal(HardwareDecodeAttempt.Dxva2, dxva.State.Attempt);

        var software = OpenFailureWatch.Reduce(
            Loaded(dxva.State),
            new OpenSignal(OpenSignalKind.EndFile, OpenSignal.EndError, -20),
            false);
        Assert.Equal(HardwareDecodeAttempt.None, software.State.Attempt);

        var failed = OpenFailureWatch.Reduce(
            Loaded(software.State),
            new OpenSignal(OpenSignalKind.EndFile, OpenSignal.EndError, -20),
            false);
        Assert.Equal(OpenWatchAction.Failed, failed.Action);
        Assert.Contains("파일을 열 수 없습니다", failed.State.FailureStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void Eof_stop_and_redirect_are_not_open_failures()
    {
        var loaded = Loaded(OpenWatchState.Begin(true));
        foreach (var reason in new[] { OpenSignal.EndEof, OpenSignal.EndStop, OpenSignal.EndQuit })
        {
            var decision = OpenFailureWatch.Reduce(
                loaded,
                new OpenSignal(OpenSignalKind.EndFile, reason, 0),
                false);
            Assert.Equal(OpenWatchAction.Wait, decision.Action);
            Assert.False(decision.State.Failed);
            Assert.True(decision.State.FileLoaded);
        }

        var redirect = OpenFailureWatch.Reduce(
            loaded,
            new OpenSignal(OpenSignalKind.EndFile, OpenSignal.EndRedirect, 0),
            false);
        Assert.False(redirect.State.FileLoaded);
        Assert.False(redirect.State.Failed);
    }

    [Fact]
    public void Hardware_then_no_still_steps_to_the_next_decoder()
    {
        var hardware = OpenFailureWatch.Reduce(Loaded(OpenWatchState.Begin(true)), Hw("d3d11va"), false);
        Assert.Equal(OpenWatchAction.PlayingHardware, hardware.Action);

        var later = OpenFailureWatch.Reduce(hardware.State, Hw("no"), false);
        Assert.Equal(OpenWatchAction.Retry, later.Action);
        Assert.Equal(HardwareDecodeAttempt.Dxva2, later.State.Attempt);
        Assert.False(later.State.HardwareActive);
        Assert.False(later.State.Failed);
    }

    [Fact]
    public void Session_shows_open_failure_and_software_fallback_on_the_existing_slot()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("clip.mp4", [1, 2, 3]);
        var engine = new FakeMediaEngine();
        var session = new PlaybackSession(engine, workspace.Data);
        session.SetSpeed(1.25);
        var opened = session.Open(video);
        Assert.True(opened.Success);
        Assert.False(session.Shell.Status.Visible);

        engine.RaisePlaybackFault(new PlaybackFault(
            Fatal: true,
            HardwareActive: false,
            Status: StatusText.PlaybackFailed("불러오지 못했습니다"),
            VideoCodec: "h264",
            AudioCodec: "aac"));
        Assert.Contains("재생 실패", session.Shell.Status.Text, StringComparison.Ordinal);
        Assert.Contains("불러오지 못했습니다", session.Shell.Status.Text, StringComparison.Ordinal);
        Assert.Equal(1.25, session.Speed);

        session.Shell.Status.Clear();
        engine.RaisePlaybackFault(new PlaybackFault(false, false, null, "hevc", "aac"));
        Assert.Contains(UiCopy.SoftwareFallback, session.Shell.Status.Text, StringComparison.Ordinal);
        Assert.Contains("HEVC", session.Shell.Status.Text, StringComparison.Ordinal);
        Assert.True(engine.IsOpen);
        Assert.Equal(1.0, new PlaybackSession(new FakeMediaEngine(), workspace.Data).Speed);
    }

    [Fact]
    public void Http_open_failure_uses_the_network_line_and_software_does_not_clobber_cast()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine();
        var session = new PlaybackSession(engine, workspace.Data);
        var opened = session.OpenUrl("https://example.com/video.mp4");
        Assert.True(opened.Success);

        engine.RaisePlaybackFault(new PlaybackFault(true, false, "파일을 열 수 없습니다.", null, null));
        Assert.Equal(StatusText.PlaybackFailed(), session.Shell.Status.Text);

        session.Shell.Status.Fail(UiCopy.CastFailed);
        engine.RaisePlaybackFault(new PlaybackFault(false, false, null, "h264", "aac"));
        Assert.Equal(UiCopy.CastFailed, session.Shell.Status.Text);
    }

    private static OpenWatchState Loaded(OpenWatchState state)
        => OpenFailureWatch.Reduce(state, new OpenSignal(OpenSignalKind.FileLoaded), false).State;

    private static OpenSignal Hw(string value)
        => new(OpenSignalKind.HwdecCurrent, HwdecCurrent: value);
}
