using System.Globalization;
using System.Text.Json;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Skip;

/// <summary>
/// In-season skip markers next to the episode. Same folder only. No remote lookup.
/// </summary>
public static class SeasonSkipMarkers
{
    public const int MaxFileBytes = 64 * 1024;
    public const string SeasonFileName = "skip-markers.json";
    public const string HiddenSeasonFileName = ".skip-markers.json";

    public static IReadOnlyList<string> CandidateFileNames(string stem)
        =>
        [
            stem + ".skip.json",
            SeasonFileName,
            HiddenSeasonFileName
        ];

    public static IReadOnlyList<SkipSegment> Load(string mediaPath)
    {
        var media = PathValidator.ValidateLocalFilePath(mediaPath);
        if (!media.Success || media.FullPath is null)
        {
            return [];
        }

        var directory = Path.GetDirectoryName(media.FullPath);
        var stem = Path.GetFileNameWithoutExtension(media.FullPath);
        if (directory is null || string.IsNullOrEmpty(stem) || FileNameSanitizer.LooksMalicious(stem + ".skip.json"))
        {
            return [];
        }

        foreach (var name in CandidateFileNames(stem))
        {
            if (FileNameSanitizer.LooksMalicious(name))
            {
                continue;
            }

            var candidate = Path.Combine(directory, name);
            var resolved = PathValidator.ValidateLocalFilePath(candidate);
            if (!resolved.Success || resolved.FullPath is null)
            {
                continue;
            }

            if (!PathValidator.IsSameDirectory(media.FullPath, resolved.FullPath))
            {
                continue;
            }

            if (!File.Exists(resolved.FullPath))
            {
                continue;
            }

            var parsed = ParseFile(resolved.FullPath);
            if (parsed.Count > 0)
            {
                return parsed;
            }
        }

        return [];
    }

    public static IReadOnlyList<SkipSegment> ParseFile(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length > MaxFileBytes)
        {
            return [];
        }

        try
        {
            var text = File.ReadAllText(path);
            return ParseJson(text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyList<SkipSegment> ParseJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return ParseJsonCore(json);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<SkipSegment> ParseJsonCore(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var found = new List<SkipSegment>();
        if (root.TryGetProperty("segments", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                if (TryReadSegment(item, kindHint: null, out var segment))
                {
                    found.Add(segment);
                }
            }
        }

        TryAddKind(root, "intro", SkipKind.Intro, found);
        TryAddKind(root, "recap", SkipKind.Recap, found);
        TryAddKind(root, "credits", SkipKind.Credits, found);
        return found;
    }

    private static void TryAddKind(JsonElement root, string name, SkipKind kind, List<SkipSegment> found)
    {
        if (!root.TryGetProperty(name, out var node))
        {
            return;
        }

        if (TryReadSegment(node, kind, out var segment))
        {
            found.Add(segment);
        }
    }

    private static bool TryReadSegment(JsonElement node, SkipKind? kindHint, out SkipSegment segment)
    {
        segment = new SkipSegment(SkipKind.Intro, 0, 0, SkipSource.Marker);
        var kind = kindHint;
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("kind", out var kindNode) && kindNode.ValueKind == JsonValueKind.String)
            {
                kind = ChapterAliases.Classify(kindNode.GetString()) ?? ParseKindName(kindNode.GetString());
            }

            if (kind is null)
            {
                return false;
            }

            if (!TryReadSeconds(node, "start", out var start) || !TryReadSeconds(node, "end", out var end))
            {
                return false;
            }

            if (end <= start)
            {
                return false;
            }

            segment = new SkipSegment(kind.Value, start, end, SkipSource.Marker);
            return true;
        }

        if (node.ValueKind == JsonValueKind.Array && kindHint is { } hinted)
        {
            var values = new List<double>();
            foreach (var item in node.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var value))
                {
                    values.Add(value);
                }
                else if (item.ValueKind == JsonValueKind.String && TryParseSeconds(item.GetString(), out var parsed))
                {
                    values.Add(parsed);
                }
            }

            if (values.Count >= 2 && values[1] > values[0])
            {
                segment = new SkipSegment(hinted, values[0], values[1], SkipSource.Marker);
                return true;
            }
        }

        return false;
    }

    private static SkipKind? ParseKindName(string? value)
        => ChapterAliases.Normalize(value) switch
        {
            "intro" => SkipKind.Intro,
            "recap" => SkipKind.Recap,
            "credits" => SkipKind.Credits,
            _ => ChapterAliases.Classify(value)
        };

    private static bool TryReadSeconds(JsonElement node, string name, out double seconds)
    {
        seconds = 0;
        if (!node.TryGetProperty(name, out var value))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out seconds))
        {
            return true;
        }

        return value.ValueKind == JsonValueKind.String && TryParseSeconds(value.GetString(), out seconds);
    }

    private static bool TryParseSeconds(string? text, out double seconds)
    {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
        {
            return true;
        }

        var formats = new[] { @"hh\:mm\:ss", @"h\:mm\:ss", @"mm\:ss" };
        if (TimeSpan.TryParseExact(text.Trim(), formats, CultureInfo.InvariantCulture, out var span))
        {
            seconds = span.TotalSeconds;
            return true;
        }

        return TimeSpan.TryParse(text.Trim(), CultureInfo.InvariantCulture, out span)
               && (seconds = span.TotalSeconds) >= 0;
    }
}
