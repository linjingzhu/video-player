using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Series;

public sealed class SeriesDrillDown
{
    private readonly List<SeriesShow> _shows = [];
    private SeriesShow? _show;
    private SeriesSeason? _season;

    public Shell.SeriesDrillLevel Level { get; private set; } = Shell.SeriesDrillLevel.Shows;
    public IReadOnlyList<SeriesShow> Shows => _shows;
    public SeriesShow? Show => _show;
    public SeriesSeason? Season => _season;

    public void ReplaceShows(IEnumerable<SeriesShow> shows)
    {
        _shows.Clear();
        _shows.AddRange(shows);
        _show = null;
        _season = null;
        Level = Shell.SeriesDrillLevel.Shows;
    }

    public void AddOrUpdate(SeriesShow show)
    {
        _shows.RemoveAll(s => string.Equals(s.RootPath, show.RootPath, PathValidator.PathComparison));
        _shows.Insert(0, show);
    }

    public void OpenShow(SeriesShow show)
    {
        _show = show;
        _season = null;
        Level = Shell.SeriesDrillLevel.Seasons;
    }

    public void OpenSeason(SeriesSeason season)
    {
        _season = season;
        Level = Shell.SeriesDrillLevel.Episodes;
    }

    public bool Back()
    {
        if (Level == Shell.SeriesDrillLevel.Episodes)
        {
            _season = null;
            Level = Shell.SeriesDrillLevel.Seasons;
            return true;
        }

        if (Level == Shell.SeriesDrillLevel.Seasons)
        {
            _show = null;
            Level = Shell.SeriesDrillLevel.Shows;
            return true;
        }

        return false;
    }

    public IReadOnlyList<Shell.SeriesListItem> ListItems(ResumeStore resume, string? currentPath)
    {
        if (Level == Shell.SeriesDrillLevel.Shows)
        {
            return _shows.Select(s => new Shell.SeriesListItem("", s.Name, "", s.RootPath, 0, "show")).ToList();
        }

        if (Level == Shell.SeriesDrillLevel.Seasons && _show is not null)
        {
            return _show.Seasons
                .OrderBy(s => s.SeasonNumber)
                .Select(s => new Shell.SeriesListItem("", s.Name, "", s.FolderPath, 0, "season"))
                .ToList();
        }

        if (_season is null)
        {
            return [];
        }

        return _season.Episodes
            .OrderBy(e => e.SortKey)
            .Select(e =>
            {
                var saved = resume.Find(e.Path, e.Size);
                var progress = saved switch
                {
                    { Completed: true } => "✓",
                    { DurationSeconds: > 0 } entry => $"{Math.Clamp(entry.PositionSeconds / entry.DurationSeconds * 100, 0, 100):0}%",
                    _ => "-"
                };
                var current = currentPath is not null
                              && string.Equals(currentPath, e.Path, PathValidator.PathComparison);
                return new Shell.SeriesListItem(
                    EpisodeParser.EpisodeLabel(e.SortKey),
                    EpisodeParser.TitleFromFileName(e.FileName),
                    progress,
                    e.Path,
                    e.Size,
                    current ? "episode-current" : "episode");
            })
            .ToList();
    }

    public string Heading()
        => Level switch
        {
            Shell.SeriesDrillLevel.Seasons when _show is not null => _show.Name,
            Shell.SeriesDrillLevel.Episodes when _season is not null => _season.Name,
            _ => "시리즈"
        };
}
