using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Safety;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Series;

public sealed record SeriesTreeNode(
    string Label,
    string Kind,
    string Path,
    bool Selected,
    IReadOnlyList<SeriesTreeNode> Children);

/// <summary>
/// C v2 master-detail: tree is 작품 → S01/S02; table is the selected season's episodes.
/// Old sequential show/season/episode table drill is discarded.
/// </summary>
public sealed class SeriesDrillDown
{
    private readonly List<SeriesShow> _shows = [];
    private SeriesShow? _show;
    private SeriesSeason? _season;
    private string? _preferredRoot;

    public SeriesDrillLevel Level { get; private set; } = SeriesDrillLevel.Shows;
    public IReadOnlyList<SeriesShow> Shows => _shows;
    public SeriesShow? Show => _show;
    public SeriesSeason? Season => _season;
    public string SortLockedLabel => SeriesScanner.SortLockedLabel;

    public void ReplaceShows(IEnumerable<SeriesShow> shows)
    {
        _shows.Clear();
        _shows.AddRange(shows);
        ApplyPreferredOrFirst();
    }

    public void AddOrUpdate(SeriesShow show)
    {
        _shows.RemoveAll(s => string.Equals(s.RootPath, show.RootPath, PathValidator.PathComparison));
        _shows.Insert(0, show);
        _preferredRoot = show.RootPath;
        SelectShow(show);
    }

    public void OpenShow(SeriesShow show)
    {
        SelectShow(show);
    }

    public void OpenSeason(SeriesSeason season)
    {
        if (_show is { } show)
        {
            var match = show.Seasons.FirstOrDefault(s =>
                string.Equals(s.FolderPath, season.FolderPath, PathValidator.PathComparison));
            if (match is not null)
            {
                SelectSeason(show, match);
                return;
            }
        }

        var owner = _shows.FirstOrDefault(s =>
            s.Seasons.Any(x => string.Equals(x.FolderPath, season.FolderPath, PathValidator.PathComparison)));
        if (owner is not null)
        {
            var match = owner.Seasons.First(s =>
                string.Equals(s.FolderPath, season.FolderPath, PathValidator.PathComparison));
            SelectSeason(owner, match);
        }
    }

    public void SelectSeason(SeriesShow show, SeriesSeason season)
    {
        var existing = _shows.FirstOrDefault(s =>
            string.Equals(s.RootPath, show.RootPath, PathValidator.PathComparison));
        if (existing is null)
        {
            AddOrUpdate(show);
            existing = _shows.FirstOrDefault(s =>
                string.Equals(s.RootPath, show.RootPath, PathValidator.PathComparison)) ?? show;
        }

        var match = existing.Seasons.FirstOrDefault(s =>
            string.Equals(s.FolderPath, season.FolderPath, PathValidator.PathComparison)) ?? season;
        _show = existing;
        _season = match;
        Level = SeriesDrillLevel.Episodes;
        _preferredRoot = existing.RootPath;
    }

    public bool Back()
    {
        // C v2 tree stays put; page Back returns to the player.
        return false;
    }

    public IReadOnlyList<SeriesTreeNode> Tree()
        => _shows.Select(show => new SeriesTreeNode(
            show.Name,
            "show",
            show.RootPath,
            Selected: false,
            Children: show.Seasons
                .Select(season => new SeriesTreeNode(
                    season.Name,
                    "season",
                    season.FolderPath,
                    Selected: _season is not null
                              && string.Equals(_season.FolderPath, season.FolderPath, PathValidator.PathComparison),
                    Children: []))
                .ToList()))
            .ToList();

    public IReadOnlyList<SeriesListItem> ListItems(ResumeStore resume, string? currentPath)
    {
        if (_season is null)
        {
            return [];
        }

        return _season.Episodes
            .OrderBy(e => e.SortKey)
            .Select(e =>
            {
                var saved = resume.Find(e.Path, e.Size);
                var current = currentPath is not null
                              && string.Equals(currentPath, e.Path, PathValidator.PathComparison);
                return new SeriesListItem(
                    EpisodeParser.EpisodeLabel(e.SortKey),
                    SeriesScanner.EpisodeTitle(e.FileName),
                    SeriesScanner.ProgressMark(saved),
                    e.Path,
                    e.Size,
                    current ? "episode-current" : "episode");
            })
            .ToList();
    }

    public string Heading() => UiCopy.SeriesPanel;

    public string FooterLeft()
        => _season is null
            ? ""
            : SeriesScanner.FooterSeason(_season.Name, _season.Episodes.Count);

    public string FooterRight() => SeriesScanner.SortLockedLabel;

    private void SelectShow(SeriesShow show)
    {
        _show = show;
        _preferredRoot = show.RootPath;
        if (show.Seasons.Count == 0)
        {
            _season = null;
            Level = SeriesDrillLevel.Seasons;
            return;
        }

        var keep = _season is not null
            ? show.Seasons.FirstOrDefault(s =>
                string.Equals(s.FolderPath, _season.FolderPath, PathValidator.PathComparison))
            : null;
        SelectSeason(show, keep ?? show.Seasons[0]);
    }

    private void ApplyPreferredOrFirst()
    {
        if (_shows.Count == 0)
        {
            _show = null;
            _season = null;
            Level = SeriesDrillLevel.Shows;
            return;
        }

        var preferred = _preferredRoot is not null
            ? _shows.FirstOrDefault(s => string.Equals(s.RootPath, _preferredRoot, PathValidator.PathComparison))
            : null;
        preferred ??= _show is not null
            ? _shows.FirstOrDefault(s => string.Equals(s.RootPath, _show.RootPath, PathValidator.PathComparison))
            : null;
        preferred ??= _shows[0];
        SelectShow(preferred);
    }
}
