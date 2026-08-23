using System.Text.Json;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Skip;

/// <summary>
/// User "여기까지 스킵" markers keyed by season folder and shared across episodes.
/// No IntroDB or accounts.
/// </summary>
public sealed class SeasonSkipStore
{
    public const string FileName = "season-skips.json";
    public const double MinLengthSeconds = 1;

    private readonly Dictionary<string, List<SkipSegment>> _bySeason = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SkipSegment> ForSeason(string? seasonFolder)
    {
        var key = NormalizeKey(seasonFolder);
        return key is not null && _bySeason.TryGetValue(key, out var items) ? [.. items] : [];
    }

    public IReadOnlyList<SkipSegment> ForMedia(string mediaPath)
        => ForSeason(SeasonFolder(mediaPath));

    public SkipSegment? MarkToHere(string mediaPath, double position)
    {
        var folder = SeasonFolder(mediaPath);
        if (folder is null || position < MinLengthSeconds)
        {
            return null;
        }

        var existing = ForSeason(folder);
        var previous = existing
            .Where(item => item.Kind is SkipKind.Intro or SkipKind.Recap)
            .OrderBy(item => item.Start)
            .LastOrDefault();
        var start = previous?.Start ?? 0;
        if (position <= start)
        {
            return null;
        }

        var kind = previous?.Kind ?? SkipKind.Intro;
        var segment = new SkipSegment(kind, start, position, SkipSource.Marker);
        var next = existing.Where(item => item.Kind != kind).ToList();
        next.Add(segment);
        _bySeason[folder] = next;
        return segment;
    }

    public string ToJson()
    {
        var dto = new StoreDto(
            _bySeason.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(item => new MarkerDto(KindName(item.Kind), item.Start, item.End)).ToList(),
                StringComparer.OrdinalIgnoreCase));
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public static SeasonSkipStore FromJson(string? json)
    {
        var store = new SeasonSkipStore();
        if (string.IsNullOrWhiteSpace(json))
        {
            return store;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<StoreDto>(json, JsonOptions);
            if (dto?.Markers is null)
            {
                return store;
            }

            foreach (var (folder, markers) in dto.Markers)
            {
                var key = NormalizeKey(folder);
                if (key is null)
                {
                    continue;
                }

                var items = new List<SkipSegment>();
                foreach (var marker in markers)
                {
                    var kind = ChapterAliases.Classify(marker.Kind) ?? ParseKind(marker.Kind);
                    if (kind is { } mapped && marker.End > marker.Start)
                    {
                        items.Add(new SkipSegment(mapped, marker.Start, marker.End, SkipSource.Marker));
                    }
                }

                store._bySeason[key] = items;
            }
        }
        catch (JsonException)
        {
            return store;
        }

        return store;
    }

    public static string? SeasonFolder(string? mediaPath)
    {
        var media = PathValidator.ValidateLocalFilePath(mediaPath);
        if (!media.Success || media.FullPath is null)
        {
            return null;
        }

        var directory = Path.GetDirectoryName(media.FullPath);
        return NormalizeKey(directory);
    }

    private static string? NormalizeKey(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return null;
        }

        var check = PathValidator.ValidateLocalFilePath(folder);
        if (!check.Success || check.FullPath is null)
        {
            return null;
        }

        return Path.GetFullPath(check.FullPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string KindName(SkipKind kind)
        => kind switch
        {
            SkipKind.Recap => "recap",
            SkipKind.Credits => "credits",
            _ => "intro"
        };

    private static SkipKind? ParseKind(string? value)
        => ChapterAliases.Normalize(value) switch
        {
            "intro" => SkipKind.Intro,
            "recap" => SkipKind.Recap,
            "credits" => SkipKind.Credits,
            _ => null
        };

    private sealed record StoreDto(Dictionary<string, List<MarkerDto>> Markers);

    private sealed record MarkerDto(string Kind, double Start, double End);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
