using VideoPlayer.Core.Library;

namespace VideoPlayer.Core.Playback;

public static class PlaybackSpeed
{
    public const double Min = 0.5;
    public const double Max = 2.0;
    public const double Default = 1.0;

    public const double Step = 0.1;

    public static readonly IReadOnlyList<double> Presets = [0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0];

    /// <summary>Quick-menu choices. Keyboard steps stay on the 0.1 grid.</summary>
    public static readonly IReadOnlyList<double> QuickMenuPresets = [0.5, 1.0, 1.25, 1.5, 2.0];

    public static double Clamp(double speed)
    {
        if (double.IsNaN(speed) || double.IsInfinity(speed))
        {
            return Default;
        }

        return Math.Clamp(speed, Min, Max);
    }

    public static double Nudge(double speed, int direction)
    {
        if (direction == 0)
        {
            return Clamp(speed);
        }

        var next = Math.Round(Clamp(speed) + (direction * Step), 2, MidpointRounding.AwayFromZero);
        return Clamp(next);
    }

    public static bool IsPresetSelected(double speed, double preset)
        => Math.Abs(Clamp(speed) - preset) < 0.001;

    public static string Format(double speed)
    {
        var value = Math.Round(Clamp(speed), 2, MidpointRounding.AwayFromZero);
        var tenths = Math.Round(value * 10, MidpointRounding.AwayFromZero);
        if (Math.Abs((value * 10) - tenths) < 0.001)
        {
            return $"{value:0.0}x";
        }

        return $"{value:0.00}x";
    }

    public static string MenuHeader(double speed) => $"배속 {Format(speed)}";

    public static string OsdText(double speed) => MenuHeader(speed);
}

/// <summary>
/// Plain C steps speed up. Ctrl+Shift+C stays capture.
/// Plain V toggles captions. V was not bound before this move.
/// </summary>
public static class SpeedKeys
{
    public const string Faster = "C";
    public const string Slower = "X";
    public const string Reset = "Z";
    public const string Captions = "V";

    public static readonly IReadOnlyList<string> PreviouslyBound =
        ["Space", "Left", "Right", "I", "O", "Escape", "Enter", "F11", "M", "C", "X", "Z"];

    public enum Binding
    {
        None,
        Capture,
        Faster,
        Slower,
        Reset,
        Captions
    }

    public static Binding Resolve(string key, bool control, bool shift, bool alt)
    {
        if (string.IsNullOrEmpty(key))
        {
            return Binding.None;
        }

        if (control && shift && !alt && key.Equals(Faster, StringComparison.OrdinalIgnoreCase))
        {
            return Binding.Capture;
        }

        if (control || shift || alt)
        {
            return Binding.None;
        }

        if (key.Equals(Faster, StringComparison.OrdinalIgnoreCase))
        {
            return Binding.Faster;
        }

        if (key.Equals(Slower, StringComparison.OrdinalIgnoreCase))
        {
            return Binding.Slower;
        }

        if (key.Equals(Reset, StringComparison.OrdinalIgnoreCase))
        {
            return Binding.Reset;
        }

        if (key.Equals(Captions, StringComparison.OrdinalIgnoreCase))
        {
            return Binding.Captions;
        }

        return Binding.None;
    }
}

public static class SeekCommands
{
    public const double SkipSeconds = JumpInterval.DefaultSeconds;

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
