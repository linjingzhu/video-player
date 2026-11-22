using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.Tests;

public class RecentFilesMenuTests
{
    [Fact]
    public void Menu_keeps_ten_local_stems_and_marks_missing_files()
    {
        using var workspace = new TempWorkspace();
        var present = workspace.File("Show.mkv", [1]);
        var items = new List<RecentItem>
        {
            new("https://example.com/a.mp4", 1, "url", DateTimeOffset.UnixEpoch),
            new(@"\\server\share\clip.mkv", 1, "unc", DateTimeOffset.UnixEpoch),
            new(present, 1, "show", DateTimeOffset.UnixEpoch),
            new(Path.Combine(workspace.Root, "gone.mkv"), 1, "gone", DateTimeOffset.UnixEpoch)
        };
        for (var i = 0; i < 12; i++)
        {
            items.Add(new(workspace.File($"extra-{i:00}.mp4", [2]), 2, "extra", DateTimeOffset.UnixEpoch));
        }

        var menu = RecentFilesMenu.Build(items, File.Exists);
        Assert.Equal(10, menu.Count);
        Assert.Equal("Show", menu[0].Label);
        Assert.False(menu[0].Missing);
        Assert.Equal(RecentFilesMenu.MissingLabel, menu[1].Label);
        Assert.True(menu[1].Missing);
        Assert.DoesNotContain(menu, entry => entry.Path.StartsWith("https:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(menu, entry => entry.Path.StartsWith(@"\\", StringComparison.Ordinal));
        Assert.Equal("목록 비우기", RecentFilesMenu.ClearLabel);
    }

    [Fact]
    public void Opening_a_missing_recent_file_removes_it_and_clear_empties_the_store()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        var kept = workspace.File("kept.mkv", [1]);
        var dropped = Path.Combine(workspace.Root, "dropped.mkv");
        Assert.True(session.Recent.TryAdd(kept, 1, "h264", "aac"));
        Assert.True(session.Recent.TryAdd(dropped, 1, "h264", "aac"));
        session.RemoveRecentFile(dropped);
        Assert.DoesNotContain(session.Recent.Items, item => item.Path == dropped);

        var reopened = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Contains(reopened.Recent.Items, item => item.Path == kept);
        reopened.ClearRecentFiles();
        Assert.Empty(reopened.Recent.Items);

        var again = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Empty(again.Recent.Items);
        again.OpenUrl("https://example.com/movie.mp4");
        Assert.Empty(again.Recent.Items);
    }
}
