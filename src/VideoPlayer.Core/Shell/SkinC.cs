namespace VideoPlayer.Core.Shell;

/// <summary>
/// Confirmed Skin Pack C tokens. Same density as Skin A: body 12, spacing 4 8 12 16.
/// Series page only. Colors, tight radii, and type scale match the shared lock.
/// </summary>
public static class SkinC
{
    public const string Background = SkinA.Background;
    public const string Panel = SkinA.Panel;
    public const string Border = SkinA.Border;
    public const double BorderOpacity = SkinA.BorderOpacity;
    public const string Text = SkinA.Text;
    public const string Secondary = SkinA.Secondary;
    public const string Accent = SkinA.Accent;

    public const string FontFamily = SkinA.FontFamily;
    public const string FontFallback = SkinA.FontFallback;
    public const int TitleSize = SkinA.TitleSize;
    public const int TitleWeight = SkinA.TitleWeight;
    public const int BodySize = SkinA.BodySize;
    public const int BodyWeight = SkinA.BodyWeight;
    public const int MetaSize = SkinA.MetaSize;
    public const int MetaWeight = SkinA.MetaWeight;

    public static readonly int[] SpacingScale = SkinA.SpacingScale;
    public const int SpacingMin = SkinA.SpacingMin;
    public const int SpacingMax = SkinA.SpacingMax;
    public const int RadiusPanel = SkinA.RadiusPanel;

    public const bool SelectionIsPanelPlusHairline = true;
    public const bool BorderlessFolderIcons = true;
    public const bool HoverIsCircularEightPercent = SkinA.HoverIsCircularEightPercent;
}
