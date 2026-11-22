using System.Globalization;
using System.IO;
using System.Windows.Forms;
using VideoPlayer.Core.Playback;
using Path = System.IO.Path;

namespace VideoPlayer.App.Playback;

public sealed class MpvMediaEngine : IMediaEngine, IDisposable
{
    private const ulong HwdecReply = 1;
    private const string SoftwareFallbackError = "하드웨어 가속에 실패하여 소프트웨어로 재생합니다.";

    private readonly SynchronizationContext? _ui = SynchronizationContext.Current;
    private readonly MpvNative.WakeupCallback _wakeup;
    private IntPtr _mpv;
    private bool _available;
    private bool _wakeupSet;
    private OpenWatchState _watch = OpenWatchState.Begin(true);
    private string? _openPath;
    private bool _httpOpen;
    private bool _preferHardware = true;
    private bool _opening;
    private int _watchId;
    private bool _dispatching;
    private bool _dispatchAgain;
    private bool _restoreAfterRetry;
    private double _restorePosition;
    private double _desiredSpeed = PlaybackSpeed.Default;
    private bool _wantPause = true;

    public MpvMediaEngine()
    {
        _wakeup = OnWakeup;
        Host = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.Black
        };
        TryCreate();
    }

    public System.Windows.Forms.Panel Host { get; }
    public bool IsOpen { get; private set; }
    public bool IsPaused { get; private set; } = true;
    public double Position => ReadDouble("time-pos");
    public double Duration => ReadDouble("duration");
    public double Volume
    {
        get => ReadDouble("volume") / 100d;
        set
        {
            if (_available)
            {
                MpvNative.Set(_mpv, "volume", (Math.Clamp(value, 0, 1) * 100d).ToString("0.###", CultureInfo.InvariantCulture));
            }
        }
    }

    public double Speed
    {
        get => _available ? ReadDouble("speed") : _desiredSpeed;
        set
        {
            _desiredSpeed = PlaybackSpeed.Clamp(value);
            if (_available)
            {
                MpvNative.Set(_mpv, "speed", _desiredSpeed.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }
    }

    public string? VideoCodec => MpvNative.ReadString(_mpv, "video-codec") ?? MpvNative.ReadString(_mpv, "video-format");
    public string? AudioCodec => MpvNative.ReadString(_mpv, "audio-codec") ?? MpvNative.ReadString(_mpv, "audio-codec-name");
    public bool HardwareActive => !string.Equals(MpvNative.ReadString(_mpv, "hwdec-current"), "no", StringComparison.OrdinalIgnoreCase)
                                  && !string.IsNullOrEmpty(MpvNative.ReadString(_mpv, "hwdec-current"));
    public string? LastError { get; private set; }
    public event EventHandler<PlaybackFault>? PlaybackFaulted;
    public IReadOnlyList<MediaChapter> Chapters => ReadChapters();
    public IReadOnlyList<MediaSubtitleTrack> SubtitleTracks => ReadSubtitleTracks();
    public HdrMode HdrMode { get; private set; } = HdrPassThrough.Default;

    public OpenMediaResult Open(string path, bool preferHardware)
    {
        if (!_available)
        {
            LastError = "libmpv를 찾을 수 없습니다. Windows 빌드에 libmpv-2.dll을 함께 두세요.";
            return new OpenMediaResult { Success = false, Path = path, Error = LastError, Status = LastError };
        }

        // hwdec-current is empty until the decoder starts, so a read right
        // after loadfile is not a hardware failure.
        _watchId++;
        var watchId = _watchId;
        _openPath = path;
        _httpOpen = OpenUrlRules.IsAcceptedHttpUrl(path);
        _preferHardware = preferHardware;
        _watch = OpenWatchState.Begin(preferHardware);
        _restoreAfterRetry = false;
        if (preferHardware)
        {
            MpvNative.Set(_mpv, "hwdec", OpenFailureWatch.HwdecOption(_watch.Attempt));
        }

        var code = MpvNative.Command(_mpv, "loadfile", path, "replace");
        if (code < 0)
        {
            LastError = _httpOpen ? StatusText.PlaybackFailed() : "파일을 열 수 없습니다.";
            IsOpen = false;
            IsPaused = true;
            _watch = _watch with { Settled = true, Failed = true, FailureStatus = LastError };
            return new OpenMediaResult { Success = false, Path = path, Error = LastError, Status = LastError };
        }

        IsOpen = true;
        IsPaused = false;
        _wantPause = false;
        MpvNative.Set(_mpv, "pause", "no");

        _opening = true;
        _dispatching = true;
        try
        {
            var deadline = DateTime.UtcNow + OpenFailureWatch.SettleWindow;
            while (_watch.Pending && watchId == _watchId && DateTime.UtcNow < deadline)
            {
                var ev = MpvNative.WaitEvent(_mpv, 0.05);
                if (watchId != _watchId)
                {
                    break;
                }

                if (ev.Id == MpvNative.EventNone)
                {
                    continue;
                }

                Dispatch(ev);
            }
        }
        finally
        {
            _dispatching = false;
            _opening = false;
            if (_dispatchAgain && watchId == _watchId)
            {
                DrainEvents();
            }
        }

        return BuildOpenResult(path);
    }

    public void Play()
    {
        if (!_available)
        {
            return;
        }

        MpvNative.Set(_mpv, "pause", "no");
        IsPaused = false;
        _wantPause = false;
    }

    public void Pause()
    {
        if (!_available)
        {
            return;
        }

        MpvNative.Set(_mpv, "pause", "yes");
        IsPaused = true;
        _wantPause = true;
    }

    public void Stop()
    {
        Pause();
        if (IsOpen)
        {
            Seek(0);
        }
    }

    public void Seek(double seconds)
    {
        if (!_available)
        {
            return;
        }

        MpvNative.Command(_mpv, "seek", seconds.ToString("0.###", CultureInfo.InvariantCulture), "absolute");
    }

    public void FrameStep(int direction)
    {
        if (!_available)
        {
            return;
        }

        MpvNative.Command(_mpv, direction >= 0 ? "frame-step" : "frame-back-step");
        IsPaused = true;
    }

    public bool ScreenshotToFile(string path)
    {
        if (!_available || !IsOpen || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        if (extension is "jpg" or "jpeg")
        {
            MpvNative.Set(_mpv, "screenshot-format", "jpg");
            MpvNative.Set(_mpv, "screenshot-jpeg-quality", "90");
        }
        else if (extension == "webp")
        {
            MpvNative.Set(_mpv, "screenshot-format", "webp");
            MpvNative.Set(_mpv, "screenshot-webp-quality", "80");
            MpvNative.Set(_mpv, "screenshot-webp-lossless", "no");
        }
        else
        {
            MpvNative.Set(_mpv, "screenshot-format", "png");
        }

        var code = MpvNative.Command(_mpv, "screenshot-to-file", path, "video");
        return code >= 0 && File.Exists(path);
    }

    public void SetFitMode(string mode)
    {
        if (!_available)
        {
            return;
        }

        if (mode == "cover")
        {
            MpvNative.Set(_mpv, "keepaspect", "no");
            MpvNative.Set(_mpv, "panscan", "1.0");
        }
        else
        {
            MpvNative.Set(_mpv, "keepaspect", "yes");
            MpvNative.Set(_mpv, "panscan", "0.0");
        }
    }

    public void SetHdrMode(HdrMode mode)
    {
        HdrMode = HdrPassThrough.Clamp(mode);
        if (!_available)
        {
            return;
        }

        ApplyHdrOptions(HdrMode, beforeInitialize: false);
    }

    public void Close()
    {
        _watchId++;
        _restoreAfterRetry = false;
        _watch = _watch with { Settled = true };
        if (_available)
        {
            MpvNative.Command(_mpv, "stop");
        }

        IsOpen = false;
        IsPaused = true;
        _wantPause = true;
    }

    public void Dispose()
    {
        _watchId++;
        _available = false;
        if (_mpv != IntPtr.Zero)
        {
            if (_wakeupSet)
            {
                MpvNative.SetWakeupCallback(_mpv, null);
                _wakeupSet = false;
            }

            MpvNative.mpv_destroy(_mpv);
            _mpv = IntPtr.Zero;
        }

        Host.Dispose();
    }

    private OpenMediaResult BuildOpenResult(string path)
    {
        if (_watch.Failed)
        {
            var status = string.IsNullOrWhiteSpace(_watch.FailureStatus)
                ? (_httpOpen ? StatusText.PlaybackFailed() : "파일을 열 수 없습니다.")
                : _watch.FailureStatus;
            LastError = status;
            IsOpen = false;
            IsPaused = true;
            _wantPause = true;
            return new OpenMediaResult { Success = false, Path = path, Error = status, Status = status };
        }

        var hardware = _watch.HardwareActive || (_watch.Pending && _preferHardware);
        var video = VideoCodec;
        var audio = AudioCodec;
        if (!hardware && _preferHardware && !_watch.Pending)
        {
            LastError = SoftwareFallbackError;
        }

        return new OpenMediaResult
        {
            Success = true,
            Path = path,
            VideoCodec = video,
            AudioCodec = audio,
            HardwareActive = hardware,
            AddedToRecent = true,
            Status = StatusText.Format(hardware ? DecodePath.Hardware : DecodePath.Software, video, audio)
        };
    }

    private void OnWakeup(IntPtr _)
    {
        if (_ui is not null)
        {
            try
            {
                _ui.Post(static state => ((MpvMediaEngine)state!).DrainEvents(), this);
            }
            catch (InvalidOperationException)
            {
            }

            return;
        }

        try
        {
            if (Host.IsHandleCreated)
            {
                Host.BeginInvoke((Action)DrainEvents);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void DrainEvents()
    {
        if (_dispatching || !_available)
        {
            _dispatchAgain = true;
            return;
        }

        _dispatching = true;
        try
        {
            do
            {
                _dispatchAgain = false;
                while (_available)
                {
                    var ev = MpvNative.WaitEvent(_mpv, 0);
                    if (ev.Id == MpvNative.EventNone)
                    {
                        break;
                    }

                    Dispatch(ev);
                }
            }
            while (_dispatchAgain && _available);
        }
        catch (Exception ex)
        {
            // A malformed libmpv event must not take down the UI thread.
            LastError = ex.Message;
        }
        finally
        {
            _dispatching = false;
        }
    }

    private void Dispatch(MpvNative.MpvEventView ev)
    {
        switch (ev.Id)
        {
            case MpvNative.EventFileLoaded:
                ReapplyAfterRetry();
                Apply(new OpenSignal(OpenSignalKind.FileLoaded));
                break;
            case MpvNative.EventPlaybackRestart when _watch.FileLoaded && !_watch.Failed:
                Apply(new OpenSignal(OpenSignalKind.HwdecCurrent, HwdecCurrent: MpvNative.ReadString(_mpv, "hwdec-current")));
                break;
            case MpvNative.EventPropertyChange:
                var change = MpvNative.ReadPropertyChange(ev.Data);
                if (string.Equals(change.Name, "hwdec-current", StringComparison.Ordinal))
                {
                    Apply(new OpenSignal(OpenSignalKind.HwdecCurrent, HwdecCurrent: change.Value));
                }

                break;
            case MpvNative.EventEndFile:
                var end = MpvNative.ReadEndFile(ev.Data);
                Apply(new OpenSignal(OpenSignalKind.EndFile, end.Reason, end.Error));
                break;
        }
    }

    private void Apply(OpenSignal signal)
    {
        if (_watch.Failed)
        {
            return;
        }

        var decision = OpenFailureWatch.Reduce(_watch, signal, _httpOpen, allowHardwareRetry: _opening);
        _watch = decision.State;
        switch (decision.Action)
        {
            case OpenWatchAction.Retry:
                Reload(decision.State.Attempt);
                break;
            case OpenWatchAction.Failed:
                IsOpen = false;
                IsPaused = true;
                _wantPause = true;
                LastError = decision.State.FailureStatus;
                if (!_opening)
                {
                    PlaybackFaulted?.Invoke(this, new PlaybackFault(
                        Fatal: true,
                        HardwareActive: false,
                        Status: decision.State.FailureStatus,
                        VideoCodec: VideoCodec,
                        AudioCodec: AudioCodec));
                }

                break;
            case OpenWatchAction.PlayingSoftware when _preferHardware && !_opening:
                LastError = SoftwareFallbackError;
                PlaybackFaulted?.Invoke(this, new PlaybackFault(
                    Fatal: false,
                    HardwareActive: false,
                    Status: null,
                    VideoCodec: VideoCodec,
                    AudioCodec: AudioCodec));
                break;
        }
    }

    private void Reload(HardwareDecodeAttempt attempt)
    {
        if (!_opening && IsOpen)
        {
            _restorePosition = Position;
            _restoreAfterRetry = true;
        }

        MpvNative.Set(_mpv, "hwdec", OpenFailureWatch.HwdecOption(attempt));
        if (string.IsNullOrWhiteSpace(_openPath))
        {
            return;
        }

        var code = MpvNative.Command(_mpv, "loadfile", _openPath, "replace");
        if (code >= 0)
        {
            return;
        }

        var status = _httpOpen
            ? StatusText.PlaybackFailed()
            : StatusText.PlaybackFailed("파일을 열 수 없습니다.");
        _watch = _watch with { Settled = true, Failed = true, HardwareActive = false, FailureStatus = status };
        IsOpen = false;
        IsPaused = true;
        _wantPause = true;
        LastError = status;
        if (!_opening)
        {
            PlaybackFaulted?.Invoke(this, new PlaybackFault(true, false, status, VideoCodec, AudioCodec));
        }
    }

    private void ReapplyAfterRetry()
    {
        if (!_restoreAfterRetry || !_available)
        {
            return;
        }

        _restoreAfterRetry = false;
        MpvNative.Set(_mpv, "speed", _desiredSpeed.ToString("0.###", CultureInfo.InvariantCulture));
        MpvNative.Set(_mpv, "pause", _wantPause ? "yes" : "no");
        if (_restorePosition > 0)
        {
            MpvNative.Command(
                _mpv,
                "seek",
                _restorePosition.ToString("0.###", CultureInfo.InvariantCulture),
                "absolute");
        }
    }

    private void ApplyHdrOptions(HdrMode mode, bool beforeInitialize)
    {
        foreach (var option in HdrPassThrough.RuntimeOptions(mode))
        {
            if (beforeInitialize)
            {
                MpvNative.Option(_mpv, option.Name, option.Value);
            }
            else
            {
                MpvNative.Set(_mpv, option.Name, option.Value);
            }
        }
    }

    private void TryCreate()
    {
        try
        {
            _mpv = MpvNative.mpv_create();
            if (_mpv == IntPtr.Zero)
            {
                LastError = "libmpv 초기화에 실패했습니다.";
                return;
            }

            Host.HandleCreated += (_, _) =>
            {
                MpvNative.Option(_mpv, "wid", Host.Handle.ToInt64().ToString(CultureInfo.InvariantCulture));
            };

            MpvNative.Option(_mpv, "hwdec", "d3d11va");
            MpvNative.Option(_mpv, "hwdec-extra-frames", "0");
            MpvNative.Option(_mpv, "gpu-api", HdrPassThrough.GpuApi);
            MpvNative.Option(_mpv, "vo", HdrPassThrough.Vo);
            ApplyHdrOptions(HdrMode.Auto, beforeInitialize: true);
            MpvNative.Option(_mpv, "keep-open", "yes");
            MpvNative.Option(_mpv, "osc", "no");
            MpvNative.Option(_mpv, "input-default-bindings", "no");
            if (MpvNative.mpv_initialize(_mpv) < 0)
            {
                LastError = "libmpv 초기화에 실패했습니다.";
                return;
            }

            MpvNative.Observe(_mpv, HwdecReply, "hwdec-current", MpvNative.StringFormat);
            MpvNative.SetWakeupCallback(_mpv, _wakeup);
            _wakeupSet = true;
            _available = true;
        }
        catch (DllNotFoundException)
        {
            LastError = "libmpv-2.dll이 없습니다.";
        }
        catch (BadImageFormatException)
        {
            LastError = "libmpv-2.dll 아키텍처가 맞지 않습니다.";
        }
    }

    private IReadOnlyList<MediaSubtitleTrack> ReadSubtitleTracks()
    {
        if (!_available || !IsOpen)
        {
            return [];
        }

        var countText = MpvNative.ReadString(_mpv, "track-list/count");
        if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count <= 0)
        {
            return [];
        }

        var tracks = new List<MediaSubtitleTrack>();
        for (var i = 0; i < count; i++)
        {
            var type = MpvNative.ReadString(_mpv, $"track-list/{i}/type");
            if (!string.Equals(type, "sub", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var idText = MpvNative.ReadString(_mpv, $"track-list/{i}/id");
            _ = int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id);
            var external = string.Equals(MpvNative.ReadString(_mpv, $"track-list/{i}/external"), "yes", StringComparison.OrdinalIgnoreCase);
            tracks.Add(new MediaSubtitleTrack(
                id,
                MpvNative.ReadString(_mpv, $"track-list/{i}/lang"),
                MpvNative.ReadString(_mpv, $"track-list/{i}/title"),
                Embedded: !external));
        }

        return tracks;
    }

    private double ReadDouble(string name)
    {
        if (!_available)
        {
            return 0;
        }

        var text = MpvNative.ReadString(_mpv, name);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private IReadOnlyList<MediaChapter> ReadChapters()
    {
        if (!_available || !IsOpen)
        {
            return [];
        }

        var countText = MpvNative.ReadString(_mpv, "chapter-list/count");
        if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count <= 0)
        {
            return [];
        }

        var chapters = new List<MediaChapter>(count);
        var duration = Duration;
        for (var i = 0; i < count; i++)
        {
            var title = MpvNative.ReadString(_mpv, $"chapter-list/{i}/title") ?? "";
            var start = ReadDouble($"chapter-list/{i}/time");
            var end = i + 1 < count ? ReadDouble($"chapter-list/{i + 1}/time") : duration;
            chapters.Add(new MediaChapter(title, start, end));
        }

        return chapters;
    }
}
