using VideoPlayer.Core.Playback;

namespace VideoPlayer.Core.Skip;

/// <summary>
/// Skip regions from locked chapter aliases and season-folder user markers only.
/// Overlap: recap before intro. No IntroDB.
/// </summary>
public static class SkipDetector
{
    public static IReadOnlyList<SkipSegment> Detect(
        IReadOnlyList<MediaChapter> chapters,
        IReadOnlyList<SkipSegment> markers,
        double duration)
    {
        var byKind = new Dictionary<SkipKind, SkipSegment>();

        foreach (var chapter in ExpandChapters(chapters, duration))
        {
            var kind = ChapterAliases.Classify(chapter.Title);
            if (kind is not { } mapped || chapter.End <= chapter.Start)
            {
                continue;
            }

            byKind[mapped] = new SkipSegment(mapped, chapter.Start, chapter.End, SkipSource.Chapter);
        }

        foreach (var marker in markers)
        {
            if (marker.End <= marker.Start)
            {
                continue;
            }

            byKind[marker.Kind] = marker with { Source = SkipSource.Marker };
        }

        return SkipKinds.DisplayOrder
            .Where(byKind.ContainsKey)
            .Select(kind => byKind[kind])
            .ToList();
    }

    public static SkipSegment? Active(IReadOnlyList<SkipSegment> segments, double position, IReadOnlySet<SkipKind>? dismissed = null)
    {
        foreach (var kind in SkipKinds.DisplayOrder)
        {
            if (dismissed is not null && dismissed.Contains(kind))
            {
                continue;
            }

            var match = segments.FirstOrDefault(segment => segment.Kind == kind && segment.Contains(position));
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    public static IReadOnlyList<MediaChapter> ExpandChapters(IReadOnlyList<MediaChapter> chapters, double duration)
    {
        if (chapters.Count == 0)
        {
            return [];
        }

        var ordered = chapters
            .Select((chapter, index) => (chapter, index))
            .OrderBy(item => item.chapter.Start)
            .ThenBy(item => item.index)
            .Select(item => item.chapter)
            .ToList();

        var expanded = new List<MediaChapter>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var start = Math.Max(0, ordered[i].Start);
            var end = ordered[i].End > start
                ? ordered[i].End
                : i + 1 < ordered.Count
                    ? ordered[i + 1].Start
                    : duration > start ? duration : start;
            expanded.Add(new MediaChapter(ordered[i].Title, start, end));
        }

        return expanded;
    }
}
