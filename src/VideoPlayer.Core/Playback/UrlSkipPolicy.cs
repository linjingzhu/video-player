namespace VideoPlayer.Core.Playback;

/// <summary>
/// Chapter skip uses only chapters present on the stream. No IntroDB.
/// URL sources never invent markers; season-folder markers stay file-only.
/// </summary>
public static class UrlSkipPolicy
{
    public const bool UsesIntroDb = false;

    public static bool AllowsInventedMarkers(MediaSourceKind source)
        => source == MediaSourceKind.LocalFile;

    public static bool CanChapterSkip(IReadOnlyList<MediaChapter> streamChapters)
        => streamChapters.Count > 0;

    public static IReadOnlyList<MediaChapter> ChaptersForSkip(
        MediaSourceKind source,
        IReadOnlyList<MediaChapter> streamChapters)
    {
        _ = source;
        return streamChapters.Count == 0 ? [] : streamChapters;
    }

    public static MediaChapter? NextChapter(IReadOnlyList<MediaChapter> chapters, double position)
    {
        MediaChapter? next = null;
        foreach (var chapter in chapters.OrderBy(c => c.Start))
        {
            if (chapter.Start > position + 0.25)
            {
                next = chapter;
                break;
            }
        }

        return next;
    }

    public static MediaChapter? PreviousChapter(IReadOnlyList<MediaChapter> chapters, double position)
    {
        MediaChapter? previous = null;
        foreach (var chapter in chapters.OrderBy(c => c.Start))
        {
            if (chapter.Start < position - 0.25)
            {
                previous = chapter;
            }
        }

        return previous;
    }
}
