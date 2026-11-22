namespace VideoPlayer.Core.Playback;

public static class ChapterMenu
{
    public const string MenuHeader = "챕터";
    public const string EmptyLabel = "챕터 없음";

    public readonly record struct Entry(string Label, double? Start, bool Enabled);

    public static IReadOnlyList<Entry> Build(IReadOnlyList<MediaChapter>? chapters)
    {
        if (chapters is null || chapters.Count == 0)
        {
            return [new Entry(EmptyLabel, null, false)];
        }

        var entries = new List<Entry>(chapters.Count);
        for (var i = 0; i < chapters.Count; i++)
        {
            var chapter = chapters[i];
            var label = string.IsNullOrWhiteSpace(chapter.Title) ? $"챕터 {i + 1}" : chapter.Title.Trim();
            entries.Add(new Entry(label, chapter.Start, true));
        }

        return entries;
    }
}
