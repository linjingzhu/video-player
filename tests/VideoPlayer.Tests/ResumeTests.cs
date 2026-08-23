using VideoPlayer.Core.Playback;

namespace VideoPlayer.Tests;

public class ResumeTests
{
    [Fact]
    public void Resume_key_is_path_plus_size()
    {
        var key = ResumeKey.From("/videos/show/S01E01.mkv", 12345);
        Assert.Contains("|12345", key);
        Assert.True(ResumeKey.TryParse(key, out var path, out var size));
        Assert.Equal(12345, size);
        Assert.False(string.IsNullOrWhiteSpace(path));
    }

    [Fact]
    public void Different_size_is_a_different_resume_key()
    {
        var a = ResumeKey.From("/videos/a.mkv", 10);
        var b = ResumeKey.From("/videos/a.mkv", 11);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Last_ten_seconds_records_next_episode_at_zero()
    {
        var current = new MediaIdentity("/library/S01E01.mkv", 1000);
        var next = new MediaIdentity("/library/S01E02.mkv", 2000);
        var result = CompletionPolicy.Checkpoint(current, positionSeconds: 91, durationSeconds: 100, next);

        Assert.True(result.CurrentCompleted);
        Assert.True(result.RecordedNextFromLastTenSeconds);
        Assert.NotNull(result.NextEpisodeAtZero);
        Assert.Equal(0, result.NextEpisodeAtZero!.PositionSeconds);
        Assert.Equal(next.Key, result.NextEpisodeAtZero.Key);
        Assert.Equal(0, result.Current.PositionSeconds);
        Assert.True(result.Current.Completed);
    }

    [Fact]
    public void Mid_episode_does_not_advance_to_next()
    {
        var current = new MediaIdentity("/library/S01E01.mkv", 1000);
        var next = new MediaIdentity("/library/S01E02.mkv", 2000);
        var result = CompletionPolicy.Checkpoint(current, 40, 100, next);

        Assert.False(result.CurrentCompleted);
        Assert.Null(result.NextEpisodeAtZero);
        Assert.Equal(40, result.Current.PositionSeconds);
    }

    [Fact]
    public void Ninety_five_percent_marks_complete_without_forcing_next_unless_last_ten()
    {
        var current = new MediaIdentity("/library/long.mkv", 5000);
        var next = new MediaIdentity("/library/next.mkv", 6000);
        var result = CompletionPolicy.Checkpoint(current, positionSeconds: 960, durationSeconds: 1000, next);

        Assert.True(CompletionPolicy.IsAtLeastNinetyFivePercent(960, 1000));
        Assert.False(CompletionPolicy.IsInLastTenSeconds(960, 1000));
        Assert.True(result.CurrentCompleted);
        Assert.False(result.RecordedNextFromLastTenSeconds);
        Assert.Null(result.NextEpisodeAtZero);
    }

    [Fact]
    public void Resume_store_continue_points_at_next_after_last_ten()
    {
        var store = new ResumeStore();
        var result = CompletionPolicy.Checkpoint(
            new MediaIdentity("/a/S01E01.mkv", 1),
            99,
            100,
            new MediaIdentity("/a/S01E02.mkv", 2));
        store.Apply(result);

        Assert.NotNull(store.Continue);
        Assert.Equal("/a/S01E02.mkv", store.Continue!.Path);
        Assert.Equal(0, store.Continue.PositionSeconds);
    }

    [Fact]
    public void Resume_store_round_trips_json_and_rejects_remote_entries()
    {
        var store = new ResumeStore();
        store.Apply(CompletionPolicy.Checkpoint(new MediaIdentity("/local/ok.mkv", 9), 12, 80, null));
        var json = store.ToJson();
        json = json.Replace("\"path\": \"/local/ok.mkv\"", "\"path\": \"http://evil.example/ok.mkv\"", StringComparison.Ordinal);

        var loaded = ResumeStore.FromJson(json);
        Assert.Empty(loaded.Entries);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("|")]
    [InlineData("nopath|")]
    [InlineData("|12")]
    [InlineData("file|-3")]
    public void Invalid_resume_keys_do_not_parse(string? key)
        => Assert.False(ResumeKey.TryParse(key, out _, out _));
}
