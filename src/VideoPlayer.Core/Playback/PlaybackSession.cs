using VideoPlayer.Core.Capture;
using VideoPlayer.Core.Library;
using VideoPlayer.Core.Media;
using VideoPlayer.Core.Safety;
using VideoPlayer.Core.Series;
using VideoPlayer.Core.Shell;
using VideoPlayer.Core.Subtitles;

namespace VideoPlayer.Core.Playback;

public sealed class PlaybackSession
{
    private readonly HardwareDecodePolicy _hw = new();
    private readonly SeriesDrillDown _drill = new();
    private IReadOnlyList<SeriesEpisode> _flatEpisodes = [];
    private DateTimeOffset _lastActivity = DateTimeOffset.UtcNow;
    private bool _endedHandled;
    private bool _capturing;

    public PlaybackSession(IMediaEngine engine, string? dataDirectory = null)
    {
        Engine = engine;
        DataDirectory = dataDirectory ?? Path.Combine(Path.GetTempPath(), "video-player-test");
        Directory.CreateDirectory(DataDirectory);
        LoadPersisted();
        Speed = PlaybackSpeed.Default;
        AutoNext = true;
        RefreshSeriesPanel();
    }

    public IMediaEngine Engine { get; }
    public string DataDirectory { get; }
    public PlayerShell Shell { get; } = PlayerShell.Boot();
    public ResumeStore Resume { get; private set; } = new();
    public RecentStore Recent { get; private set; } = new();
    public RecentSeriesStore RecentSeries { get; private set; } = new();
    public WindowMemory Window { get; private set; } = new();
    public AppSettings Settings { get; private set; } = new();
    public AutoNextOffer AutoNextOffer { get; } = new();
    public int JumpSeconds => Settings.JumpSeconds;
    public MediaIdentity? Current { get; private set; }
    public IReadOnlyList<SubtitleCue> Cues { get; private set; } = [];
    public double Speed { get; private set; }
    public bool AutoNext { get; set; } = true;
    public bool IsCapturing => _capturing;
    public string? LastUnsupportedCodec { get; private set; }
    public SeriesDrillDown Series => _drill;

    public OpenMediaResult Open(string path)
    {
        var check = PathValidator.ValidateLocalFilePath(path);
        if (!check.Success || check.FullPath is null)
        {
            Shell.Status.Fail(check.Error ?? "열 수 없습니다.");
            return new OpenMediaResult { Success = false, Path = path ?? "", Error = check.Error, Status = Shell.Status.Text };
        }

        if (SupportedFormats.IsOutOfScopeContainer(check.FullPath) || !SupportedFormats.IsSupportedContainer(check.FullPath))
        {
            var name = Path.GetExtension(check.FullPath).TrimStart('.').ToUpperInvariant();
            LastUnsupportedCodec = string.IsNullOrEmpty(name) ? "알 수 없음" : name;
            Shell.Status.Fail(StatusText.Unsupported(LastUnsupportedCodec));
            return OpenMediaResult.Unsupported(check.FullPath, LastUnsupportedCodec);
        }

        Checkpoint("episode-change");

        var opened = Engine.Open(check.FullPath, preferHardware: true);
        if (!opened.Success)
        {
            LastUnsupportedCodec = opened.UnsupportedCodecName;
            Shell.Status.Fail(opened.Status);
            return opened with { AddedToRecent = false };
        }

        if (opened.VideoCodec is not null &&
            (SupportedFormats.IsOutOfScopeCodec(opened.VideoCodec) || !SupportedFormats.IsSupportedVideoCodec(opened.VideoCodec)))
        {
            LastUnsupportedCodec = SupportedFormats.DisplayCodecName(opened.VideoCodec);
            Engine.Close();
            Current = null;
            Shell.Status.Fail(StatusText.Unsupported(opened.VideoCodec));
            return OpenMediaResult.Unsupported(check.FullPath, opened.VideoCodec);
        }

        var identity = File.Exists(check.FullPath)
            ? MediaIdentity.FromFile(check.FullPath)
            : new MediaIdentity(check.FullPath, 0);
        Current = identity;
        _endedHandled = false;
        AutoNextOffer.ResetForNewTitle();

        if (!opened.HardwareActive)
        {
            Shell.Status.Fail(_hw.OnHardwareFailed(opened.VideoCodec, opened.AudioCodec).StatusText);
        }
        else
        {
            Shell.Status.Clear();
        }

        var resumeAt = Resume.PositionOrZero(identity.Path, identity.Size);
        if (resumeAt > 0)
        {
            Engine.Seek(resumeAt);
        }

        Engine.Speed = Speed;
        LoadSidecarSubtitles(identity.Path);
        Recent.TryAdd(identity.Path, identity.Size, opened.VideoCodec ?? Engine.VideoCodec, opened.AudioCodec ?? Engine.AudioCodec);
        RefreshSidebar();
        RefreshSeriesPanel();
        SyncTransport();
        Shell.IsPaused = false;
        Persist();
        return opened with
        {
            Success = true,
            Path = identity.Path,
            Size = identity.Size,
            AddedToRecent = true,
            Status = Shell.Status.Text,
            HardwareActive = opened.HardwareActive
        };
    }

    public IReadOnlyList<OpenMediaResult> Drop(IEnumerable<string> paths)
    {
        var results = new List<OpenMediaResult>();
        foreach (var path in paths)
        {
            var check = PathValidator.ValidateLocalFilePath(path);
            if (check.Success && check.FullPath is not null && Directory.Exists(check.FullPath))
            {
                OpenSeriesFolder(check.FullPath);
                results.Add(new OpenMediaResult { Success = true, Path = check.FullPath, Status = Shell.Status.Text });
                continue;
            }

            results.Add(Open(path));
        }

        return results;
    }

    public void PlayPause()
    {
        if (_capturing || !Engine.IsOpen)
        {
            return;
        }

        if (Engine.IsPaused)
        {
            Engine.Play();
            Shell.IsPaused = false;
        }
        else
        {
            Engine.Pause();
            Shell.IsPaused = true;
            Checkpoint("pause");
        }

        UpdateChromeVisibility(DateTimeOffset.UtcNow);
    }

    public void SeekRelative(double seconds)
    {
        if (_capturing || !Engine.IsOpen)
        {
            return;
        }

        Engine.Seek(SeekCommands.ApplyRelative(Engine.Position, Engine.Duration, seconds));
        _endedHandled = false;
        SyncTransport();
        UpdateNextEpisodeChrome(DateTimeOffset.UtcNow);
    }

    public void SkipBack() => SeekRelative(-JumpSeconds);

    public void SkipForward() => SeekRelative(JumpSeconds);

    public int SetJumpSeconds(int seconds)
    {
        var applied = Settings.SetJumpSeconds(seconds);
        Persist();
        return applied;
    }

    public void SeekAbsolute(double seconds)
    {
        if (_capturing || !Engine.IsOpen)
        {
            return;
        }

        Engine.Seek(Math.Max(0, seconds));
        _endedHandled = false;
        SyncTransport();
        UpdateNextEpisodeChrome(DateTimeOffset.UtcNow);
    }

    public void SetSpeed(double speed)
    {
        Speed = PlaybackSpeed.Clamp(speed);
        Engine.Speed = Speed;
        Shell.Transport.Speed = Speed;
    }

    public void AdjustVolume(double delta)
    {
        Engine.Volume = Math.Clamp(Engine.Volume + delta, 0, 1);
        Shell.Transport.Volume = Engine.Volume;
    }

    public void ToggleCaptions()
    {
        Shell.Transport.CaptionsOn = !Shell.Transport.CaptionsOn;
        if (!Shell.Transport.CaptionsOn)
        {
            Shell.OverlaySubtitle = "";
        }
    }

    public void Checkpoint(string reason)
    {
        if (Current is not { } current || !Engine.IsOpen)
        {
            return;
        }

        var result = CompletionPolicy.Checkpoint(current, Engine.Position, Engine.Duration);
        Resume.Apply(result);
        Persist();
        RefreshSidebar();
        RefreshSeriesPanel();
        _ = reason;
    }

    public void RememberWindow(WindowBounds bounds)
    {
        Window.Remember(bounds);
        Persist();
    }

    public SeriesShow OpenSeriesFolder(string folder)
    {
        var show = SeriesScanner.Scan(folder);
        _flatEpisodes = show.Seasons.SelectMany(s => s.Episodes).ToList();
        _drill.AddOrUpdate(show);
        _drill.ReplaceShows(MergeShows(show));
        RecentSeries.Add(show.RootPath, show.Name);
        RefreshSidebar();
        RefreshSeriesPanel();
        Persist();
        return show;
    }

    public void DrillInto(SeriesListItem item)
    {
        if (item.Kind == "show")
        {
            var show = _drill.Shows.FirstOrDefault(s => string.Equals(s.RootPath, item.Path, PathValidator.PathComparison));
            if (show is not null)
            {
                _flatEpisodes = show.Seasons.SelectMany(s => s.Episodes).ToList();
                _drill.OpenShow(show);
            }
        }
        else if (item.Kind == "season" && _drill.Show is { } currentShow)
        {
            var season = currentShow.Seasons.FirstOrDefault(s => string.Equals(s.FolderPath, item.Path, PathValidator.PathComparison));
            if (season is not null)
            {
                _drill.OpenSeason(season);
            }
        }
        else if (item.Kind.StartsWith("episode", StringComparison.Ordinal) && item.Path is not null)
        {
            Open(item.Path);
            Shell.Screen = ShellScreen.Main;
        }

        RefreshSeriesPanel();
    }

    public void SeriesBack()
    {
        _drill.Back();
        RefreshSeriesPanel();
    }

    public void PlayNextEpisode() => PlayAdjacentEpisode(+1);

    public void PlayPreviousEpisode() => PlayAdjacentEpisode(-1);

    public void CancelAutoNext() => AutoNextOffer.Cancel();

    public void Tick(DateTimeOffset now)
    {
        SyncTransport();
        if (_capturing)
        {
            return;
        }

        if (Shell.Transport.CaptionsOn && Cues.Count > 0)
        {
            var text = SubtitleParser.CueAt(Cues, TimeSpan.FromSeconds(Engine.Position));
            Shell.OverlaySubtitle = string.IsNullOrEmpty(text) ? "" : text;
        }

        UpdateChromeVisibility(now);
        UpdateNextEpisodeChrome(now);

        if (Engine.IsOpen && Engine.Duration > 0 && Engine.Position >= Engine.Duration - 0.25)
        {
            if (!_endedHandled)
            {
                _endedHandled = true;
                Checkpoint("ended");
                if (AutoNext && Current is { } current)
                {
                    var next = SeriesScanner.NextEpisode(_flatEpisodes, current.Path);
                    if (next is { } identity)
                    {
                        AutoNextOffer.Begin(identity.Path, now);
                    }
                }
            }

            if (AutoNextOffer.ShouldAdvance(now) && AutoNextOffer.NextPath is { } nextPath)
            {
                Open(nextPath);
            }
        }
    }

    public void EnterFullscreen()
    {
        Shell.EnterFullscreen();
        Shell.Fullscreen.Title = Current is { } cur
            ? FileNameSanitizer.ForDisplay(Path.GetFileNameWithoutExtension(cur.Path))
            : UiCopy.AppTitle;
    }

    public void ExitFullscreen() => Shell.ExitFullscreen();

    public void ContinueWatching()
    {
        if (Resume.Continue is { } pointer)
        {
            Open(pointer.Path);
        }
    }

    public void ToggleSidebar() => Shell.Sidebar.Open = !Shell.Sidebar.Open;

    public void OpenCaptureSheet()
    {
        Shell.Capture.FolderPath = StillFrameCapture.ResolveFolder(
            string.IsNullOrWhiteSpace(Shell.Capture.FolderPath)
                ? Settings.CaptureFolder
                : Shell.Capture.FolderPath);
        Shell.Capture.Open = true;
    }

    public void CloseCaptureSheet() => Shell.Capture.Open = false;

    public void NudgeCaptureCount(int delta) => Shell.Capture.NudgeCount(delta);

    public void NudgeCaptureInterval(int delta) => Shell.Capture.NudgeInterval(delta);

    public void SetCaptureFormat(CaptureFormat format) => Shell.Capture.Format = format;

    public bool SetCaptureFolder(string path)
    {
        var check = PathValidator.ValidateLocalFilePath(path);
        if (!check.Success || check.FullPath is null)
        {
            return false;
        }

        RememberCaptureFolder(check.FullPath);
        return true;
    }

    public CaptureRunResult RunStillCapture()
    {
        if (Current is not { } current || !Engine.IsOpen)
        {
            var missing = new CaptureRunResult(
                StillFrameCapture.ClampCount(Shell.Capture.Count),
                0,
                false,
                Engine.IsPaused,
                false,
                CaptureBannerKind.Failure,
                UiCopy.CaptureNoMedia,
                []);
            ApplyCaptureBanner(missing);
            return missing;
        }

        _capturing = true;
        try
        {
            Engine.Pause();
            Shell.IsPaused = true;
            var result = StillFrameCapture.Run(
                Engine,
                new CaptureJob(
                    Path.GetFileNameWithoutExtension(current.Path),
                    Shell.Capture.FolderPath,
                    Shell.Capture.Count,
                    Shell.Capture.IntervalFrames,
                    Shell.Capture.Format));
            ApplyCaptureBanner(result);
            if (result.Saved > 0)
            {
                RememberCaptureFolder(Shell.Capture.FolderPath);
            }

            Shell.Capture.Open = false;
            SyncTransport();
            return result;
        }
        finally
        {
            _capturing = false;
        }
    }

    public void DismissCaptureBanner() => Shell.CaptureBanner.Clear();

    public void UpdateChromeVisibility(DateTimeOffset now)
        => Shell.ChromeVisible = FullscreenChromeController.ShouldShow(Shell.Screen == ShellScreen.Fullscreen, Shell.IsPaused, now, _lastActivity);

    public void NoteActivity(DateTimeOffset now) => _lastActivity = now;

    private void UpdateNextEpisodeChrome(DateTimeOffset now)
    {
        var inEndRegion = Engine.IsOpen
                          && CompletionPolicy.IsInLastTenSeconds(Engine.Position, Engine.Duration)
                          && NextEpisodePath() is not null;
        Shell.NextEpisode.ShowCta = inEndRegion || AutoNextOffer.Pending;
        Shell.NextEpisode.AutoNextPending = AutoNextOffer.Pending;
        Shell.NextEpisode.Label = AutoNextOffer.Pending
            ? $"{UiCopy.NextEpisode} ({Math.Ceiling(AutoNextOffer.Remaining(now).TotalSeconds):0})"
            : UiCopy.NextEpisodeCta;
    }

    private void PlayAdjacentEpisode(int offset)
    {
        AutoNextOffer.ResetForNewTitle();
        if (_capturing || Current is not { } current)
        {
            return;
        }

        var identity = offset < 0
            ? SeriesScanner.PreviousEpisode(_flatEpisodes, current.Path)
            : SeriesScanner.NextEpisode(_flatEpisodes, current.Path);
        if (identity is { } next)
        {
            Open(next.Path);
        }
    }

    private string? NextEpisodePath()
    {
        if (Current is not { } current)
        {
            return null;
        }

        return SeriesScanner.NextEpisode(_flatEpisodes, current.Path)?.Path;
    }

    private string? PreviousEpisodePath()
    {
        if (Current is not { } current)
        {
            return null;
        }

        return SeriesScanner.PreviousEpisode(_flatEpisodes, current.Path)?.Path;
    }

    private void LoadSidecarSubtitles(string mediaPath)
    {
        Cues = [];
        foreach (var sidecar in SubtitleLocator.FindSidecars(mediaPath))
        {
            var parsed = SubtitleParser.ParseFile(sidecar);
            if (parsed.Cues.Count > 0)
            {
                Cues = parsed.Cues;
                break;
            }
        }
    }

    private void SyncTransport()
    {
        Shell.Transport.Position = Engine.Position;
        Shell.Transport.Duration = Engine.Duration;
        Shell.Transport.Volume = Engine.Volume;
        Shell.Transport.Speed = Speed;
        Shell.Transport.HasPrevious = PreviousEpisodePath() is not null;
        Shell.Transport.HasNext = NextEpisodePath() is not null;
        Shell.OverlayTime = $"{Shell.Transport.PositionText} / {Shell.Transport.DurationText}";
        Shell.IsPaused = Engine.IsPaused;
    }

    private void RefreshSidebar()
    {
        Shell.Sidebar.Resume = Resume.Continue is { } pointer
            ? new SidebarResumeItem(FileNameSanitizer.ForDisplay(Path.GetFileName(pointer.Path)), pointer.Path, pointer.Size)
            : null;
        Shell.Sidebar.RecentSeries.Clear();
        foreach (var series in RecentSeries.Items)
        {
            Shell.Sidebar.RecentSeries.Add(new SidebarSeriesItem(series.Title, series.FolderPath));
        }
    }

    private void ApplyCaptureBanner(CaptureRunResult result)
        => Shell.CaptureBanner.Show(result.BannerKind, result.Banner);

    private void RefreshSeriesPanel()
    {
        Shell.Series.Level = _drill.Level;
        Shell.Series.Heading = _drill.Heading();
        Shell.Series.Items = [.. _drill.ListItems(Resume, Current?.Path)];
    }

    private IEnumerable<SeriesShow> MergeShows(SeriesShow incoming)
    {
        var map = _drill.Shows.ToDictionary(s => s.RootPath, StringComparer.OrdinalIgnoreCase);
        map[incoming.RootPath] = incoming;
        return map.Values;
    }

    private void LoadPersisted()
    {
        Resume = ResumeStore.FromJson(ReadOptional("resume.json"));
        Recent = RecentStore.FromJson(ReadOptional("recent.json"));
        RecentSeries = RecentSeriesStore.FromJson(ReadOptional("recent-series.json"));
        Window = WindowMemory.FromJson(ReadOptional("window.json"));
        Settings = AppSettings.FromJson(ReadOptional(AppSettings.FileName));
        Shell.Capture.FolderPath = StillFrameCapture.ResolveFolder(Settings.CaptureFolder);
        RefreshSidebar();
    }

    private void RememberCaptureFolder(string path)
    {
        var check = PathValidator.ValidateLocalFilePath(path);
        if (!check.Success || check.FullPath is null)
        {
            return;
        }

        Shell.Capture.FolderPath = check.FullPath;
        Settings.SetCaptureFolder(check.FullPath);
        Persist();
    }

    private void Persist()
    {
        WriteOptional("resume.json", Resume.ToJson());
        WriteOptional("recent.json", Recent.ToJson());
        WriteOptional("recent-series.json", RecentSeries.ToJson());
        WriteOptional("window.json", Window.ToJson());
        WriteOptional(AppSettings.FileName, Settings.ToJson());
    }

    private string? ReadOptional(string name)
    {
        var path = Path.Combine(DataDirectory, name);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private void WriteOptional(string name, string json)
        => File.WriteAllText(Path.Combine(DataDirectory, name), json);
}

public static class FullscreenChromeController
{
    public static readonly TimeSpan IdleHide = TimeSpan.FromSeconds(3);

    public static bool ShouldShow(bool fullscreen, bool paused, DateTimeOffset now, DateTimeOffset lastActivity)
    {
        if (!fullscreen)
        {
            return true;
        }

        if (paused)
        {
            return true;
        }

        return now - lastActivity < IdleHide;
    }
}
