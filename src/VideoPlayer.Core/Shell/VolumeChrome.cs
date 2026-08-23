namespace VideoPlayer.Core.Shell;

/// <summary>SeriesOn volume: speaker + horizontal slider + numeric. No popover.</summary>
public sealed class VolumeChrome
{
    public static IReadOnlyList<string> MuteKeys { get; } = ["M"];
    public static readonly int[] SpacingScale = SeriesOn.SpacingScale;

    public const int PercentSize = SeriesOn.BodySize;
    public const int SliderWidth = SeriesOn.VolumeSliderWidthPx;
    public const int TrackHeight = 4;

    public bool HorizontalSliderOnTransport { get; } = true;
    public bool SpeakerOnTransport { get; } = true;
    public bool SpeakerClickMutes { get; } = true;
    public bool SpeakerClickTogglesPopover { get; } = false;
    public bool SpeakerRightClickMutes { get; } = false;
    public bool ClickAgainCloses { get; } = false;
    public bool ClickOutsideCloses { get; } = false;
    public bool VerticalPopover { get; } = false;
    public bool PercentOnTop { get; } = false;
    public bool PercentBesideSlider { get; } = true;
    public bool WhiteThumb { get; } = false;
    public bool SquareThumb { get; } = false;
    public bool RoundThumb { get; } = true;
    public bool AccentOnFilledTrackOnly { get; } = true;
    public bool WheelOverSpeakerChangesVolume { get; } = true;
    public bool WheelOverPopoverChangesVolume { get; } = false;
    public bool WheelOverSliderChangesVolume { get; } = true;
    public bool WheelShowsPercent { get; } = true;
    public bool CompactDensity { get; } = true;
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
        PopoverOpen = false;
    }
}
