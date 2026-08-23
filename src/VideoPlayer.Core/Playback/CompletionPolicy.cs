namespace VideoPlayer.Core.Playback;

/// <summary>
/// Completion and next-episode rules.
/// Last 10 seconds is confirmed. 95% is an estimate used as an additional complete signal.
/// </summary>
public static class CompletionPolicy
{
    public const double LastSecondsThreshold = 10d;
    public const double PercentThreshold = 0.95d;

    public static bool IsInLastTenSeconds(double positionSeconds, double durationSeconds)
        => durationSeconds > 0
           && positionSeconds >= 0
           && positionSeconds >= durationSeconds - LastSecondsThreshold;

    public static bool IsAtLeastNinetyFivePercent(double positionSeconds, double durationSeconds)
        => durationSeconds > 0
           && positionSeconds >= 0
           && positionSeconds / durationSeconds >= PercentThreshold;

    public static bool IsComplete(double positionSeconds, double durationSeconds)
        => IsInLastTenSeconds(positionSeconds, durationSeconds)
           || IsAtLeastNinetyFivePercent(positionSeconds, durationSeconds);

    public static CheckpointResult Checkpoint(
        MediaIdentity current,
        double positionSeconds,
        double durationSeconds,
        MediaIdentity? nextEpisode)
    {
        var lastTen = IsInLastTenSeconds(positionSeconds, durationSeconds);
        var complete = lastTen || IsAtLeastNinetyFivePercent(positionSeconds, durationSeconds);

        var currentEntry = new ResumeEntry
        {
            Key = current.Key,
            Path = current.Path,
            Size = current.Size,
            PositionSeconds = complete ? 0 : Math.Max(0, positionSeconds),
            DurationSeconds = Math.Max(0, durationSeconds),
            Completed = complete,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        ResumeEntry? nextAtZero = null;
        if (lastTen && nextEpisode is { } next)
        {
            nextAtZero = new ResumeEntry
            {
                Key = next.Key,
                Path = next.Path,
                Size = next.Size,
                PositionSeconds = 0,
                DurationSeconds = 0,
                Completed = false,
                UpdatedUtc = DateTimeOffset.UtcNow
            };
        }

        return new CheckpointResult(currentEntry, nextAtZero, complete, lastTen);
    }
}

public sealed class ResumeEntry
{
    public required string Key { get; init; }
    public required string Path { get; init; }
    public required long Size { get; init; }
    public double PositionSeconds { get; init; }
    public double DurationSeconds { get; init; }
    public bool Completed { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; }
}

public readonly record struct CheckpointResult(
    ResumeEntry Current,
    ResumeEntry? NextEpisodeAtZero,
    bool CurrentCompleted,
    bool RecordedNextFromLastTenSeconds);
