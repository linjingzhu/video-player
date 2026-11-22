namespace VideoPlayer.Core.Playback;

public enum OpenWatchAction
{
    Wait,
    Retry,
    PlayingHardware,
    PlayingSoftware,
    Failed
}

public enum OpenSignalKind
{
    FileLoaded,
    EndFile,
    HwdecCurrent
}

/// <summary>
/// One libmpv observation. End-file reason and error values match
/// mpv_end_file_reason and mpv_error in libmpv-2.
/// </summary>
public readonly record struct OpenSignal(
    OpenSignalKind Kind,
    int EndReason = OpenSignal.NoEnd,
    int ErrorCode = 0,
    string? HwdecCurrent = null)
{
    public const int NoEnd = -1;
    public const int EndEof = 0;
    public const int EndStop = 2;
    public const int EndQuit = 3;
    public const int EndError = 4;
    public const int EndRedirect = 5;

    public const int ErrorLoadingFailed = -13;
    public const int ErrorNothingToPlay = -16;
    public const int ErrorUnknownFormat = -17;
    public const int ErrorUnsupported = -18;
}

public readonly record struct OpenWatchState(
    bool PreferHardware,
    HardwareDecodeAttempt Attempt,
    bool FileLoaded,
    bool Settled,
    bool Failed,
    bool HardwareActive,
    string? FailureStatus)
{
    public bool Pending => !Settled && !Failed;

    public static OpenWatchState Begin(bool preferHardware)
        => new(
            preferHardware,
            preferHardware ? HardwareDecodeAttempt.D3D11VA : HardwareDecodeAttempt.None,
            FileLoaded: false,
            Settled: false,
            Failed: false,
            HardwareActive: false,
            FailureStatus: null);
}

public readonly record struct OpenWatchDecision(OpenWatchAction Action, OpenWatchState State);

/// <summary>
/// Decides, from libmpv file-loaded / end-file / hwdec-current, whether the
/// open failed or hardware decode has to step D3D11VA → DXVA2 → software.
/// </summary>
public static class OpenFailureWatch
{
    public static readonly TimeSpan SettleWindow = TimeSpan.FromMilliseconds(800);

    public static string HwdecOption(HardwareDecodeAttempt attempt)
        => attempt switch
        {
            HardwareDecodeAttempt.D3D11VA => "d3d11va",
            HardwareDecodeAttempt.Dxva2 => "dxva2",
            _ => "no"
        };

    public static bool IsHardwareDecoder(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && !value.Equals("no", StringComparison.OrdinalIgnoreCase);

    public static bool IsDefiniteOpenError(int mpvError)
        => mpvError is OpenSignal.ErrorLoadingFailed
            or OpenSignal.ErrorNothingToPlay
            or OpenSignal.ErrorUnknownFormat
            or OpenSignal.ErrorUnsupported;

    public static string FailureStatus(bool httpUrl, int mpvError)
    {
        if (httpUrl && mpvError is 0 or OpenSignal.ErrorLoadingFailed)
        {
            return StatusText.PlaybackFailed();
        }

        var detail = mpvError switch
        {
            OpenSignal.ErrorLoadingFailed => "불러오지 못했습니다",
            OpenSignal.ErrorNothingToPlay => "재생할 내용이 없습니다",
            OpenSignal.ErrorUnknownFormat => "형식을 알 수 없습니다",
            OpenSignal.ErrorUnsupported => "지원하지 않습니다",
            _ => null
        };

        return httpUrl
            ? StatusText.PlaybackFailed(detail)
            : StatusText.PlaybackFailed(detail ?? "파일을 열 수 없습니다.");
    }

    public static OpenWatchDecision Reduce(OpenWatchState state, OpenSignal signal, bool httpUrl)
    {
        if (state.Failed)
        {
            return new OpenWatchDecision(OpenWatchAction.Wait, state);
        }

        return signal.Kind switch
        {
            OpenSignalKind.FileLoaded => new OpenWatchDecision(
                OpenWatchAction.Wait,
                state with { FileLoaded = true }),
            OpenSignalKind.EndFile => ReduceEnd(state, signal, httpUrl),
            OpenSignalKind.HwdecCurrent => ReduceHwdec(state, signal.HwdecCurrent),
            _ => new OpenWatchDecision(OpenWatchAction.Wait, state)
        };
    }

    private static OpenWatchDecision ReduceEnd(OpenWatchState state, OpenSignal signal, bool httpUrl)
    {
        if (signal.EndReason == OpenSignal.EndRedirect)
        {
            return new OpenWatchDecision(OpenWatchAction.Wait, state with { FileLoaded = false });
        }

        if (signal.EndReason != OpenSignal.EndError)
        {
            return new OpenWatchDecision(OpenWatchAction.Wait, state);
        }

        // A load that has not settled yet still walks the hardware chain.
        // Definite container/network errors do not.
        if (state.Settled
            || !state.PreferHardware
            || state.Attempt == HardwareDecodeAttempt.None
            || IsDefiniteOpenError(signal.ErrorCode))
        {
            return Fail(state, httpUrl, signal.ErrorCode);
        }

        return Retry(state);
    }

    private static OpenWatchDecision ReduceHwdec(OpenWatchState state, string? hwdec)
    {
        if (!state.FileLoaded || string.IsNullOrWhiteSpace(hwdec))
        {
            return new OpenWatchDecision(OpenWatchAction.Wait, state);
        }

        if (IsHardwareDecoder(hwdec))
        {
            if (state.Settled && state.HardwareActive)
            {
                return new OpenWatchDecision(OpenWatchAction.Wait, state);
            }

            return Playing(state, OpenWatchAction.PlayingHardware, hardware: true);
        }

        if (state.Settled && !state.HardwareActive)
        {
            return new OpenWatchDecision(OpenWatchAction.Wait, state);
        }

        // "no" after a hardware attempt, including one already marked playing,
        // still walks D3D11VA → DXVA2 → software. The notification is not repeated.
        if (state.PreferHardware && state.Attempt != HardwareDecodeAttempt.None)
        {
            return Retry(state);
        }

        return Playing(state, OpenWatchAction.PlayingSoftware, hardware: false);
    }

    private static OpenWatchDecision Playing(OpenWatchState state, OpenWatchAction action, bool hardware)
        => new(
            action,
            state with
            {
                Settled = true,
                Failed = false,
                HardwareActive = hardware,
                Attempt = hardware ? state.Attempt : HardwareDecodeAttempt.None,
                FailureStatus = null
            });

    private static OpenWatchDecision Retry(OpenWatchState state)
    {
        var next = state.Attempt switch
        {
            HardwareDecodeAttempt.D3D11VA => HardwareDecodeAttempt.Dxva2,
            HardwareDecodeAttempt.Dxva2 => HardwareDecodeAttempt.None,
            _ => HardwareDecodeAttempt.None
        };

        if (next == state.Attempt)
        {
            return Fail(state, httpUrl: false, mpvError: 0);
        }

        return new OpenWatchDecision(
            OpenWatchAction.Retry,
            state with
            {
                Attempt = next,
                FileLoaded = false,
                Settled = false,
                Failed = false,
                HardwareActive = false,
                FailureStatus = null
            });
    }

    private static OpenWatchDecision Fail(OpenWatchState state, bool httpUrl, int mpvError)
        => new(
            OpenWatchAction.Failed,
            state with
            {
                Settled = true,
                Failed = true,
                HardwareActive = false,
                FailureStatus = FailureStatus(httpUrl, mpvError)
            });
}
