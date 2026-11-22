using VideoPlayer.Core.Playback;

namespace VideoPlayer.Tests;

public class LoopRangeTests
{
    [Fact]
    public void Missing_marks_disable_the_item_and_a_span_rewinds_at_the_out_point()
    {
        Assert.False(LoopRange.HasBothMarks(null, 10));
        Assert.False(LoopRange.HasBothMarks(1, null));
        Assert.Equal(LoopRange.NeedsMarksHeader, "I/O 먼저");
        Assert.Equal("L", LoopRange.Key);
        Assert.Equal(0.25, LoopRange.BandOpacity);

        Assert.True(LoopRange.TrySpan(30, 10, out var start, out var end));
        Assert.Equal(10, start);
        Assert.Equal(30, end);
        Assert.Null(LoopRange.RewindTarget(true, 29.5, 10, 30));
        Assert.Equal(10, LoopRange.RewindTarget(true, 40, 10, 40));
        Assert.Null(LoopRange.RewindTarget(false, 40, 10, 30));
        Assert.Null(LoopRange.RewindTarget(true, 40, 10, 10));
    }

    [Fact]
    public void Band_shows_between_marks_and_hides_while_clip_handles_are_visible()
    {
        Assert.True(LoopRange.ShowBand(true, 5, 20, sheetHandlesVisible: false));
        Assert.False(LoopRange.ShowBand(true, 5, 20, sheetHandlesVisible: true));
        Assert.False(LoopRange.ShowBand(false, 5, 20, sheetHandlesVisible: false));
        Assert.False(LoopRange.ShowBand(true, null, 20, sheetHandlesVisible: false));
    }

    [Fact]
    public void Session_loops_between_marks_and_turns_off_when_a_mark_disappears()
    {
        using var workspace = new TempWorkspace();
        var engine = new FakeMediaEngine { Duration = 100 };
        var session = new PlaybackSession(engine, workspace.Data);
        var file = workspace.File("episode.mkv", [1]);
        session.Open(file);

        Assert.False(session.LoopMenuEnabled);
        Assert.Equal("I/O 먼저", session.LoopMenuHeader);
        Assert.False(session.ToggleLoopRange());

        engine.Seek(12);
        session.SetInMark();
        engine.Seek(48);
        session.SetOutMark();
        Assert.True(session.LoopMenuEnabled);
        Assert.Equal("구간 반복", session.LoopMenuHeader);
        Assert.True(session.ToggleLoopRange());
        Assert.True(session.LoopEnabled);

        engine.Seek(48);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.Equal(12, engine.Position, 3);

        engine.Seek(20);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.Equal(20, engine.Position, 3);

        session.Shell.Clip.OutMark = null;
        session.SyncLoopAvailability();
        Assert.False(session.LoopEnabled);
        Assert.False(session.LoopMenuEnabled);

        session.Shell.Clip.OutMark = 48;
        session.ToggleLoopRange();
        session.OpenClipSheet();
        Assert.True(session.Shell.Clip.ShowStartHandle);
        Assert.False(LoopRange.ShowBand(
            session.LoopEnabled,
            session.Shell.Clip.InMark,
            session.Shell.Clip.OutMark,
            session.Shell.Clip.ShowStartHandle || session.Shell.Clip.ShowEndHandle));
    }
}
