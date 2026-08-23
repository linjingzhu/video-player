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
        Assert.Equal("S01", SeriesScanner.SeasonLabel(1));
        Assert.Equal("S02", SeriesScanner.SeasonLabel(2));
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
            Assert.Equal("S01", show.Seasons[0].Name);
            Assert.Equal(new[] { 2, 10 }, show.Seasons[0].Episodes.Select(e => e.SortKey.Episode));
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Scanner_tree_is_work_then_sxx_seasons()
    {
        var root = Directory.CreateTempSubdirectory("드라마-");
        try
        {
            WriteSeason(root.FullName, "S01", 2);
            WriteSeason(root.FullName, "Season 02", 8);
            File.WriteAllBytes(Path.Combine(root.FullName, "trailer.mkv"), [1]);

            var show = SeriesScanner.Scan(root.FullName);
            Assert.Equal(2, show.Seasons.Count);
            Assert.Equal(new[] { "S01", "S02" }, show.Seasons.Select(s => s.Name));
            Assert.Equal(8, show.Seasons[1].Episodes.Count);
            Assert.All(show.Seasons[1].Episodes, e => Assert.DoesNotContain(".mkv", SeriesScanner.EpisodeTitle(e.FileName)));
            Assert.Equal("S02E01", SeriesScanner.EpisodeTitle("S02E01.mkv"));
            Assert.Equal("S02E01", SeriesScanner.EpisodeTitle("S02E01"));
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void C_v2_tree_and_table_are_fixed_by_episode()
    {
        var root = Directory.CreateTempSubdirectory("드라마-");
        try
        {
            WriteSeason(root.FullName, "S01", 2);
            WriteSeason(root.FullName, "S02", 8);
            var show = SeriesScanner.Scan(root.FullName);
            var drill = new SeriesDrillDown();
            drill.ReplaceShows([show]);

            Assert.Equal(UiCopy.SeriesPanel, drill.Heading());
            Assert.Equal(SeriesDrillLevel.Episodes, drill.Level);
            Assert.Equal("S01", drill.Season?.Name);

            var tree = drill.Tree();
            Assert.Single(tree);
            Assert.Equal("show", tree[0].Kind);
            Assert.Contains("드라마", tree[0].Label, StringComparison.Ordinal);
            Assert.Equal(new[] { "S01", "S02" }, tree[0].Children.Select(c => c.Label));
            Assert.True(tree[0].Children[0].Selected);

            drill.OpenSeason(show.Seasons[1]);
            Assert.Equal("S02", drill.Season?.Name);
            Assert.Equal("S02 8화", drill.FooterLeft());
            Assert.Equal("정렬 회차 고정", drill.FooterRight());
            Assert.False(drill.Back());

            var resume = new ResumeStore();
            var first = show.Seasons[1].Episodes[0];
            var third = show.Seasons[1].Episodes[2];
            resume.Apply(CompletionPolicy.Checkpoint(new MediaIdentity(first.Path, first.Size), 99, 100));
            resume.Apply(CompletionPolicy.Checkpoint(new MediaIdentity(third.Path, third.Size), 58, 100));

            var items = drill.ListItems(resume, null);
            Assert.Equal(8, items.Count);
            Assert.Equal(items.Select(i => i.Path).Distinct().Count(), items.Count);
            Assert.Equal(Enumerable.Range(1, 8).Select(i => $"E{i:00}"), items.Select(i => i.Episode));
            Assert.Equal(Enumerable.Range(1, 8).Select(i => $"S02E{i:00}"), items.Select(i => i.Title));
            Assert.Equal("E03", items[2].Episode);
            Assert.NotEqual("S02E01", items[2].Episode);
            Assert.Equal("S02E03", items[2].Title);
            Assert.All(items, i => Assert.False(i.Title.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase)));
            Assert.Equal("✓", items[0].Progress);
            Assert.Equal("-", items[1].Progress);
            Assert.Equal("▶ 58%", items[2].Progress);
            Assert.All(items, i => Assert.Equal("episode", i.Kind));
            Assert.Equal("회차", UiCopy.ColumnEpisode);
            Assert.Equal("제목", UiCopy.ColumnTitle);
            Assert.Equal("진행", UiCopy.ColumnProgress);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Designer_c_v2_title_is_stem_and_tree_is_korean_work_to_sxx()
    {
        var root = Directory.CreateTempSubdirectory("series-c-v2-");
        try
        {
            var work = Path.Combine(root.FullName, "드라마");
            Directory.CreateDirectory(work);
            WriteSeason(work, "시즌 1", 1);
            WriteSeason(work, "Season 02", 2);

            var show = SeriesScanner.Scan(work);
            Assert.Equal("드라마", show.Name);
            Assert.Equal(new[] { "S01", "S02" }, show.Seasons.Select(s => s.Name));

            var drill = new SeriesDrillDown();
            drill.ReplaceShows([show]);
            drill.OpenSeason(show.Seasons[1]);
            var tree = drill.Tree();
            Assert.Equal("드라마", tree[0].Label);
            Assert.Equal(new[] { "S01", "S02" }, tree[0].Children.Select(c => c.Label));

            var items = drill.ListItems(new ResumeStore(), null);
            Assert.Equal(new[] { "S02E01", "S02E02" }, items.Select(i => i.Title));
            Assert.All(items, i => Assert.False(i.Title.Contains(".mkv", StringComparison.OrdinalIgnoreCase)));
            Assert.Equal("S02E01", SeriesScanner.EpisodeTitle("S02E01.mkv"));
            Assert.NotEqual("S02E01.mkv", SeriesScanner.EpisodeTitle("S02E01.mkv"));
            Assert.Equal(
                Path.GetFileNameWithoutExtension("Show.S02E01.Better.Name.mkv"),
                SeriesScanner.EpisodeTitle("Show.S02E01.Better.Name.mkv"));
            Assert.NotEqual(
                EpisodeParser.TitleFromFileName("Show.S02E01.Better.Name.mkv"),
                SeriesScanner.EpisodeTitle("Show.S02E01.Better.Name.mkv"));
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Table_title_is_stem_only_and_keeps_sxxexx()
    {
        var root = Directory.CreateTempSubdirectory("series-title-");
        try
        {
            var s02 = Path.Combine(root.FullName, "S02");
            Directory.CreateDirectory(s02);
            File.WriteAllBytes(Path.Combine(s02, "Show.S02E01.Better.Name.mkv"), [1]);
            var show = SeriesScanner.Scan(root.FullName);
            var drill = new SeriesDrillDown();
            drill.ReplaceShows([show]);
            var items = drill.ListItems(new ResumeStore(), null);
            Assert.Equal("Show.S02E01.Better.Name", items[0].Title);
            Assert.Equal(
                Path.GetFileNameWithoutExtension("Show.S02E01.Better.Name.mkv"),
                items[0].Title);
            Assert.NotEqual(
                EpisodeParser.TitleFromFileName("Show.S02E01.Better.Name.mkv"),
                items[0].Title);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Designer_skin_c_episode_is_e03_title_is_stem()
    {
        var root = Directory.CreateTempSubdirectory("드라마-");
        try
        {
            WriteSeason(root.FullName, "S02", 8);
            var show = SeriesScanner.Scan(root.FullName);
            var drill = new SeriesDrillDown();
            drill.ReplaceShows([show]);

            var items = drill.ListItems(new ResumeStore(), null);
            Assert.Equal(8, items.Count);
            Assert.Equal(items.Select(i => i.Episode).Distinct().Count(), items.Count);
            Assert.Equal("E03", items[2].Episode);
            Assert.NotEqual("S02E01", items[0].Episode);
            Assert.NotEqual("S02E01", items[2].Episode);
            Assert.Equal("S02E03", items[2].Title);
            Assert.Equal(Path.GetFileNameWithoutExtension(show.Seasons[0].Episodes[2].FileName), items[2].Title);
            Assert.Equal(UiCopy.Back, "뒤로");
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Progress_mark_is_check_play_percent_or_dash()
    {
        Assert.Equal("✓", SeriesScanner.ProgressMark(new ResumeEntry
        {
            Key = "k",
            Path = "/a",
            Size = 1,
            Completed = true,
            DurationSeconds = 100,
            PositionSeconds = 0
        }));
        Assert.Equal("▶ 58%", SeriesScanner.ProgressMark(new ResumeEntry
        {
            Key = "k",
            Path = "/a",
            Size = 1,
            Completed = false,
            DurationSeconds = 100,
            PositionSeconds = 58
        }));
        Assert.Equal("-", SeriesScanner.ProgressMark(null));
    }

    private static string WriteSeason(string root, string folder, int count)
    {
        var dir = Path.Combine(root, folder);
        Directory.CreateDirectory(dir);
        var season = EpisodeParser.ParseSeasonFolder(folder);
        for (var i = 1; i <= count; i++)
        {
            File.WriteAllBytes(Path.Combine(dir, $"S{season:00}E{i:00}.mkv"), [(byte)i]);
        }

        return dir;
    }
}
