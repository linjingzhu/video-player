namespace VideoPlayer.Core.Playback;

/// <summary>
/// One global fit value. 맞춤 keeps the picture inside the frame.
/// 채우기 crops to fill. Stored on the existing AppData window record.
/// </summary>
public static class FitModes
{
    public const string Contain = "contain";
    public const string Cover = "cover";
    public const string MenuHeader = "화면 맞춤";
    public const string ContainLabel = "맞춤";
    public const string CoverLabel = "채우기";

    public static string Normalize(string? mode)
        => string.Equals(mode, Cover, StringComparison.OrdinalIgnoreCase) ? Cover : Contain;

    public static bool IsCover(string? mode) => Normalize(mode) == Cover;
}
