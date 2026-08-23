using VideoPlayer.Core.Playback;

namespace VideoPlayer.Core.Shell;

/// <summary>HDR lives in 보기 only. No transport badge, no Cast/Miracast.</summary>
public sealed class HdrChrome
{
    public HdrMode Mode { get; set; } = HdrPassThrough.Default;
    public string Menu { get; } = UiCopy.Hdr;
    public IReadOnlyList<string> Choices { get; } = UiCopy.HdrChoices;
    public bool OpensFromViewMenuOnly { get; } = true;
    public bool HasBadgeOnTransport { get; } = false;
    public bool HasCast { get; } = false;
    public bool HasMiracast { get; } = false;
    public bool PassThroughWhenDisplaySupports => HdrPassThrough.IsPassThrough(Mode);
}
