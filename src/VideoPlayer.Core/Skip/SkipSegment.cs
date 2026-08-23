namespace VideoPlayer.Core.Skip;

public enum SkipKind
{
    Intro,
    Recap,
    Credits
}

public enum SkipSource
{
    Chapter,
    Marker
}

public sealed record SkipSegment(SkipKind Kind, double Start, double End, SkipSource Source)
{
    public bool Contains(double position) => position >= Start && position < End && End > Start;

    public double Length => Math.Max(0, End - Start);
}

public static class SkipKinds
{
    public static IReadOnlyList<SkipKind> DisplayOrder { get; } =
        [SkipKind.Recap, SkipKind.Intro, SkipKind.Credits];
}
