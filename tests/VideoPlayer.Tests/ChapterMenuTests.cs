using VideoPlayer.Core.Playback;

namespace VideoPlayer.Tests;

public class ChapterMenuTests
{
    [Fact]
    public void Empty_chapter_list_is_a_single_disabled_row()
    {
        var empty = ChapterMenu.Build([]);
        Assert.Single(empty);
        Assert.Equal("챕터 없음", empty[0].Label);
        Assert.False(empty[0].Enabled);
        Assert.Null(empty[0].Start);
        Assert.Equal("챕터", ChapterMenu.MenuHeader);
    }

    [Fact]
    public void Chapters_seek_to_the_stream_start_and_keep_their_titles()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine { Duration = 200 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(workspace.File("movie.mkv", [1]));
        engine.Chapters =
        [
            new MediaChapter("  ", 0, 12),
            new MediaChapter("본편", 12, 180)
        ];
        session.Tick(DateTimeOffset.UtcNow);

        var menu = ChapterMenu.Build(session.Chapters);
        Assert.Equal("챕터 1", menu[0].Label);
        Assert.Equal(0, menu[0].Start);
        Assert.Equal("본편", menu[1].Label);
        Assert.True(menu[1].Enabled);

        session.SeekAbsolute(menu[1].Start!.Value);
        Assert.Equal(12, engine.Position, 3);
    }
}
