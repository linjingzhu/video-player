using System.Text.Json;
using VideoPlayer.Core.Media;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Library;

public sealed record RecentItem(string Path, long Size, string Title, DateTimeOffset OpenedUtc);

public sealed class RecentStore
{
    public const int MaxItems = 30;
    private readonly List<RecentItem> _items = [];

    public IReadOnlyList<RecentItem> Items => _items;

    public bool TryAdd(string path, long size, string? videoCodec, string? audioCodec)
    {
        if (SupportedFormats.IsOutOfScopeContainer(path)
            || SupportedFormats.IsOutOfScopeCodec(videoCodec)
            || (videoCodec is not null && !SupportedFormats.IsSupportedVideoCodec(videoCodec)))
        {
            return false;
        }

        if (audioCodec is not null && !SupportedFormats.IsSupportedAudioCodec(audioCodec)
            && !SupportedFormats.IsSupportedVideoCodec(videoCodec))
        {
            return false;
        }

        var check = PathValidator.ValidateLocalFilePath(path);
        if (!check.Success || check.FullPath is null)
        {
            return false;
        }

        _items.RemoveAll(i => string.Equals(i.Path, check.FullPath, PathValidator.PathComparison) && i.Size == size);
        _items.Insert(0, new RecentItem(
            check.FullPath,
            size,
            FileNameSanitizer.ForDisplay(Path.GetFileName(check.FullPath)),
            DateTimeOffset.UtcNow));

        if (_items.Count > MaxItems)
        {
            _items.RemoveRange(MaxItems, _items.Count - MaxItems);
        }

        return true;
    }

    public string ToJson() => JsonSerializer.Serialize(_items, JsonOptions);

    public static RecentStore FromJson(string? json)
    {
        var store = new RecentStore();
        if (string.IsNullOrWhiteSpace(json))
        {
            return store;
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<RecentItem>>(json, JsonOptions);
            if (items is null)
            {
                return store;
            }

            foreach (var item in items)
            {
                if (item is null || PathValidator.IsRemoteUri(item.Path) || PathValidator.LooksLikeUnc(item.Path))
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
