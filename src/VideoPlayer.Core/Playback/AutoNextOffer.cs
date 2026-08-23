namespace VideoPlayer.Core.Playback;

/// <summary>Natural-end auto-next with a 3 second cancel window. Does not fire from last-10s complete.</summary>
public sealed class AutoNextOffer
{
    public static readonly TimeSpan CancelWindow = TimeSpan.FromSeconds(3);

    public bool Pending { get; private set; }
    public bool Cancelled { get; private set; }
    public string? NextPath { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }

    public void Begin(string nextPath, DateTimeOffset now)
    {
        if (Cancelled || Pending || string.IsNullOrWhiteSpace(nextPath))
        {
            return;
        }

        Pending = true;
        NextPath = nextPath;
        StartedAt = now;
    }

    public void Cancel()
    {
        Cancelled = true;
        Pending = false;
    }

    public void ResetForNewTitle()
    {
        Pending = false;
        Cancelled = false;
        NextPath = null;
    }

    public bool ShouldAdvance(DateTimeOffset now)
        => Pending && !Cancelled && NextPath is not null && now - StartedAt >= CancelWindow;

    public TimeSpan Remaining(DateTimeOffset now)
    {
        if (!Pending)
        {
            return TimeSpan.Zero;
        }

        var left = CancelWindow - (now - StartedAt);
        return left < TimeSpan.Zero ? TimeSpan.Zero : left;
    }
}
