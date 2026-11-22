namespace VideoPlayer.Core.Playback;

/// <summary>
/// Trailing debounce for volume saves. Wheel ticks arrive faster than a disk
/// write should; the last level in the window is the one that is stored.
/// Speed is never part of this save.
/// </summary>
public static class VolumePersist
{
    public static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(400);

    public static DateTimeOffset DueAt(DateTimeOffset changedAt) => changedAt + Delay;

    public static bool ShouldWrite(DateTimeOffset now, DateTimeOffset? dueAt)
        => dueAt is { } due && now >= due;
}
