namespace VideoPlayer.Core.Playback;

public interface IMediaEngine
{
    bool IsOpen { get; }
    bool IsPaused { get; }
    double Position { get; }
    double Duration { get; }
    double Volume { get; set; }
    double Speed { get; set; }
    string? VideoCodec { get; }
    string? AudioCodec { get; }
    bool HardwareActive { get; }
    string? LastError { get; }

    OpenMediaResult Open(string path, bool preferHardware);
    void Play();
    void Pause();
    void Seek(double seconds);
    void FrameStep(int direction);
    void SetFitMode(string mode);
    void Close();
}

public sealed class FakeMediaEngine : IMediaEngine
{
    public bool IsOpen { get; private set; }
    public bool IsPaused { get; private set; } = true;
    public double Position { get; private set; }
    public double Duration { get; set; } = 100;
    public double Volume { get; set; } = 1;
    public double Speed { get; set; } = 1;
    public string? VideoCodec { get; set; } = "h264";
    public string? AudioCodec { get; set; } = "aac";
    public bool HardwareActive { get; set; } = true;
    public bool FailHardware { get; set; }
    public bool FailOpen { get; set; }
    public string? ForcedUnsupportedCodec { get; set; }
    public string? LastError { get; private set; }

    public OpenMediaResult Open(string path, bool preferHardware)
    {
        if (FailOpen)
        {
            LastError = "열 수 없습니다.";
            IsOpen = false;
            return new OpenMediaResult
            {
                Success = false,
                Path = path,
                Error = LastError,
                Status = LastError
            };
        }

        if (ForcedUnsupportedCodec is not null)
        {
            IsOpen = false;
            return OpenMediaResult.Unsupported(path, ForcedUnsupportedCodec);
        }

        IsOpen = true;
        IsPaused = false;
        Position = 0;
        HardwareActive = preferHardware && !FailHardware;
        LastError = HardwareActive || !preferHardware ? null : "하드웨어 가속에 실패하여 소프트웨어로 재생합니다.";
        return new OpenMediaResult
        {
            Success = true,
            Path = path,
            VideoCodec = VideoCodec,
            AudioCodec = AudioCodec,
            HardwareActive = HardwareActive,
            AddedToRecent = true,
            Status = StatusText.Format(
                HardwareActive ? DecodePath.Hardware : DecodePath.Software,
                VideoCodec,
                AudioCodec)
        };
    }

    public void Play() => IsPaused = false;

    public void Pause() => IsPaused = true;

    public void Seek(double seconds) => Position = Math.Max(0, seconds);

    public void FrameStep(int direction)
    {
        IsPaused = true;
        Position = Math.Max(0, Position + (direction >= 0 ? 1d / 24d : -1d / 24d));
    }

    public void SetFitMode(string mode) => _ = mode;

    public void Close()
    {
        IsOpen = false;
        IsPaused = true;
    }
}
