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

public static class SeriesScanner
{
    public static IReadOnlyList<SeriesEpisode> SortEpisodes(IEnumerable<SeriesEpisode> episodes)
        => episodes.OrderBy(e => e.SortKey).ToList();

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

        var rootVideos = EnumerateVideos(root, sizeProvider, EpisodeParser.ParseSeasonFolder(Path.GetFileName(root)));
        if (rootVideos.Count > 0)
        {
            seasons.Add(new SeriesSeason(root, Path.GetFileName(root), EpisodeParser.ParseSeasonFolder(Path.GetFileName(root)), SortEpisodes(rootVideos)));
        }

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
                FileNameSanitizer.ForDisplay(Path.GetFileName(dir)),
                seasonNumber,
                SortEpisodes(episodes)));
        }

        seasons = [.. seasons.OrderBy(s => s.SeasonNumber).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
        return new SeriesShow(root, rootName, seasons);
    }

    public static MediaIdentity? NextEpisode(IReadOnlyList<SeriesEpisode> ordered, string currentPath)
    {
        for (var i = 0; i < ordered.Count; i++)
        {
            if (string.Equals(ordered[i].Path, currentPath, PathValidator.PathComparison)
                && i + 1 < ordered.Count)
            {
                var next = ordered[i + 1];
                return new MediaIdentity(next.Path, next.Size);
            }
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
                Path.GetFileName(folder)));
        }

        return list;
    }
}
