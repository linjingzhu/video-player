using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Skip;

public sealed class SkipAutoOffer
{
    public static readonly TimeSpan CancelWindow = TimeSpan.FromSeconds(3);

    public bool Pending { get; private set; }
    public SkipKind? Kind { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    private readonly HashSet<SkipKind> _cancelled = [];

    public bool WasCancelled(SkipKind kind) => _cancelled.Contains(kind);

    public void Begin(SkipKind kind, DateTimeOffset now)
    {
        if (Pending || _cancelled.Contains(kind))
        {
            return;
        }

        Pending = true;
        Kind = kind;
        StartedAt = now;
    }

    public void Cancel()
    {
        if (Kind is { } kind)
        {
            _cancelled.Add(kind);
        }

        Pending = false;
    }

    public void ResetForNewTitle()
    {
        Pending = false;
        Kind = null;
        StartedAt = default;
        _cancelled.Clear();
    }

    public void LeaveRegion()
    {
        Pending = false;
        Kind = null;
    }

    public bool ShouldApply(DateTimeOffset now)
        => Pending && Kind is not null && now - StartedAt >= CancelWindow;

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

public sealed class SkipCapsuleState
{
    public bool Visible { get; set; }
    public bool AutoPending { get; set; }
    public SkipKind? Kind { get; set; }
    public SkipSource? Source { get; set; }
    public double SkipTo { get; set; }
    public int AutoSeconds { get; set; }
    public bool OverlayOnly { get; } = true;
    public bool OnTransport { get; } = false;
    public OverlayAnchor Anchor { get; } = OverlayAnchor.BottomRight;
    public string PanelColor { get; } = SkinA.Panel;
    public string BackgroundColor { get; } = SkinA.Background;
    public string AccentColor { get; } = SkinA.Accent;
    public int CapsuleRadius { get; } = SkinA.RadiusPill;
    public bool UsesExternalDatabase { get; } = false;
    public bool UsesIntroDb { get; } = false;
    public bool UsesAccounts { get; } = false;
    public bool DefaultIsButtonOnly { get; } = true;
    public bool AutoEnabled { get; set; }

    public string Label => Kind switch
    {
        SkipKind.Intro => UiCopy.SkipIntro,
        SkipKind.Recap => UiCopy.SkipRecap,
        SkipKind.Credits => UiCopy.SkipCredits,
        _ => ""
    };

    public string CancelLabel => string.Format(UiCopy.SkipCancelCountdown, Math.Max(0, AutoSeconds));

    public bool TwoLine => Visible && AutoPending;

    public void Hide()
    {
        Visible = false;
        AutoPending = false;
        Kind = null;
        Source = null;
        SkipTo = 0;
        AutoSeconds = 0;
    }

    public void Show(SkipSegment segment, bool autoPending, int autoSeconds)
    {
        Visible = true;
        Kind = segment.Kind;
        Source = segment.Source;
        SkipTo = segment.End;
        AutoPending = autoPending;
        AutoSeconds = AutoPending ? Math.Max(0, autoSeconds) : 0;
    }
}
