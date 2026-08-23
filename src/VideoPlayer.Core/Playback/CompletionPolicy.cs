namespace VideoPlayer.Core.Playback;

/// <summary>
/// Resume completion. Last 10 seconds marks the current title complete only.
/// Does not seek or record the next episode.
/// </summary>
public static class CompletionPolicy
{
    public const double LastSecondsThreshold = 10d;

    public static bool IsInLastTenSeconds(double positionSeconds, double durationSeconds)
        => durationSeconds > 0
           && positionSeconds >= 0
           && positionSeconds >= durationSeconds - LastSecondsThreshold;

    public static bool IsComplete(double positionSeconds, double durationSeconds)
        => IsInLastTenSeconds(positionSeconds, durationSeconds);

    public static CheckpointResult Checkpoint(
        MediaIdentity current,
        double positionSeconds,
        double durationSeconds)
    {
        var lastTen = IsInLastTenSeconds(positionSeconds, durationSeconds);

        var currentEntry = new ResumeEntry
        {
            Key = current.Key,
            Path = current.Path,
            Size = current.Size,
            PositionSeconds = lastTen ? 0 : Math.Max(0, positionSeconds),
            DurationSeconds = Math.Max(0, durationSeconds),
            Completed = lastTen,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        return new CheckpointResult(currentEntry, CurrentCompleted: lastTen);
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

public readonly record struct CheckpointResult(ResumeEntry Current, bool CurrentCompleted);
