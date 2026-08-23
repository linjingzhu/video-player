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
    private SeriesShow? _series;
    private IReadOnlyList<SeriesEpisode> _flatEpisodes = [];

    public PlaybackSession(IMediaEngine engine, string? dataDirectory = null)
    {
        Engine = engine;
        DataDirectory = dataDirectory ?? Path.Combine(Path.GetTempPath(), "video-player-test");
        Directory.CreateDirectory(DataDirectory);
        LoadPersisted();
        Speed = PlaybackSpeed.Default;
    }

    public IMediaEngine Engine { get; }
    public string DataDirectory { get; }
    public PlayerShell Shell { get; } = PlayerShell.Boot();
    public ResumeStore Resume { get; private set; } = new();
    public RecentStore Recent { get; private set; } = new();
    public PlaylistStore Playlist { get; } = new();
    public WindowMemory Window { get; private set; } = new();
    public AppSettings Settings { get; private set; } = new();
    public int JumpSeconds => Settings.JumpSeconds;
    public MediaIdentity? Current { get; private set; }
    public IReadOnlyList<SubtitleCue> Cues { get; private set; } = [];
    public double Speed { get; private set; }
    public string FitMode { get; private set; } = "contain";
    public bool AutoNext { get; set; } = true;
    public string? LastUnsupportedCodec { get; private set; }

    public OpenMediaResult Open(string path)
    {
        var check = PathValidator.ValidateLocalFilePath(path);
        if (!check.Success || check.FullPath is null)
        {
            Shell.Status.Text = check.Error ?? "열 수 없습니다.";
            return new OpenMediaResult { Success = false, Path = path ?? "", Error = check.Error, Status = Shell.Status.Text };
        }

        if (SupportedFormats.IsOutOfScopeContainer(check.FullPath) || !SupportedFormats.IsSupportedContainer(check.FullPath))
        {
            var name = Path.GetExtension(check.FullPath).TrimStart('.').ToUpperInvariant();
            LastUnsupportedCodec = string.IsNullOrEmpty(name) ? "알 수 없음" : name;
            Shell.Status.Text = StatusText.Unsupported(LastUnsupportedCodec);
            return OpenMediaResult.Unsupported(check.FullPath, LastUnsupportedCodec);
        }

        Checkpoint("episode-change");

        var opened = Engine.Open(check.FullPath, preferHardware: true);
        if (!opened.Success)
        {
            LastUnsupportedCodec = opened.UnsupportedCodecName;
            Shell.Status.Text = opened.Status;
            return opened with { AddedToRecent = false };
        }

        if (opened.VideoCodec is not null &&
            (SupportedFormats.IsOutOfScopeCodec(opened.VideoCodec) || !SupportedFormats.IsSupportedVideoCodec(opened.VideoCodec)))
        {
            LastUnsupportedCodec = SupportedFormats.DisplayCodecName(opened.VideoCodec);
            Engine.Close();
            Current = null;
            Shell.Status.Text = StatusText.Unsupported(opened.VideoCodec);
            return OpenMediaResult.Unsupported(check.FullPath, opened.VideoCodec);
        }

        var identity = File.Exists(check.FullPath)
            ? MediaIdentity.FromFile(check.FullPath)
            : new MediaIdentity(check.FullPath, 0);
        Current = identity;

        if (!opened.HardwareActive)
        {
            var fallback = _hw.OnHardwareFailed(opened.VideoCodec, opened.AudioCodec);
            Shell.Status.Text = fallback.StatusText;
        }
        else
        {
            Shell.Status.Text = StatusText.Format(DecodePath.Hardware, opened.VideoCodec, opened.AudioCodec);
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
        if (!Engine.IsOpen)
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
        if (!Engine.IsOpen)
        {
            return;
        }

        Engine.Seek(SeekCommands.ApplyRelative(Engine.Position, Engine.Duration, seconds));
        SyncTransport();
    }

    public void SkipBack() => SeekRelative(-JumpSeconds);

    public void SkipForward() => SeekRelative(JumpSeconds);

    /// <summary>v1.5 live apply. Persists globally; next skip uses the new value.</summary>
    public int SetJumpSeconds(int seconds)
    {
        var applied = Settings.SetJumpSeconds(seconds);
        Persist();
        return applied;
    }

    public void SeekAbsolute(double seconds)
    {
        if (!Engine.IsOpen)
        {
            return;
        }

        Engine.Seek(Math.Max(0, seconds));
        SyncTransport();
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

    public void SetFitMode(string mode)
    {
        FitMode = mode is "cover" ? "cover" : "contain";
        Window.FitMode = FitMode;
        Engine.SetFitMode(FitMode);
    }

    public void FrameStep(int direction)
    {
        Engine.FrameStep(direction);
        Shell.IsPaused = true;
        SyncTransport();
    }

    public void Checkpoint(string reason)
    {
        if (Current is not { } current || !Engine.IsOpen)
        {
            return;
        }

        var next = _flatEpisodes.Count > 0 ? SeriesScanner.NextEpisode(_flatEpisodes, current.Path) : null;
        var result = CompletionPolicy.Checkpoint(current, Engine.Position, Engine.Duration, next);
        Resume.Apply(result);
        Persist();

        if (reason is "ended" or "exit" && result.RecordedNextFromLastTenSeconds && AutoNext && next is { } nextId)
        {
            Open(nextId.Path);
        }
    }

    public void RememberWindow(WindowBounds bounds)
    {
        Window.Remember(bounds);
        Persist();
    }

    public SeriesShow OpenSeriesFolder(string folder)
    {
        _series = SeriesScanner.Scan(folder);
        _flatEpisodes = _series.Seasons.SelectMany(s => s.Episodes).ToList();
        Shell.Series.Tree = [.. _series.Seasons.Select(s => s.Name)];
        Shell.Series.Rows = [.. BuildRows(_series.Seasons.FirstOrDefault())];
        Shell.Status.SeriesSummary = Shell.Series.Rows.Count > 0
            ? $"{_series.Seasons.First().Name} {Shell.Series.Rows.Count}개 파일"
            : "";
        RefreshSidebar();
        if (_flatEpisodes.Count > 0 && Current is null)
        {
            Open(_flatEpisodes[0].Path);
        }

        return _series;
    }

    public void PlayNextEpisode()
    {
        if (Current is not { } current)
        {
            return;
        }

        var next = SeriesScanner.NextEpisode(_flatEpisodes, current.Path);
        if (next is { } identity)
        {
            Open(identity.Path);
        }
    }

    public void PlayPreviousEpisode()
    {
        if (Current is not { } current)
        {
            return;
        }

        for (var i = 0; i < _flatEpisodes.Count; i++)
        {
            if (string.Equals(_flatEpisodes[i].Path, current.Path, PathValidator.PathComparison) && i > 0)
            {
                Open(_flatEpisodes[i - 1].Path);
                return;
            }
        }
    }

    public void Tick(DateTimeOffset now)
    {
        SyncTransport();
        if (Shell.Transport.CaptionsOn && Cues.Count > 0)
        {
            var text = SubtitleParser.CueAt(Cues, TimeSpan.FromSeconds(Engine.Position));
            Shell.OverlaySubtitle = string.IsNullOrEmpty(text) ? "" : text;
        }

        UpdateChromeVisibility(now);
        if (Engine.IsOpen && Engine.Duration > 0 && Engine.Position >= Engine.Duration - 0.25)
        {
            Checkpoint("ended");
        }
    }

    public void EnterFullscreen()
    {
        Shell.EnterFullscreen();
        Shell.Fullscreen.Title = Current is { } cur
            ? FileNameSanitizer.ForDisplay(Path.GetFileNameWithoutExtension(cur.Path))
            : UiCopy.AppTitle;
        Shell.Fullscreen.AlwaysOnTop = Window.AlwaysOnTop;
    }

    public void ExitFullscreen() => Shell.ExitFullscreen();

    public void ContinueWatching()
    {
        if (Resume.Continue is { } pointer)
        {
            Open(pointer.Path);
        }
    }

    public void UpdateChromeVisibility(DateTimeOffset now)
        => Shell.ChromeVisible = FullscreenChromeController.ShouldShow(Shell.Screen == ShellScreen.Fullscreen, Shell.IsPaused, now, _lastActivity);

    public void NoteActivity(DateTimeOffset now) => _lastActivity = now;

    private DateTimeOffset _lastActivity = DateTimeOffset.UtcNow;

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
        Shell.OverlayTime = $"{Shell.Transport.PositionText} / {Shell.Transport.DurationText}";
        Shell.IsPaused = Engine.IsPaused;
    }

    private IEnumerable<SeriesRow> BuildRows(SeriesSeason? season)
    {
        if (season is null)
        {
            yield break;
        }

        foreach (var episode in season.Episodes)
        {
            var saved = Resume.Find(episode.Path, episode.Size);
            var progress = saved switch
            {
                { Completed: true } => "✓",
                { DurationSeconds: > 0 } e => $"{Math.Clamp(e.PositionSeconds / e.DurationSeconds * 100, 0, 100):0}%",
                _ => "-"
            };
            var current = Current is { } cur && string.Equals(cur.Path, episode.Path, PathValidator.PathComparison);
            yield return new SeriesRow(EpisodeParser.EpisodeLabel(episode.SortKey), episode.FileName, "--:--:--", progress, current);
        }
    }

    private void RefreshSidebar()
    {
        Shell.Sidebar.Items.Clear();
        Shell.Sidebar.Items.Add(UiCopy.ContinueWatching);
        foreach (var recent in Recent.Items.Take(8))
        {
            Shell.Sidebar.Items.Add(recent.Title);
        }
    }

    private void LoadPersisted()
    {
        Resume = ResumeStore.FromJson(ReadOptional("resume.json"));
        Recent = RecentStore.FromJson(ReadOptional("recent.json"));
        Window = WindowMemory.FromJson(ReadOptional("window.json"));
        Settings = AppSettings.FromJson(ReadOptional(AppSettings.FileName));
        FitMode = Window.FitMode;
        RefreshSidebar();
    }

    private void Persist()
    {
        WriteOptional("resume.json", Resume.ToJson());
        WriteOptional("recent.json", Recent.ToJson());
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
