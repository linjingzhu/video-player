using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Series;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class SeriesSortTests
{
    [Fact]
    public void SxxExx_sorts_by_season_then_episode()
    {
        var names = new[] { "Show.S01E10.mkv", "Show.S01E02.mkv", "Show.S02E01.mkv", "Show.S01E3.mkv" };
        var sorted = names.Select(n => EpisodeParser.Parse(n)).OrderBy(k => k).Select(k => k.Episode).ToArray();
        Assert.Equal(new[] { 2, 3, 10, 1 }, sorted);
        Assert.Equal(new[] { 1, 1, 1, 2 }, names.Select(n => EpisodeParser.Parse(n)).OrderBy(k => k).Select(k => k.Season).ToArray());
    }

    [Fact]
    public void Numeric_filenames_sort_naturally()
    {
        var names = new[] { "10.mkv", "2.mkv", "1.mkv" };
        var sorted = names.Select(n => EpisodeParser.Parse(n, 1)).OrderBy(k => k).Select(k => k.Episode).ToArray();
        Assert.Equal(new[] { 1, 2, 10 }, sorted);
    }

    [Fact]
    public void Folder_name_is_the_season()
    {
        Assert.Equal(1, EpisodeParser.ParseSeasonFolder("S01"));
        Assert.Equal(2, EpisodeParser.ParseSeasonFolder("Season 02"));
        Assert.Equal(3, EpisodeParser.ParseSeasonFolder("시즌 3"));
    }

    [Fact]
    public void Weird_episode_names_still_get_a_stable_sort_key()
    {
        var weird = new[]
        {
            "",
            "....mkv",
            "S01E01.S02E03.mkv",
            "에피소드 없음.mkv",
            new string('가', 300) + ".mkv",
            "S01E01 - .. hidden.mkv"
        };

        var keys = weird.Select(name => EpisodeParser.Parse(name)).ToList();
        var ordered = keys.OrderBy(k => k).ToList();
        Assert.Equal(keys.Count, ordered.Count);
        Assert.Equal(1, EpisodeParser.Parse("S01E01.S02E03.mkv").Season);
        Assert.Equal(1, EpisodeParser.Parse("S01E01.S02E03.mkv").Episode);
        Assert.DoesNotContain('/', ordered[0].FileName);
    }

    [Fact]
    public void Scanner_sorts_mixed_folder_as_season()
    {
        var root = Directory.CreateTempSubdirectory("series-sort-");
        try
        {
            File.WriteAllBytes(Path.Combine(root.FullName, "S01E10.mkv"), [1]);
            File.WriteAllBytes(Path.Combine(root.FullName, "S01E02.mkv"), [1]);
            File.WriteAllBytes(Path.Combine(root.FullName, "readme.txt"), [1]);
            var show = SeriesScanner.Scan(root.FullName);
            Assert.Single(show.Seasons);
            Assert.Equal(new[] { 2, 10 }, show.Seasons[0].Episodes.Select(e => e.SortKey.Episode));
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Drill_down_is_show_then_season_then_episode_sorted_by_episode()
    {
        var root = Directory.CreateTempSubdirectory("series-drill-");
        try
        {
            var s01 = Path.Combine(root.FullName, "S01");
            Directory.CreateDirectory(s01);
            File.WriteAllBytes(Path.Combine(s01, "Show.S01E10.Title.Ten.mkv"), [1]);
            File.WriteAllBytes(Path.Combine(s01, "Show.S01E02.Title.Two.mkv"), [1]);
            var show = SeriesScanner.Scan(root.FullName);
            var drill = new SeriesDrillDown();
            drill.ReplaceShows([show]);
            Assert.Equal(SeriesDrillLevel.Shows, drill.Level);
            drill.OpenShow(show);
            Assert.Equal(SeriesDrillLevel.Seasons, drill.Level);
            drill.OpenSeason(show.Seasons[0]);
            var items = drill.ListItems(new ResumeStore(), null);
            Assert.Equal(new[] { "E02", "E10" }, items.Select(i => i.Episode));
            Assert.Equal("회차", UiCopy.ColumnEpisode);
            Assert.Equal("제목", UiCopy.ColumnTitle);
            Assert.Equal("진행", UiCopy.ColumnProgress);
            Assert.DoesNotContain(items, i => i.Title.Contains("S01E", StringComparison.Ordinal));
        }
        finally
        {
            root.Delete(true);
        }
    }
}
