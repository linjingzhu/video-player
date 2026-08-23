using VideoPlayer.Core.Media;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Series;

public sealed record SeriesEpisode(
    string Path,
    long Size,
    string FileName,
    EpisodeSortKey SortKey,
    string SeasonFolder);

public sealed record SeriesSeason(string FolderPath, string Name, int SeasonNumber, IReadOnlyList<SeriesEpisode> Episodes);

public sealed record SeriesShow(string RootPath, string Name, IReadOnlyList<SeriesSeason> Seasons);

/// <summary>C v2 series scan: 작품 folder → S01/S02 seasons. Episode sort is fixed.</summary>
public static class SeriesScanner
{
    public const string SortLockedLabel = "정렬 회차 고정";

    public static IReadOnlyList<SeriesEpisode> SortEpisodes(IEnumerable<SeriesEpisode> episodes)
        => episodes.OrderBy(e => e.SortKey).ToList();

    public static string SeasonLabel(int seasonNumber)
        => $"S{Math.Max(0, seasonNumber):00}";

    public static string EpisodeTitle(string fileName)
        => FileNameSanitizer.ForDisplay(Path.GetFileNameWithoutExtension(fileName));

    public static string ProgressMark(ResumeEntry? saved)
        => saved switch
        {
            { Completed: true } => "✓",
            { DurationSeconds: > 0 } entry
                => $"▶ {Math.Clamp(entry.PositionSeconds / entry.DurationSeconds * 100, 0, 100):0}%",
            _ => "-"
        };

    public static string FooterSeason(string seasonName, int episodeCount)
        => $"{seasonName} {episodeCount}화";

    public static SeriesShow Scan(string folderPath, Func<string, long>? sizeProvider = null)
    {
        var validated = PathValidator.ValidateLocalFilePath(folderPath);
        if (!validated.Success || validated.FullPath is null)
        {
            throw new InvalidOperationException(validated.Error ?? "폴더를 열 수 없습니다.");
        }

        var root = validated.FullPath;
        var rootName = FileNameSanitizer.ForDisplay(Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar)));
        var seasons = new List<SeriesSeason>();

        IEnumerable<string> subdirs;
        try
        {
            subdirs = Directory.Exists(root)
                ? Directory.EnumerateDirectories(root)
                : [];
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            subdirs = [];
        }

        foreach (var dir in subdirs.OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var seasonNumber = EpisodeParser.ParseSeasonFolder(Path.GetFileName(dir));
            var episodes = EnumerateVideos(dir, sizeProvider, seasonNumber);
            if (episodes.Count == 0)
            {
                continue;
            }

            seasons.Add(new SeriesSeason(
                dir,
                SeasonLabel(seasonNumber),
                seasonNumber,
                SortEpisodes(episodes)));
        }

        if (seasons.Count == 0)
        {
            var rootSeasonNumber = EpisodeParser.ParseSeasonFolder(Path.GetFileName(root));
            var rootVideos = EnumerateVideos(root, sizeProvider, rootSeasonNumber);
            if (rootVideos.Count > 0)
            {
                seasons.Add(new SeriesSeason(
                    root,
                    SeasonLabel(rootSeasonNumber),
                    rootSeasonNumber,
                    SortEpisodes(rootVideos)));
            }
        }

        seasons = [.. seasons.OrderBy(s => s.SeasonNumber).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
        return new SeriesShow(root, rootName, seasons);
    }

    public static MediaIdentity? NextEpisode(IReadOnlyList<SeriesEpisode> ordered, string currentPath)
        => AdjacentEpisode(ordered, currentPath, +1);

    public static MediaIdentity? PreviousEpisode(IReadOnlyList<SeriesEpisode> ordered, string currentPath)
        => AdjacentEpisode(ordered, currentPath, -1);

    private static MediaIdentity? AdjacentEpisode(IReadOnlyList<SeriesEpisode> ordered, string currentPath, int offset)
    {
        for (var i = 0; i < ordered.Count; i++)
        {
            if (!string.Equals(ordered[i].Path, currentPath, PathValidator.PathComparison))
            {
                continue;
            }

            var target = i + offset;
            if (target < 0 || target >= ordered.Count)
            {
                return null;
            }

            var episode = ordered[target];
            return new MediaIdentity(episode.Path, episode.Size);
        }

        return null;
    }

    private static List<SeriesEpisode> EnumerateVideos(string folder, Func<string, long>? sizeProvider, int season)
    {
        var list = new List<SeriesEpisode>();
        IEnumerable<string> files;
        try
        {
            if (!Directory.Exists(folder))
            {
                return list;
            }

            files = Directory.EnumerateFiles(folder);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return list;
        }

        foreach (var file in files)
        {
            if (!SupportedFormats.IsSupportedContainer(file) || SupportedFormats.IsOutOfScopeContainer(file))
            {
                continue;
            }

            var check = PathValidator.ValidateLocalFilePath(file);
            if (!check.Success || check.FullPath is null)
            {
                continue;
            }

            if (!PathValidator.IsSameDirectory(check.FullPath, Path.Combine(folder, "placeholder.mkv")))
            {
                continue;
            }

            long size;
            try
            {
                size = sizeProvider?.Invoke(check.FullPath) ?? new FileInfo(check.FullPath).Length;
            }
            catch (IOException)
            {
                continue;
            }

            var name = Path.GetFileName(check.FullPath);
            list.Add(new SeriesEpisode(
                check.FullPath,
                size,
                FileNameSanitizer.ForDisplay(name),
                EpisodeParser.Parse(name, season),
                SeasonLabel(season)));
        }

        return list;
    }
}
