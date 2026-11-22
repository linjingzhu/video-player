using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Library;

public static class RecentFilesMenu
{
    public const int MaxVisible = 10;
    public const string MenuHeader = "최근 파일";
    public const string MissingLabel = "(없음)";
    public const string ClearLabel = "목록 비우기";

    public readonly record struct Entry(string Path, string Label, bool Missing);

    public static IReadOnlyList<Entry> Build(IEnumerable<RecentItem> items, Func<string, bool> exists)
    {
        var result = new List<Entry>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Path)
                || PathValidator.IsRemoteUri(item.Path)
                || PathValidator.LooksLikeUnc(item.Path))
            {
                continue;
            }

            if (result.Count >= MaxVisible)
            {
                break;
            }

            var missing = !exists(item.Path);
            var label = missing
                ? MissingLabel
                : FileNameSanitizer.ForDisplay(Path.GetFileNameWithoutExtension(item.Path));
            result.Add(new Entry(item.Path, label, missing));
        }

        return result;
    }
}
