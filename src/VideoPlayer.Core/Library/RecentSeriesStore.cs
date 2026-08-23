using System.Text.Json;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Library;

public sealed record RecentSeriesItem(string FolderPath, string Title, DateTimeOffset OpenedUtc);

public sealed class RecentSeriesStore
{
    public const int MaxItems = 12;
    private readonly List<RecentSeriesItem> _items = [];

    public IReadOnlyList<RecentSeriesItem> Items => _items;

    public void Add(string folderPath, string title)
    {
        var check = PathValidator.ValidateLocalFilePath(folderPath);
        if (!check.Success || check.FullPath is null)
        {
            return;
        }

        _items.RemoveAll(i => string.Equals(i.FolderPath, check.FullPath, PathValidator.PathComparison));
        _items.Insert(0, new RecentSeriesItem(
            check.FullPath,
            FileNameSanitizer.ForDisplay(title),
            DateTimeOffset.UtcNow));

        if (_items.Count > MaxItems)
        {
            _items.RemoveRange(MaxItems, _items.Count - MaxItems);
        }
    }

    public string ToJson() => JsonSerializer.Serialize(_items, JsonOptions);

    public static RecentSeriesStore FromJson(string? json)
    {
        var store = new RecentSeriesStore();
        if (string.IsNullOrWhiteSpace(json))
        {
            return store;
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<RecentSeriesItem>>(json, JsonOptions) ?? [];
            foreach (var item in items)
            {
                if (item is null || PathValidator.IsRemoteUri(item.FolderPath) || PathValidator.LooksLikeUnc(item.FolderPath))
                {
                    continue;
                }

                store._items.Add(item);
            }
        }
        catch (JsonException)
        {
            return store;
        }

        return store;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
