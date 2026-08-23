using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;
using VideoPlayer.Core.Skip;

namespace VideoPlayer.Tests;

public class SkipCapsuleTests
{
    [Fact]
    public void Capsule_is_bottom_right_overlay_never_on_transport()
    {
        var skip = PlayerShell.Boot().Skip;
        Assert.False(skip.Visible);
        Assert.True(skip.OverlayOnly);
        Assert.False(skip.OnTransport);
        Assert.Equal(OverlayAnchor.BottomRight, skip.Anchor);
        Assert.False(skip.UsesExternalDatabase);
        Assert.Equal("#141418", skip.PanelColor);
        Assert.Equal("#0B0B0D", skip.BackgroundColor);
        Assert.Equal("#0A84FF", skip.AccentColor);
        Assert.Equal(SkinA.Panel, skip.PanelColor);
        Assert.Equal(SkinA.Accent, skip.AccentColor);
        Assert.Equal(999, skip.CapsuleRadius);
        Assert.DoesNotContain(PlayerShell.Boot().Transport.Order, control => control.ToString().Contains("Intro", StringComparison.Ordinal));
        Assert.Equal("인트로 건너뛰기", UiCopy.SkipIntro);
        Assert.Equal("리캡 건너뛰기", UiCopy.SkipRecap);
        Assert.Equal("크레딧 건너뛰기", UiCopy.SkipCredits);
        Assert.Equal("취소 ({0})", UiCopy.SkipCancelCountdown);
        Assert.True(SkinA.NoMockCaptionSentences);
    }

    [Theory]
    [InlineData("Opening", SkipKind.Intro)]
    [InlineData("INTRO", SkipKind.Intro)]
    [InlineData("오프닝 크레딧", SkipKind.Intro)]
    [InlineData("인트로", SkipKind.Intro)]
    [InlineData("Previously On", SkipKind.Recap)]
    [InlineData("리캡", SkipKind.Recap)]
    [InlineData("지난 이야기", SkipKind.Recap)]
    [InlineData("End Credits", SkipKind.Credits)]
    [InlineData("엔딩 크레딧", SkipKind.Credits)]
    [InlineData("크레딧", SkipKind.Credits)]
    [InlineData("Scene 04", null)]
    [InlineData("", null)]
    public void Chapter_aliases_map_known_titles_only(string title, SkipKind? expected)
        => Assert.Equal(expected, ChapterAliases.Classify(title));

    [Fact]
    public void Opening_credits_is_intro_not_credits()
        => Assert.Equal(SkipKind.Intro, ChapterAliases.Classify("Opening Credits"));

    [Fact]
    public void Detector_uses_chapters_and_markers_and_prefers_markers()
    {
        var chapters = new[]
        {
            new MediaChapter("Opening", 0, 80),
            new MediaChapter("Episode", 80, 2400),
            new MediaChapter("Credits", 2400, 2700)
        };
        var markers = new[] { new SkipSegment(SkipKind.Intro, 0, 92, SkipSource.Marker) };
        var detected = SkipDetector.Detect(chapters, markers, 2700);

        Assert.Equal(2, detected.Count);
        Assert.Equal(SkipKind.Intro, detected[0].Kind);
        Assert.Equal(SkipSource.Marker, detected[0].Source);
        Assert.Equal(92, detected[0].End);
        Assert.Equal(SkipKind.Credits, detected[1].Kind);
        Assert.Equal(SkipSource.Chapter, detected[1].Source);
    }

    [Fact]
    public void Season_marker_json_loads_from_the_episode_folder()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var video = Path.Combine(season, "S01E01.mkv");
        File.WriteAllBytes(video, [1]);
        File.WriteAllText(Path.Combine(season, "skip-markers.json"), """
            { "intro": [0, 90], "recap": { "start": 0, "end": 40 }, "credits": { "start": 2400, "end": 2700 } }
            """);

        var loaded = SeasonSkipMarkers.Load(video);
        Assert.Contains(loaded, item => item is { Kind: SkipKind.Intro, Start: 0, End: 90, Source: SkipSource.Marker });
        Assert.Contains(loaded, item => item is { Kind: SkipKind.Recap, End: 40, Source: SkipSource.Marker });
        Assert.Contains(loaded, item => item is { Kind: SkipKind.Credits, Start: 2400, Source: SkipSource.Marker });
    }

    [Fact]
    public void Traversal_marker_names_are_ignored()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("ep.mkv", [1]);
        Assert.Empty(SeasonSkipMarkers.Load(Path.Combine(workspace.Root, "..", "ep.mkv")));
        Assert.Empty(SeasonSkipMarkers.ParseJson("{ not json"));
    }

    [Fact]
    public void Chapter_intro_shows_one_line_capsule_without_auto()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("show.mkv", [1]);
        var engine = new FakeMediaEngine
        {
            Duration = 600,
            Chapters =
            [
                new MediaChapter("인트로", 0, 0),
                new MediaChapter("본편", 75, 0)
            ]
        };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.Tick(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        Assert.True(session.Shell.Skip.Visible);
        Assert.Equal(SkipKind.Intro, session.Shell.Skip.Kind);
        Assert.Equal("인트로 건너뛰기", session.Shell.Skip.Label);
        Assert.False(session.Shell.Skip.AutoPending);
        Assert.False(session.Shell.Skip.TwoLine);
        Assert.Equal(SkipSource.Chapter, session.Shell.Skip.Source);
        Assert.False(session.Shell.Skip.OnTransport);

        session.SkipActiveSegment();
        Assert.Equal(75, engine.Position, 3);
        Assert.False(session.Shell.Skip.Visible);
    }

    [Fact]
    public void Marker_credits_auto_counts_down_and_cancel_stops_it()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var video = Path.Combine(season, "S01E01.mkv");
        File.WriteAllBytes(video, [1]);
        File.WriteAllText(Path.Combine(season, "S01E01.skip.json"), """{ "credits": { "start": 50, "end": 80 } }""");
        var engine = new FakeMediaEngine { Duration = 90 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        engine.Seek(50);
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        session.Tick(t0);

        Assert.True(session.Shell.Skip.Visible);
        Assert.True(session.Shell.Skip.TwoLine);
        Assert.Equal("크레딧 건너뛰기", session.Shell.Skip.Label);
        Assert.Equal("취소 (3)", session.Shell.Skip.CancelLabel);
        Assert.Equal(SkipSource.Marker, session.Shell.Skip.Source);

        session.Tick(t0.AddSeconds(1));
        Assert.Equal("취소 (2)", session.Shell.Skip.CancelLabel);
        session.CancelSkipAuto();
        session.Tick(t0.AddSeconds(4));
        Assert.Equal(50, engine.Position, 3);
        Assert.True(session.Shell.Skip.Visible);
        Assert.False(session.Shell.Skip.AutoPending);
        Assert.Equal("크레딧 건너뛰기", session.Shell.Skip.Label);
    }

    [Fact]
    public void Marker_auto_skips_when_countdown_elapses()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var video = Path.Combine(season, "S01E02.mkv");
        File.WriteAllBytes(video, [1]);
        File.WriteAllText(Path.Combine(season, "skip-markers.json"), """{ "recap": [0, 35] }""");
        var engine = new FakeMediaEngine { Duration = 200 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        session.Tick(t0);
        Assert.Equal("리캡 건너뛰기", session.Shell.Skip.Label);
        Assert.True(session.Shell.Skip.AutoPending);
        session.Tick(t0.AddSeconds(3));
        Assert.Equal(35, engine.Position, 3);
        Assert.False(session.Shell.Skip.Visible);
    }

    [Fact]
    public void No_capsule_without_chapter_or_marker()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("plain.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 400, Chapters = [new MediaChapter("Part A", 0, 120)] };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.False(session.Shell.Skip.Visible);
        Assert.Empty(session.SkipSegments);
    }
}
