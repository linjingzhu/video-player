using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Safety;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Capture;

public enum CaptureFormat
{
    Png,
    Jpg,
    Webp
}

public enum CaptureBannerKind
{
    None,
    Info,
    Failure
}

public static class CaptureFormats
{
    public static IReadOnlyList<CaptureFormat> All { get; } =
        [CaptureFormat.Png, CaptureFormat.Jpg, CaptureFormat.Webp];

    public static string Extension(CaptureFormat format)
        => format switch
        {
            CaptureFormat.Jpg => "jpg",
            CaptureFormat.Webp => "webp",
            _ => "png"
        };

    public static CaptureFormat Parse(string? value)
    {
        if (string.Equals(value, "jpg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return CaptureFormat.Jpg;
        }

        if (string.Equals(value, "webp", StringComparison.OrdinalIgnoreCase))
        {
            return CaptureFormat.Webp;
        }

        return CaptureFormat.Png;
    }

    public static int Quality(CaptureFormat format)
        => format switch
        {
            CaptureFormat.Jpg => StillFrameCapture.JpegQuality,
            CaptureFormat.Webp => StillFrameCapture.WebpQuality,
            _ => 100
        };
}

public sealed record CaptureJob(
    string Stem,
    string FolderPath,
    int Count,
    int IntervalFrames,
    CaptureFormat Format);

public sealed record CaptureRunResult(
    int Requested,
    int Saved,
    bool HitEnd,
    bool Paused,
    bool NeedsConfirm,
    CaptureBannerKind BannerKind,
    string Banner,
    IReadOnlyList<string> Files);

/// <summary>Still-frame capture only. Not video segment save.</summary>
public static class StillFrameCapture
{
    public const int MinCount = 1;
    public const int MaxCount = 999;
    public const int DefaultCount = 1;
    public const int MinInterval = 1;
    public const int MaxInterval = 999;
    public const int DefaultInterval = 1;
    public const int ConfirmAt = 60;
    public const int JpegQuality = 90;
    public const int WebpQuality = 80;
    public const string PicturesLabel = "Pictures";
    public const CaptureFormat DefaultFormat = CaptureFormat.Png;

    public static bool NeedsConfirm(int count) => ClampCount(count) >= ConfirmAt;

    public static int ClampCount(int count) => Math.Clamp(count, MinCount, MaxCount);

    public static int ClampInterval(int interval) => Math.Clamp(interval, MinInterval, MaxInterval);

    public static string EofBanner(int requested, int saved)
        => string.Format(UiCopy.CaptureEofBanner, ClampCount(requested), Math.Max(0, saved));

    public static string ResolveFolder(string? lastUsed)
    {
        if (string.IsNullOrWhiteSpace(lastUsed))
        {
            return DefaultFolderPath();
        }

        var check = PathValidator.ValidateLocalFilePath(lastUsed);
        if (!check.Success || check.FullPath is null || !Directory.Exists(check.FullPath))
        {
            return DefaultFolderPath();
        }

        return check.FullPath;
    }

    public static string DefaultFolderPath()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (!string.IsNullOrWhiteSpace(pictures)
            && !PathsEqual(pictures, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
        {
            return pictures;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(home)
            ? PicturesLabel
            : Path.Combine(home, PicturesLabel);
    }

    public static string FolderLabel(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return PicturesLabel;
        }

        if (PathsEqual(folderPath, DefaultFolderPath()))
        {
            return PicturesLabel;
        }

        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (!string.IsNullOrWhiteSpace(pictures)
            && PathsEqual(folderPath, pictures))
        {
            return PicturesLabel;
        }

        var name = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(name) ? PicturesLabel : name;
    }

    public static string FileName(string? stem, TimeSpan position, int index, CaptureFormat format)
    {
        var safe = SanitizeStem(stem);
        return $"{safe}_{Clock(position)}_{Math.Max(1, index):0000}.{CaptureFormats.Extension(format)}";
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
        var cleaned = FileNameSanitizer.ForDisplay(string.IsNullOrWhiteSpace(stem) ? "capture" : stem);
        if (cleaned is "(이름 없음)" or "capture")
        {
            return "capture";
        }

        return cleaned.TrimEnd('…').Trim();
    }

    public static CaptureRunResult Run(IMediaEngine engine, CaptureJob job)
    {
        var requested = ClampCount(job.Count);
        var interval = ClampInterval(job.IntervalFrames);
        var files = new List<string>(requested);

        if (!engine.IsOpen)
        {
            return new CaptureRunResult(
                requested, 0, false, engine.IsPaused, false,
                CaptureBannerKind.Failure, UiCopy.CaptureNoMedia, files);
        }

        engine.Pause();

        var folderCheck = PathValidator.ValidateLocalFilePath(job.FolderPath);
        if (!folderCheck.Success || folderCheck.FullPath is null)
        {
            return new CaptureRunResult(
                requested, 0, false, true, false,
                CaptureBannerKind.Failure, folderCheck.Error ?? UiCopy.CaptureSaveFailed, files);
        }

        try
        {
            Directory.CreateDirectory(folderCheck.FullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new CaptureRunResult(
                requested, 0, false, true, false,
                CaptureBannerKind.Failure, UiCopy.CaptureSaveFailed, files);
        }

        var start = TimeSpan.FromSeconds(Math.Max(0, engine.Position));
        var hitEnd = engine.Duration > 0 && engine.Position >= engine.Duration - 0.001;

        for (var index = 1; index <= requested; index++)
        {
            var path = Path.Combine(folderCheck.FullPath, FileName(job.Stem, start, index, job.Format));
            if (!engine.ScreenshotToFile(path))
            {
                return new CaptureRunResult(
                    requested,
                    files.Count,
                    hitEnd,
                    true,
                    false,
                    CaptureBannerKind.Failure,
                    files.Count == 0
                        ? UiCopy.CaptureSaveFailed
                        : string.Format(UiCopy.CapturePartialFailBanner, files.Count, requested),
                    files);
            }

            files.Add(path);

            if (index == requested)
            {
                break;
            }

            var before = engine.Position;
            for (var step = 0; step < interval; step++)
            {
                engine.FrameStep(+1);
            }

            if (engine.Duration > 0 && engine.Position >= engine.Duration - 0.001)
            {
                hitEnd = true;
                if (engine.Position <= before + 1e-9)
                {
                    break;
                }

                continue;
            }

            if (engine.Position <= before + 1e-9)
            {
                hitEnd = true;
                break;
            }
        }

        if (files.Count < requested)
        {
            return new CaptureRunResult(
                requested,
                files.Count,
                true,
                true,
                false,
                files.Count == 0 ? CaptureBannerKind.Failure : CaptureBannerKind.Info,
                    files.Count == 0
                        ? UiCopy.CaptureSaveFailed
                        : EofBanner(requested, files.Count),
                    files);
        }

        return new CaptureRunResult(requested, files.Count, hitEnd, true, false, CaptureBannerKind.None, "", files);
    }

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

public sealed class CaptureSheetState
{
    public bool Open { get; set; }
    public int Count { get; set; } = StillFrameCapture.DefaultCount;
    public int IntervalFrames { get; set; } = StillFrameCapture.DefaultInterval;
    public CaptureFormat Format { get; set; } = StillFrameCapture.DefaultFormat;
    public string FolderPath { get; set; } = StillFrameCapture.DefaultFolderPath();
    public bool HasCameraOnTransport { get; } = false;
    public bool StillFramesOnly { get; } = true;
    public bool HasQualityControls { get; } = false;

    public string FolderLabel => StillFrameCapture.FolderLabel(FolderPath);
    public string IntervalText => $"{IntervalFrames}프레임";
    public string Title => UiCopy.CaptureSheetTitle;
    public string CountLabel => UiCopy.CaptureCount;
    public string IntervalLabel => UiCopy.CaptureInterval;
    public string FormatLabel => UiCopy.CaptureFormatLabel;
    public string FolderFieldLabel => UiCopy.CaptureFolder;
    public string CountRange => UiCopy.CaptureCountRange;
    public string Footer => UiCopy.CaptureFooter;
    public string StartLabel => UiCopy.CaptureStart;
    public string CancelLabel => UiCopy.CaptureCancel;
    public string ChangeFolderLabel => UiCopy.CaptureChangeFolder;
    public string PanelColor => SkinA.Panel;
    public string StartColor => SkinA.Accent;
    public string TextColor => SkinA.Text;
    public string SecondaryColor => SkinA.Secondary;
    public int PanelRadius => SkinA.RadiusPanel;
    public int StartRadius => SkinA.RadiusControl;
    public bool NeedsConfirm => StillFrameCapture.NeedsConfirm(Count);
    public IReadOnlyList<CaptureFormat> Formats => CaptureFormats.All;

    public void NudgeCount(int delta) => Count = StillFrameCapture.ClampCount(Count + delta);

    public void NudgeInterval(int delta) => IntervalFrames = StillFrameCapture.ClampInterval(IntervalFrames + delta);
}

public sealed class CaptureBannerState
{
    public string Text { get; set; } = "";
    public CaptureBannerKind Kind { get; set; } = CaptureBannerKind.None;
    public bool Visible => !string.IsNullOrWhiteSpace(Text);

    public void Clear()
    {
        Text = "";
        Kind = CaptureBannerKind.None;
    }

    public void Show(CaptureBannerKind kind, string text)
    {
        Kind = string.IsNullOrWhiteSpace(text) ? CaptureBannerKind.None : kind;
        Text = string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
    }
}
