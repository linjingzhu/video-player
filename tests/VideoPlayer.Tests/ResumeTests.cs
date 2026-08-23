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
    public void Last_ten_seconds_marks_complete_and_does_not_seek_next()
    {
        var current = new MediaIdentity("/library/S01E01.mkv", 1000);
        var result = CompletionPolicy.Checkpoint(current, positionSeconds: 91, durationSeconds: 100);

        Assert.True(result.CurrentCompleted);
        Assert.True(result.Current.Completed);
        Assert.Equal(0, result.Current.PositionSeconds);
        Assert.Equal(current.Key, result.Current.Key);
    }

    [Fact]
    public void Mid_episode_keeps_position_and_is_not_complete()
    {
        var current = new MediaIdentity("/library/S01E01.mkv", 1000);
        var result = CompletionPolicy.Checkpoint(current, 40, 100);

        Assert.False(result.CurrentCompleted);
        Assert.Equal(40, result.Current.PositionSeconds);
    }

    [Fact]
    public void Resume_store_does_not_point_continue_at_next_after_last_ten()
    {
        var store = new ResumeStore();
        store.Apply(CompletionPolicy.Checkpoint(new MediaIdentity("/a/S01E01.mkv", 1), 50, 100));
        Assert.Equal("/a/S01E01.mkv", store.Continue!.Path);

        store.Apply(CompletionPolicy.Checkpoint(new MediaIdentity("/a/S01E01.mkv", 1), 99, 100));
        Assert.Null(store.Continue);
        Assert.True(store.Find("/a/S01E01.mkv", 1)!.Completed);
    }

    [Fact]
    public void Resume_store_round_trips_json_and_rejects_remote_entries()
    {
        var store = new ResumeStore();
        store.Apply(CompletionPolicy.Checkpoint(new MediaIdentity("/local/ok.mkv", 9), 12, 80));
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
