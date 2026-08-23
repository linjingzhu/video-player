namespace VideoPlayer.Core.Shell;

/// <summary>Locked volume pack: speaker + vertical popover. No horizontal transport slider.</summary>
public sealed class VolumeChrome
{
    public static IReadOnlyList<string> MuteKeys { get; } = ["M"];

    public bool HorizontalSliderOnTransport { get; } = false;
    public bool SpeakerOnTransport { get; } = true;
    public bool SpeakerClickMutes { get; } = false;
    public bool SpeakerClickTogglesPopover { get; } = true;
    public bool VerticalPopover { get; } = true;
    public bool PercentOnTop { get; } = true;
    public bool WhiteThumb { get; } = true;
    public bool AccentOnFilledTrackOnly { get; } = true;
    public bool WheelOverSpeakerChangesVolume { get; } = true;
    public bool WheelOverPopoverChangesVolume { get; } = true;
    public bool VolumeZeroMutes { get; } = true;
    public bool PopoverOpen { get; set; }
    public bool Muted { get; set; }
    public double Level { get; set; } = 1.0;
    public int Percent { get; set; } = 100;

    public string PercentText => Percent.ToString();

    public static bool IsMuteKey(string? key)
        => !string.IsNullOrEmpty(key)
           && MuteKeys.Any(mute => string.Equals(mute, key, StringComparison.OrdinalIgnoreCase));

    public static int ToPercent(double volume)
        => (int)Math.Round(Math.Clamp(volume, 0, 1) * 100, MidpointRounding.AwayFromZero);

    public void Sync(double volume, bool muted)
    {
        Level = Math.Clamp(volume, 0, 1);
        Muted = muted || Level <= 0;
        Percent = ToPercent(Level);
    }
}
