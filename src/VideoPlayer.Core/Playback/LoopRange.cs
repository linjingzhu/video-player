namespace VideoPlayer.Core.Playback;

/// <summary>
/// Repeat between the I and O marks. The seek band is lime at 25% and hides
/// while the clip-save handles are on the bar.
/// </summary>
public static class LoopRange
{
    public const string ItemHeader = "구간 반복";
    public const string NeedsMarksHeader = "I/O 먼저";
    public const string Key = "L";
    public const double BandOpacity = 0.25;

    public static bool HasBothMarks(double? inMark, double? outMark)
        => inMark is double start
           && outMark is double end
           && double.IsFinite(start)
           && double.IsFinite(end);

    public static bool TrySpan(double? inMark, double? outMark, out double start, out double end)
    {
        start = 0;
        end = 0;
        if (!HasBothMarks(inMark, outMark))
        {
            return false;
        }

        start = Math.Min(inMark!.Value, outMark!.Value);
        end = Math.Max(inMark.Value, outMark.Value);
        return end - start > 0.001;
    }

    public static bool ShowBand(bool enabled, double? inMark, double? outMark, bool sheetHandlesVisible)
        => enabled && !sheetHandlesVisible && TrySpan(inMark, outMark, out _, out _);

    /// <summary>
    /// When the playhead has reached the out point, return the in point.
    /// Positions before the span keep playing until they hit the out point.
    /// </summary>
    public static double? RewindTarget(bool enabled, double position, double? inMark, double? outMark)
    {
        if (!enabled || !double.IsFinite(position) || !TrySpan(inMark, outMark, out var start, out var end))
        {
            return null;
        }

        if (position + 0.001 < end)
        {
            return null;
        }

        return start;
    }
}
