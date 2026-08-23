namespace VideoPlayer.Core.Playback;

public static class PlaybackSpeed
{
    public const double Min = 0.5;
    public const double Max = 2.0;
    public const double Default = 1.0;

    public static readonly IReadOnlyList<double> Presets = [0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0];

    public static double Clamp(double speed)
    {
        if (double.IsNaN(speed) || double.IsInfinity(speed))
        {
            return Default;
        }

        return Math.Clamp(speed, Min, Max);
    }

    public static string Format(double speed) => $"{Clamp(speed):0.0}x";
}

public static class SeekCommands
{
    public const double SkipSeconds = 10;

    public static double ApplyRelative(double position, double duration, double delta)
    {
        if (double.IsNaN(position) || double.IsInfinity(position))
        {
            position = 0;
        }

        var next = position + delta;
        if (duration > 0)
        {
            next = Math.Clamp(next, 0, duration);
        }

        return Math.Max(0, next);
    }
}
