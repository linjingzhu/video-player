using System.Text.RegularExpressions;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Series;

public sealed record EpisodeSortKey(int Season, int Episode, string FileName)
    : IComparable<EpisodeSortKey>
{
    public int CompareTo(EpisodeSortKey? other)
    {
        if (other is null)
        {
            return 1;
        }

        var season = Season.CompareTo(other.Season);
        if (season != 0)
        {
            return season;
        }

        var episode = Episode.CompareTo(other.Episode);
        if (episode != 0)
        {
            return episode;
        }

        return string.Compare(FileName, other.FileName, StringComparison.OrdinalIgnoreCase);
    }
}

public static partial class EpisodeParser
{
    [GeneratedRegex(@"[Ss](?<season>\d{1,2})[Ee](?<episode>\d{1,3})", RegexOptions.CultureInvariant)]
    private static partial Regex SeasonEpisodeRegex();

    [GeneratedRegex(@"(?:^|[^\d])(?<episode>\d{1,4})(?:[^\d]|$)", RegexOptions.CultureInvariant)]
    private static partial Regex NumericRegex();

    [GeneratedRegex(@"^(?:[Ss](?:eason)?\s*)?(?<season>\d{1,2})$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex SeasonFolderRegex();

    [GeneratedRegex(@"시즌\s*(?<season>\d{1,2})", RegexOptions.CultureInvariant)]
    private static partial Regex KoreanSeasonRegex();

    public static EpisodeSortKey Parse(string fileName, int fallbackSeason = 1)
    {
        var safeName = FileNameSanitizer.ForDisplay(Path.GetFileName(fileName));
        var match = SeasonEpisodeRegex().Match(fileName);
        if (match.Success
            && int.TryParse(match.Groups["season"].Value, out var season)
            && int.TryParse(match.Groups["episode"].Value, out var episode))
        {
            return new EpisodeSortKey(season, episode, safeName);
        }

        var numeric = NumericRegex().Match(Path.GetFileNameWithoutExtension(fileName));
        if (numeric.Success && int.TryParse(numeric.Groups["episode"].Value, out var onlyEpisode))
        {
            return new EpisodeSortKey(fallbackSeason, onlyEpisode, safeName);
        }

        return new EpisodeSortKey(fallbackSeason, int.MaxValue, safeName);
    }

    public static int ParseSeasonFolder(string folderName, int fallback = 1)
    {
        var name = Path.GetFileName(folderName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(name))
        {
            return fallback;
        }

        var korean = KoreanSeasonRegex().Match(name);
        if (korean.Success && int.TryParse(korean.Groups["season"].Value, out var k))
        {
            return k;
        }

        var ascii = SeasonFolderRegex().Match(name);
        if (ascii.Success && int.TryParse(ascii.Groups["season"].Value, out var s))
        {
            return s;
        }

        return fallback;
    }

    public static string EpisodeLabel(EpisodeSortKey key)
        => key.Episode == int.MaxValue ? "-" : $"E{key.Episode:00}";
}
