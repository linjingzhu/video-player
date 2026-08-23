using System.Globalization;
using VideoPlayer.Core.Safety;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Clip;

public enum ClipFormat
{
    StreamCopy,
    Webp,
    Gif
}

public enum ClipBannerKind
{
    None,
    Failure
}

public enum ClipTickKind
{
    Square
}

public static class ClipFormats
{
    public static IReadOnlyList<ClipFormat> All { get; } =
        [ClipFormat.StreamCopy, ClipFormat.Webp, ClipFormat.Gif];

    public static string Label(ClipFormat format)
        => format switch
        {
            ClipFormat.Webp => UiCopy.ClipFormatWebp,
            ClipFormat.Gif => UiCopy.ClipFormatGif,
            _ => UiCopy.ClipFormatStreamCopy
        };

    public static string Extension(ClipFormat format, string? sourcePath)
        => format switch
        {
            ClipFormat.Webp => "webp",
            ClipFormat.Gif => "gif",
            _ => SourceExtension(sourcePath)
        };

    public static string SourceExtension(string? sourcePath)
    {
        var ext = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(ext))
        {
            return "mkv";
        }

        return ext.TrimStart('.').ToLowerInvariant();
    }

    public static ClipFormat Parse(string? value)
    {
        if (string.Equals(value, "webp", StringComparison.OrdinalIgnoreCase))
        {
            return ClipFormat.Webp;
        }

        if (string.Equals(value, "gif", StringComparison.OrdinalIgnoreCase))
        {
            return ClipFormat.Gif;
        }

        return ClipFormat.StreamCopy;
    }

    /// <summary>webp and gif re-encode with fps + ping-pong. Only 원본복사 locks those controls.</summary>
    public static bool EncodingEnabled(ClipFormat format)
        => format is ClipFormat.Webp or ClipFormat.Gif;

    public static bool PaletteNoticeVisible(ClipFormat format)
        => format == ClipFormat.Gif;

    public static bool KeyframeNoticeVisible(ClipFormat format)
        => format == ClipFormat.StreamCopy;
}

public sealed record ClipJob(
    string SourcePath,
    string Stem,
    string FolderPath,
    double StartSeconds,
    double EndSeconds,
    ClipFormat Format,
    int? Fps,
    bool PingPong);

public sealed record ClipRunResult(
    bool Saved,
    string? Path,
    ClipBannerKind BannerKind,
    string Banner,
    bool SheetStaysOpen,
    IReadOnlyList<string> Arguments);

public readonly record struct ClipProcessResult(bool Success, int ExitCode, string Error);

public interface IClipProcessRunner
{
    string? Executable { get; }

    ClipProcessResult Run(string executable, IReadOnlyList<string> arguments);
}

/// <summary>
/// Confirmed v4 구간 저장. Stream-copy, animated webp, or 256-color gif.
/// Folder is clip-only (default Videos\구간), never the capture folder.
/// </summary>
public static class ClipSave
{
    public const double MinDurationSeconds = 1;
    public const int MinFps = 1;
    public const int MaxFps = 60;
    public const int NudgeFromSourceFps = 15;
    public const int GifPaletteColors = 256;
    public const int WebpQuality = 80;
    public const string DefaultFolderLabel = @"Videos\구간";
    public const string DefaultFolderLeaf = "구간";
    public const ClipFormat DefaultFormat = ClipFormat.StreamCopy;
    public const bool DefaultPingPong = false;
    public const ClipTickKind TickKind = ClipTickKind.Square;
    public const int TickSizePx = SkinA.IoTickSizePx;
    public const bool RenderIoLetters = false;
    public const bool HasPaletteControl = false;
    public const bool HasRecordButton = false;
    public const bool HasVideoDragSelect = false;
    public const bool HasSheetCurrentMarks = true;
    public const bool HasKeyboardIoMarks = true;

    public static bool IsLongEnough(double durationSeconds)
        => durationSeconds >= MinDurationSeconds - 1e-9;

    public static bool IsValidRange(double start, double end)
        => end >= start && IsLongEnough(end - start);

    public static bool CanSave(bool hasMedia, double start, double end)
        => hasMedia && IsValidRange(start, end);

    public static bool EncodingEnabled(ClipFormat format) => ClipFormats.EncodingEnabled(format);

    public static int? EffectiveFps(ClipFormat format, int? fps)
        => EncodingEnabled(format) ? ClampFps(fps) : null;

    public static bool EffectivePingPong(ClipFormat format, bool pingPong)
        => EncodingEnabled(format) && pingPong;

    public static int? ClampFps(int? fps)
    {
        if (fps is null)
        {
            return null;
        }

        return Math.Clamp(fps.Value, MinFps, MaxFps);
    }

    public static int? NudgeFps(int? current, int delta)
    {
        if (current is null)
        {
            return delta > 0 ? NudgeFromSourceFps : null;
        }

        var next = current.Value + delta;
        if (next < MinFps)
        {
            return null;
        }

        return Math.Clamp(next, MinFps, MaxFps);
    }

    public static (double Start, double End) ResolveRange(
        double? inMark,
        double? outMark,
        double position,
        double mediaDuration)
    {
        double start;
        double end;
        if (inMark is { } inn && outMark is { } outt)
        {
            start = inn;
            end = outt;
        }
        else if (inMark is { } onlyIn)
        {
            start = onlyIn;
            end = mediaDuration > 0 ? mediaDuration : onlyIn;
        }
        else if (outMark is { } onlyOut)
        {
            start = 0;
            end = onlyOut;
        }
        else
        {
            start = Math.Max(0, position);
            end = mediaDuration > 0 ? mediaDuration : start;
        }

        if (double.IsNaN(start) || start < 0)
        {
            start = 0;
        }

        if (double.IsNaN(end) || end < 0)
        {
            end = 0;
        }

        if (mediaDuration > 0)
        {
            start = Math.Clamp(start, 0, mediaDuration);
            end = Math.Clamp(end, 0, mediaDuration);
        }

        return (start, end);
    }

    public static double TickRatio(double? mark, double mediaDuration)
    {
        if (mark is not { } value || mediaDuration <= 0)
        {
            return 0;
        }

        return Math.Clamp(value / mediaDuration, 0, 1);
    }

    public static string Clock(TimeSpan position)
    {
        if (position < TimeSpan.Zero || double.IsNaN(position.TotalSeconds))
        {
            position = TimeSpan.Zero;
        }

        var hours = (int)Math.Floor(position.TotalHours);
        return $"{hours:00}{position.Minutes:00}{position.Seconds:00}";
    }

    public static string SanitizeStem(string? stem)
    {
        var cleaned = FileNameSanitizer.ForDisplay(string.IsNullOrWhiteSpace(stem) ? "clip" : stem);
        if (cleaned is "(이름 없음)" or "clip")
        {
            return "clip";
        }

        return cleaned.TrimEnd('…').Trim();
    }

    public static string FileName(
        string? stem,
        double startSeconds,
        double endSeconds,
        ClipFormat format,
        string? sourcePath)
    {
        var safe = SanitizeStem(stem);
        var start = Clock(TimeSpan.FromSeconds(Math.Max(0, startSeconds)));
        var end = Clock(TimeSpan.FromSeconds(Math.Max(0, endSeconds)));
        return $"{safe}_{start}-{end}.{ClipFormats.Extension(format, sourcePath)}";
    }

    public static string DefaultFolderPath()
    {
        var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        if (string.IsNullOrWhiteSpace(videos)
            || PathsEqual(videos, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            videos = string.IsNullOrWhiteSpace(home) ? "Videos" : Path.Combine(home, "Videos");
        }

        return Path.Combine(videos, DefaultFolderLeaf);
    }

    public static string ResolveFolder(string? lastUsed)
    {
        if (string.IsNullOrWhiteSpace(lastUsed))
        {
            return DefaultFolderPath();
        }

        var check = PathValidator.ValidateLocalFilePath(lastUsed);
        if (!check.Success || check.FullPath is null)
        {
            return DefaultFolderPath();
        }

        if (!Directory.Exists(check.FullPath) && !PathsEqual(check.FullPath, DefaultFolderPath()))
        {
            return DefaultFolderPath();
        }

        return check.FullPath;
    }

    public static bool TryAcceptFolder(string? path, out string fullPath)
    {
        var check = PathValidator.ValidateLocalFilePath(path);
        if (!check.Success || check.FullPath is null)
        {
            fullPath = DefaultFolderPath();
            return false;
        }

        fullPath = check.FullPath;
        return true;
    }

    public static string FolderLabel(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || PathsEqual(folderPath, DefaultFolderPath()))
        {
            return DefaultFolderLabel;
        }

        var trimmed = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var leaf = Path.GetFileName(trimmed);
        var parent = Path.GetFileName(Path.GetDirectoryName(trimmed) ?? "");
        if (string.Equals(leaf, DefaultFolderLeaf, PathValidator.PathComparison)
            && (string.Equals(parent, "Videos", PathValidator.PathComparison)
                || string.Equals(parent, "videos", StringComparison.OrdinalIgnoreCase)))
        {
            return DefaultFolderLabel;
        }

        return string.IsNullOrWhiteSpace(leaf) ? DefaultFolderLabel : leaf;
    }

    public static string Timestamp(double seconds)
        => Math.Max(0, seconds).ToString("0.###", CultureInfo.InvariantCulture);

    public static string? VideoFilter(ClipJob job)
    {
        if (!EncodingEnabled(job.Format))
        {
            return null;
        }

        var fps = EffectiveFps(job.Format, job.Fps);
        var pingPong = EffectivePingPong(job.Format, job.PingPong);
        var parts = new List<string>();
        if (fps is { } rate)
        {
            parts.Add($"fps={rate.ToString(CultureInfo.InvariantCulture)}");
        }

        if (pingPong)
        {
            parts.Add("split[fwd][tmp];[tmp]reverse[rev];[fwd][rev]concat=n=2:v=1:a=0");
        }

        if (job.Format == ClipFormat.Gif)
        {
            parts.Add($"split[s0][s1];[s0]palettegen=max_colors={GifPaletteColors}[p];[s1][p]paletteuse");
        }

        return parts.Count == 0 ? null : string.Join(',', parts);
    }

    public static IReadOnlyList<string> BuildArguments(ClipJob job, string outputPath)
    {
        var start = Math.Max(0, job.StartSeconds);
        var duration = Math.Max(0, job.EndSeconds - start);
        var args = new List<string>
        {
            "-y",
            "-ss", Timestamp(start),
            "-i", job.SourcePath,
            "-t", Timestamp(duration)
        };

        if (job.Format == ClipFormat.StreamCopy)
        {
            args.AddRange(["-c", "copy", "-map", "0", "-avoid_negative_ts", "make_zero", outputPath]);
            return args;
        }

        args.Add("-an");
        var filter = VideoFilter(job);
        if (filter is not null)
        {
            args.AddRange(["-vf", filter]);
        }

        if (job.Format == ClipFormat.Webp)
        {
            args.AddRange(["-loop", "0", "-c:v", "libwebp", "-quality", WebpQuality.ToString(CultureInfo.InvariantCulture)]);
        }

        args.Add(outputPath);
        return args;
    }

    public static ClipRunResult Run(ClipJob job, IClipProcessRunner runner)
    {
        if (string.IsNullOrWhiteSpace(job.SourcePath) || !File.Exists(job.SourcePath))
        {
            return Fail(UiCopy.ClipNoMedia, Array.Empty<string>());
        }

        if (!IsValidRange(job.StartSeconds, job.EndSeconds))
        {
            return Fail(UiCopy.ClipTooShort, Array.Empty<string>());
        }

        var folderCheck = PathValidator.ValidateLocalFilePath(job.FolderPath);
        if (!folderCheck.Success || folderCheck.FullPath is null)
        {
            return Fail(folderCheck.Error ?? UiCopy.ClipSaveFailed, Array.Empty<string>());
        }

        try
        {
            Directory.CreateDirectory(folderCheck.FullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Fail(UiCopy.ClipSaveFailed, Array.Empty<string>());
        }

        var output = Path.Combine(
            folderCheck.FullPath,
            FileName(job.Stem, job.StartSeconds, job.EndSeconds, job.Format, job.SourcePath));
        if (!PathValidator.IsInsideDirectory(output, folderCheck.FullPath))
        {
            return Fail(UiCopy.ClipSaveFailed, Array.Empty<string>());
        }

        var args = BuildArguments(job with { FolderPath = folderCheck.FullPath }, output);
        if (string.IsNullOrWhiteSpace(runner.Executable))
        {
            return Fail(UiCopy.ClipFfmpegMissing, args);
        }

        var result = runner.Run(runner.Executable, args);
        if (!result.Success)
        {
            return Fail(UiCopy.ClipSaveFailed, args);
        }

        return new ClipRunResult(true, output, ClipBannerKind.None, "", false, args);
    }

    private static ClipRunResult Fail(string banner, IReadOnlyList<string> arguments)
        => new(false, null, ClipBannerKind.Failure, banner, true, arguments);

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), PathValidator.PathComparison);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}

public sealed class ClipSheetState
{
    public bool Open { get; set; }
    public ClipFormat Format { get; set; } = ClipSave.DefaultFormat;
    public int? Fps { get; set; }
    public bool PingPong { get; set; } = ClipSave.DefaultPingPong;
    public string FolderPath { get; set; } = ClipSave.DefaultFolderPath();
    public string Stem { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public double? InMark { get; set; }
    public double? OutMark { get; set; }
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public double ClipDurationSeconds { get; set; }
    public bool CanSave { get; set; }
    public bool HasMedia { get; set; }

    public bool FpsEnabled => ClipSave.EncodingEnabled(Format);
    public bool PingPongEnabled => ClipSave.EncodingEnabled(Format);
    public bool PaletteNoticeVisible => ClipFormats.PaletteNoticeVisible(Format);
    public bool KeyframeNoticeVisible => ClipFormats.KeyframeNoticeVisible(Format);
    public bool EncodingLockHintVisible => !ClipSave.EncodingEnabled(Format);
    public bool HasPaletteControl { get; } = ClipSave.HasPaletteControl;
    public bool HasRecordButton { get; } = ClipSave.HasRecordButton;
    public bool HasVideoDragSelect { get; } = ClipSave.HasVideoDragSelect;
    public bool HasSheetCurrentMarks { get; } = ClipSave.HasSheetCurrentMarks;
    public bool HasKeyboardIoMarks { get; } = ClipSave.HasKeyboardIoMarks;
    public bool CanMarkCurrent => HasMedia;
    public bool RenderIoLetters { get; } = ClipSave.RenderIoLetters;
    public ClipTickKind TickKind { get; } = ClipSave.TickKind;
    public int TickSizePx { get; } = ClipSave.TickSizePx;
    public string InLetter => "";
    public string OutLetter => "";
    public string TickColor => SeriesOn.Accent;
    public bool ShowInTick => InMark is not null;
    public bool ShowOutTick => OutMark is not null;
    public IReadOnlyList<ClipFormat> Formats => ClipFormats.All;

    public string Title => UiCopy.ClipSave;
    public string StartLabel => UiCopy.ClipStart;
    public string EndLabel => UiCopy.ClipEnd;
    public string SetStartFromNowLabel => UiCopy.ClipSetStartFromNow;
    public string SetEndFromNowLabel => UiCopy.ClipSetEndFromNow;
    public string DurationLabel => UiCopy.ClipDuration;
    public string FormatLabel => UiCopy.ClipFormat;
    public string FpsLabel => UiCopy.ClipFps;
    public string PingPongLabel => UiCopy.ClipPingPong;
    public string PaletteLabel => UiCopy.ClipPalette;
    public string PaletteValue => UiCopy.ClipPaletteValue;
    public string EncodingLockHint => UiCopy.ClipEncodingOff;
    public string KeyframeNotice => UiCopy.ClipKeyframeNotice;
    public string FolderFieldLabel => UiCopy.ClipFolder;
    public string ChangeFolderLabel => UiCopy.ClipChangeFolder;
    public string SaveLabel => UiCopy.ClipSaveAction;
    public string CancelLabel => UiCopy.ClipCancel;
    public string FolderLabel => ClipSave.FolderLabel(FolderPath);
    public string StartText => TransportState.Format(StartSeconds);
    public string EndText => TransportState.Format(EndSeconds);
    public string DurationText => TransportState.Format(ClipDurationSeconds);
    public string FpsText => Fps is { } fps
        ? fps.ToString(CultureInfo.InvariantCulture)
        : UiCopy.ClipFpsSource;
    public string PreviewFileName => ClipSave.FileName(Stem, StartSeconds, EndSeconds, Format, SourcePath);
    public string PanelColor => SkinA.Panel;
    public string SaveColor => SkinA.Accent;
    public string TextColor => SkinA.Text;
    public string SecondaryColor => SkinA.Secondary;
    public int PanelRadius => SkinA.RadiusPanel;
    public int SaveRadius => SkinA.RadiusControl;
    public int TitleSize => SkinA.TitleSize;
    public int BodySize => SkinA.BodySize;
    public int MetaSize => SkinA.MetaSize;

    public void ClearMarks()
    {
        InMark = null;
        OutMark = null;
    }

    public void NudgeFps(int delta) => Fps = ClipSave.NudgeFps(Fps, delta);
}

public sealed class ClipBannerState
{
    public string Text { get; set; } = "";
    public ClipBannerKind Kind { get; set; } = ClipBannerKind.None;
    public bool Visible => !string.IsNullOrWhiteSpace(Text);

    public void Clear()
    {
        Text = "";
        Kind = ClipBannerKind.None;
    }

    public void Show(ClipBannerKind kind, string text)
    {
        Kind = string.IsNullOrWhiteSpace(text) ? ClipBannerKind.None : kind;
        Text = string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
    }
}
