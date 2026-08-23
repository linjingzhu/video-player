using System.Globalization;
using System.Windows.Forms;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.App.Playback;

public sealed class MpvMediaEngine : IMediaEngine, IDisposable
{
    private IntPtr _mpv;
    private bool _available;

    public MpvMediaEngine()
    {
        Host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.Black
        };
        TryCreate();
    }

    public Panel Host { get; }
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
        get => ReadDouble("speed");
        set
        {
            if (_available)
            {
                MpvNative.Set(_mpv, "speed", PlaybackSpeed.Clamp(value).ToString("0.###", CultureInfo.InvariantCulture));
            }
        }
    }

    public string? VideoCodec => MpvNative.ReadString(_mpv, "video-codec") ?? MpvNative.ReadString(_mpv, "video-format");
    public string? AudioCodec => MpvNative.ReadString(_mpv, "audio-codec") ?? MpvNative.ReadString(_mpv, "audio-codec-name");
    public bool HardwareActive => !string.Equals(MpvNative.ReadString(_mpv, "hwdec-current"), "no", StringComparison.OrdinalIgnoreCase)
                                  && !string.IsNullOrEmpty(MpvNative.ReadString(_mpv, "hwdec-current"));
    public string? LastError { get; private set; }

    public OpenMediaResult Open(string path, bool preferHardware)
    {
        if (!_available)
        {
            LastError = "libmpv를 찾을 수 없습니다. Windows 빌드에 libmpv-2.dll을 함께 두세요.";
            return new OpenMediaResult { Success = false, Path = path, Error = LastError, Status = LastError };
        }

        if (preferHardware)
        {
            MpvNative.Set(_mpv, "hwdec", "d3d11va");
        }

        var code = MpvNative.Command(_mpv, "loadfile", path, "replace");
        if (code < 0)
        {
            LastError = "파일을 열 수 없습니다.";
            IsOpen = false;
            return new OpenMediaResult { Success = false, Path = path, Error = LastError, Status = LastError };
        }

        IsOpen = true;
        IsPaused = false;
        MpvNative.Set(_mpv, "pause", "no");

        if (preferHardware && !HardwareActive)
        {
            MpvNative.Set(_mpv, "hwdec", "dxva2");
        }

        if (preferHardware && !HardwareActive)
        {
            MpvNative.Set(_mpv, "hwdec", "no");
            LastError = "하드웨어 가속에 실패하여 소프트웨어로 재생합니다.";
        }

        var video = VideoCodec;
        var audio = AudioCodec;
        return new OpenMediaResult
        {
            Success = true,
            Path = path,
            VideoCodec = video,
            AudioCodec = audio,
            HardwareActive = HardwareActive,
            AddedToRecent = true,
            Status = StatusText.Format(HardwareActive ? DecodePath.Hardware : DecodePath.Software, video, audio)
        };
    }

    public void Play()
    {
        if (!_available)
        {
            return;
        }

        MpvNative.Set(_mpv, "pause", "no");
        IsPaused = false;
    }

    public void Pause()
    {
        if (!_available)
        {
            return;
        }

        MpvNative.Set(_mpv, "pause", "yes");
        IsPaused = true;
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

    public void Close()
    {
        if (_available)
        {
            MpvNative.Command(_mpv, "stop");
        }

        IsOpen = false;
        IsPaused = true;
    }

    public void Dispose()
    {
        if (_mpv != IntPtr.Zero)
        {
            MpvNative.mpv_destroy(_mpv);
            _mpv = IntPtr.Zero;
        }

        Host.Dispose();
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
            MpvNative.Option(_mpv, "vo", "gpu");
            MpvNative.Option(_mpv, "keep-open", "yes");
            MpvNative.Option(_mpv, "osc", "no");
            MpvNative.Option(_mpv, "input-default-bindings", "no");
            if (MpvNative.mpv_initialize(_mpv) < 0)
            {
                LastError = "libmpv 초기화에 실패했습니다.";
                return;
            }

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

    private double ReadDouble(string name)
    {
        if (!_available)
        {
            return 0;
        }

        var text = MpvNative.ReadString(_mpv, name);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
}
