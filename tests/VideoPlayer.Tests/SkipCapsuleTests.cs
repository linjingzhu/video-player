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
        Assert.False(skip.UsesIntroDb);
        Assert.False(skip.UsesAccounts);
        Assert.True(skip.DefaultIsButtonOnly);
        Assert.True(skip.SharesNextEpisodeCorner);
        Assert.True(skip.ExclusiveCornerCapsule);
        Assert.True(PlayerShell.Boot().NextEpisode.SharesSkipCorner);
        Assert.Equal(OverlayAnchor.BottomRight, PlayerShell.Boot().NextEpisode.Anchor);
        Assert.False(skip.AutoEnabled);
        Assert.Equal("#141418", skip.PanelColor);
        Assert.Equal("#0B0B0D", skip.BackgroundColor);
        Assert.Equal("#0A84FF", skip.AccentColor);
        Assert.Equal("인트로 건너뛰기", UiCopy.SkipIntro);
        Assert.Equal("리캡 건너뛰기", UiCopy.SkipRecap);
        Assert.Equal("크레딧 건너뛰기", UiCopy.SkipCredits);
        Assert.Equal("여기까지 스킵", UiCopy.SkipToHere);
        Assert.Equal("취소 ({0})", UiCopy.SkipCancelCountdown);
        Assert.Equal(
            new[] { "intro", "opening", "recap", "credits", "outro", "오프닝", "도입", "리캡", "예고", "엔딩", "크레딧" },
            ChapterAliases.Locked);
    }

    [Theory]
    [InlineData("Opening", SkipKind.Intro)]
    [InlineData("INTRO", SkipKind.Intro)]
    [InlineData("오프닝", SkipKind.Intro)]
    [InlineData("도입", SkipKind.Intro)]
    [InlineData("Opening Credits", SkipKind.Intro)]
    [InlineData("recap", SkipKind.Recap)]
    [InlineData("리캡", SkipKind.Recap)]
    [InlineData("예고", SkipKind.Recap)]
    [InlineData("outro", SkipKind.Credits)]
    [InlineData("Credits", SkipKind.Credits)]
    [InlineData("엔딩", SkipKind.Credits)]
    [InlineData("크레딧", SkipKind.Credits)]
    [InlineData("인트로", null)]
    [InlineData("Previously On", null)]
    [InlineData("지난 이야기", null)]
    [InlineData("Scene 04", null)]
    [InlineData("", null)]
    public void Chapter_aliases_are_the_locked_set_only(string title, SkipKind? expected)
        => Assert.Equal(expected, ChapterAliases.Classify(title));

    [Fact]
    public void Recap_wins_overlap_with_intro()
    {
        var detected = SkipDetector.Detect(
            [
                new MediaChapter("오프닝", 0, 90),
                new MediaChapter("리캡", 0, 40)
            ],
            [],
            600);
        Assert.Equal(SkipKind.Recap, SkipDetector.Active(detected, 10)!.Kind);
        Assert.Equal(SkipKind.Intro, SkipDetector.Active(detected, 50)!.Kind);
    }

    [Fact]
    public void Chapter_opening_shows_button_only_by_default()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("show.mkv", [1]);
        var engine = new FakeMediaEngine
        {
            Duration = 600,
            Chapters =
            [
                new MediaChapter("오프닝", 0, 0),
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

        session.SkipActiveSegment();
        Assert.Equal(75, engine.Position, 3);
        Assert.False(session.Shell.Skip.Visible);
    }

    [Fact]
    public void Skip_to_here_is_season_keyed_and_shared()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var first = Path.Combine(season, "S01E01.mkv");
        var second = Path.Combine(season, "S01E02.mkv");
        File.WriteAllBytes(first, [1]);
        File.WriteAllBytes(second, [2]);
        var engine = new FakeMediaEngine { Duration = 400 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(first);
        session.SeekAbsolute(90);
        var marked = session.MarkSkipToHere();

        Assert.NotNull(marked);
        Assert.Equal(0, marked!.Start);
        Assert.Equal(90, marked.End);
        Assert.Equal(SkipKind.Intro, marked.Kind);
        Assert.Equal(SkipSource.Marker, marked.Source);
        Assert.Equal(SeasonSkipStore.SeasonFolder(first), SeasonSkipStore.SeasonFolder(second));

        session.SeekAbsolute(10);
        session.Tick(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        Assert.True(session.Shell.Skip.Visible);
        Assert.Equal("인트로 건너뛰기", session.Shell.Skip.Label);
        Assert.False(session.Shell.Skip.AutoPending);

        var next = new PlaybackSession(new FakeMediaEngine { Duration = 400 }, workspace.Data);
        next.Open(second);
        next.Tick(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        Assert.True(next.Shell.Skip.Visible);
        Assert.Equal(90, next.SkipSegments.Single().End);
        Assert.False(next.SkipAutoEnabled);
        Assert.False(next.Shell.Skip.AutoPending);
    }

    [Fact]
    public void Skip_to_here_extends_from_previous_marker_start()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var video = Path.Combine(season, "S01E01.mkv");
        File.WriteAllBytes(video, [1]);
        var engine = new FakeMediaEngine { Duration = 400 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(40);
        session.MarkSkipToHere();
        session.SeekAbsolute(95);
        var extended = session.MarkSkipToHere();
        Assert.Equal(0, extended!.Start);
        Assert.Equal(95, extended.End);
    }

    [Fact]
    public void No_marker_hides_button_and_disables_auto()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("plain.mkv", [1]);
        var engine = new FakeMediaEngine { Duration = 400, Chapters = [new MediaChapter("Part A", 0, 120)] };
        var session = new PlaybackSession(engine, workspace.Data);
        session.SkipAutoEnabled = true;
        session.Open(video);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.False(session.Shell.Skip.Visible);
        Assert.False(session.Shell.Skip.AutoPending);
        Assert.Empty(session.SkipSegments);
    }

    [Fact]
    public void Auto_is_three_second_cancel_when_enabled()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var video = Path.Combine(season, "S01E01.mkv");
        File.WriteAllBytes(video, [1]);
        var engine = new FakeMediaEngine { Duration = 200 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(35);
        session.MarkSkipToHere();
        session.SeekAbsolute(0);
        session.SkipAutoEnabled = true;
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        session.Tick(t0);

        Assert.True(session.Shell.Skip.TwoLine);
        Assert.Equal("인트로 건너뛰기", session.Shell.Skip.Label);
        Assert.Equal("취소 (3)", session.Shell.Skip.CancelLabel);

        session.Tick(t0.AddSeconds(1));
        Assert.Equal("취소 (2)", session.Shell.Skip.CancelLabel);
        session.CancelSkipAuto();
        session.Tick(t0.AddSeconds(4));
        Assert.Equal(0, engine.Position, 3);
        Assert.True(session.Shell.Skip.Visible);
        Assert.False(session.Shell.Skip.AutoPending);
    }

    [Fact]
    public void Auto_elapses_to_the_marker_end()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var video = Path.Combine(season, "S01E01.mkv");
        File.WriteAllBytes(video, [1]);
        var engine = new FakeMediaEngine { Duration = 200 };
        var session = new PlaybackSession(engine, workspace.Data);
        session.Open(video);
        session.SeekAbsolute(35);
        session.MarkSkipToHere();
        session.SeekAbsolute(0);
        session.SkipAutoEnabled = true;
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        session.Tick(t0);
        session.Tick(t0.AddSeconds(3));
        Assert.Equal(35, engine.Position, 3);
        Assert.False(session.Shell.Skip.Visible);
    }

    [Fact]
    public void Credits_cta_hides_next_episode_until_credits_end()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var first = Path.Combine(season, "S01E01.mkv");
        var second = Path.Combine(season, "S01E02.mkv");
        File.WriteAllBytes(first, [1]);
        File.WriteAllBytes(second, [2]);
        var engine = new FakeMediaEngine
        {
            Duration = 100,
            Chapters =
            [
                new MediaChapter("본편", 0, 85),
                new MediaChapter("크레딧", 85, 96)
            ]
        };
        var session = new PlaybackSession(engine, workspace.Data);
        session.OpenSeriesFolder(workspace.Root);
        session.Open(first);
        session.SeekAbsolute(90);
        session.Tick(DateTimeOffset.UtcNow);

        Assert.True(session.Shell.Skip.Visible);
        Assert.Equal("크레딧 건너뛰기", session.Shell.Skip.Label);
        Assert.False(session.Shell.NextEpisode.ShowCta);

        session.SeekAbsolute(97);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.False(session.Shell.Skip.Visible);
        Assert.True(session.Shell.NextEpisode.ShowCta);
        Assert.Equal("다음 화 >", session.Shell.NextEpisode.Label);
        Assert.False(session.Shell.Skip.Visible);
    }

    [Fact]
    public void Shared_corner_shows_only_one_capsule_when_ranges_do_not_overlap()
    {
        using var workspace = new TempWorkspace();
        var season = Path.Combine(workspace.Root, "S01");
        Directory.CreateDirectory(season);
        var first = Path.Combine(season, "S01E01.mkv");
        var second = Path.Combine(season, "S01E02.mkv");
        File.WriteAllBytes(first, [1]);
        File.WriteAllBytes(second, [2]);
        var engine = new FakeMediaEngine
        {
            Duration = 200,
            Chapters = [new MediaChapter("오프닝", 0, 80)]
        };
        var session = new PlaybackSession(engine, workspace.Data);
        session.OpenSeriesFolder(workspace.Root);
        session.Open(first);

        session.SeekAbsolute(10);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.True(session.Shell.Skip.Visible);
        Assert.False(session.Shell.NextEpisode.ShowCta);
        Assert.False(SkipDetector.RangesOverlap(session.SkipSegments.Single(), 190, 200));

        session.SeekAbsolute(195);
        session.Tick(DateTimeOffset.UtcNow);
        Assert.False(session.Shell.Skip.Visible);
        Assert.True(session.Shell.NextEpisode.ShowCta);
        Assert.Equal((true, false), SkipDetector.ExclusiveCorner(true, true));
    }
}
