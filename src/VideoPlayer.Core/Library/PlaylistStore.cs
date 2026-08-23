using System.Text.Json;
using VideoPlayer.Core.Media;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Library;

public sealed record PlaylistItem(string Path, long Size, string Title);

public sealed class PlaylistStore
{
    private readonly List<PlaylistItem> _items = [];
    public IReadOnlyList<PlaylistItem> Items => _items;

    public bool Add(string path, long size)
    {
        var check = PathValidator.ValidateLocalFilePath(path);
        if (!check.Success || check.FullPath is null || !SupportedFormats.IsSupportedContainer(check.FullPath))
        {
            return false;
        }

        _items.Add(new PlaylistItem(check.FullPath, size, FileNameSanitizer.ForDisplay(Path.GetFileName(check.FullPath))));
        return true;
    }

    public string ToJson() => JsonSerializer.Serialize(_items, JsonOptions);

    public static PlaylistStore FromJson(string? json)
    {
        var store = new PlaylistStore();
        if (string.IsNullOrWhiteSpace(json))
        {
            return store;
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<PlaylistItem>>(json, JsonOptions) ?? [];
            foreach (var item in items)
            {
                if (item is null)
                {
                    continue;
                }

                store.Add(item.Path, item.Size);
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

public sealed record WindowBounds(double X, double Y, double Width, double Height)
{
    public static WindowBounds Default { get; } = new(80, 80, 1280, 800);

    public WindowBounds Sanitize()
    {
        var width = Width is > 400 and < 10000 ? Width : Default.Width;
        var height = Height is > 300 and < 10000 ? Height : Default.Height;
        var x = double.IsFinite(X) && X is > -5000 and < 20000 ? X : Default.X;
        var y = double.IsFinite(Y) && Y is > -5000 and < 20000 ? Y : Default.Y;
        return new WindowBounds(x, y, width, height);
    }
}

public sealed class WindowMemory
{
    public WindowBounds Bounds { get; private set; } = WindowBounds.Default;
    public bool AlwaysOnTop { get; set; }
    public string FitMode { get; set; } = "contain";

    public void Remember(WindowBounds bounds) => Bounds = bounds.Sanitize();

    public string ToJson() => JsonSerializer.Serialize(new WindowStateDto(Bounds, AlwaysOnTop, FitMode), JsonOptions);

    public static WindowMemory FromJson(string? json)
    {
        var memory = new WindowMemory();
        if (string.IsNullOrWhiteSpace(json))
        {
            return memory;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<WindowStateDto>(json, JsonOptions);
            if (dto?.Bounds is not null)
            {
                memory.Bounds = dto.Bounds.Sanitize();
            }

            memory.AlwaysOnTop = dto?.AlwaysOnTop ?? false;
            memory.FitMode = dto?.FitMode is "cover" ? "cover" : "contain";
        }
        catch (JsonException)
        {
            return memory;
        }

        return memory;
    }

    private sealed record WindowStateDto(WindowBounds? Bounds, bool AlwaysOnTop, string? FitMode);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
