namespace VideoPlayer.Core.Playback;

public enum MediaSourceKind
{
    None,
    LocalFile,
    HttpUrl
}

public enum FileOnlyFeature
{
    SeriesTree,
    AutoNext,
    Capture,
    ClipSave
}

/// <summary>
/// Series tree, 다음 화 auto-next, capture, and clip-save stay local-file-only.
/// Capture / clip-save PRs should consult these gates rather than infer from the path.
/// </summary>
public static class FileOnlyFeatures
{
    public static bool Allows(MediaSourceKind source, FileOnlyFeature feature)
    {
        if (source == MediaSourceKind.HttpUrl)
        {
            return false;
        }

        if (source == MediaSourceKind.LocalFile)
        {
            return true;
        }

        return feature is FileOnlyFeature.SeriesTree or FileOnlyFeature.AutoNext;
    }

    public static void Apply(FileOnlyFeatureState state, MediaSourceKind source, bool mediaOpen)
    {
        var file = source == MediaSourceKind.LocalFile;
        state.SeriesTree = Allows(source, FileOnlyFeature.SeriesTree);
        state.AutoNext = Allows(source, FileOnlyFeature.AutoNext);
        state.Capture = file && mediaOpen && Allows(source, FileOnlyFeature.Capture);
        state.ClipSave = file && mediaOpen && Allows(source, FileOnlyFeature.ClipSave);
    }
}

public sealed class FileOnlyFeatureState
{
    public bool SeriesTree { get; set; } = true;
    public bool AutoNext { get; set; } = true;
    public bool Capture { get; set; }
    public bool ClipSave { get; set; }
}
