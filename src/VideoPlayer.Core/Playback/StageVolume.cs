using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Playback;

/// <summary>
/// Volume over the video: wheel and Up/Down move five percent.
/// Left/Right stay on the ±N jump. The seek bar does not use this step.
/// </summary>
public static class StageVolume
{
    public const double Step = 0.05;
    public const string UpKey = "Up";
    public const string DownKey = "Down";

    public static readonly IReadOnlyList<string> Collisions = [];

    public static bool ClashesWithExisting(string key)
        => key.Equals("Left", StringComparison.OrdinalIgnoreCase)
           || key.Equals("Right", StringComparison.OrdinalIgnoreCase)
           || key.Equals("M", StringComparison.OrdinalIgnoreCase)
           || key.Equals("I", StringComparison.OrdinalIgnoreCase)
           || key.Equals("O", StringComparison.OrdinalIgnoreCase);

    public static double Nudge(double volume, int direction)
    {
        var level = double.IsFinite(volume) ? volume : 0;
        return Math.Clamp(level + (direction * Step), 0, 1);
    }

    public static string Osd(double volume)
    {
        var level = double.IsFinite(volume) ? Math.Clamp(volume, 0, 1) : 0;
        return $"볼륨 {VolumeChrome.ToPercent(level)}";
    }
}
