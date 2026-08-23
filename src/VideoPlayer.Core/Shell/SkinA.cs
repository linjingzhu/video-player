namespace VideoPlayer.Core.Shell;

/// <summary>
/// Confirmed Skin Pack A visual tokens. Apply to MainWindow chrome only.
/// Does not change A v2 shell structure, layout, or behavior.
/// </summary>
public static class SkinA
{
    public const string Background = "#0B0B0D";
    public const string Panel = "#141418";
    public const string Border = "#2C2C2E";
    public const double BorderOpacity = 0.20;
    public const string Text = "#F5F5F7";
    public const string Secondary = "#8E8E93";
    public const string Accent = "#0A84FF";
    public const string Thumb = "#FFFFFF";
    public const string HoverWhite = "#14FFFFFF";

    public const string FontFamily = "Segoe UI Variable";
    public const string FontFallback = "Segoe UI";
    public const int TitleSize = 20;
    public const int TitleWeight = 600;
    public const int BodySize = 13;
    public const int BodyWeight = 400;
    public const int MetaSize = 11;
    public const int MetaWeight = 400;

    public const string ChromeFill = "#14FFFFFF";
    public const int ChromeBlurRadius = 20;
    public const bool ChromeIsBlurPlusWhite = true;
    public const bool NoWireframeWindowTitle = true;
    public const bool NoMockCaptionSentences = true;

    public const int RadiusPill = 999;
    public const int RadiusPanel = 12;
    public const int RadiusControl = 10;
    public const int SpacingMin = 4;
    public const int SpacingMax = 24;
    public const int TransportHeightPx = 56;
    public const int SidebarRailWidthPx = ShellLayout.SidebarRailWidthPx;

    public const bool AccentOnTracksOnly = true;
    public const bool WhiteThumbs = true;
    public const bool BorderlessIcons = true;
    public const bool CircularIconHover = true;
    public const bool PlayIsCircularCapsule = true;
    public const bool CtaIsCapsuleOverlay = true;
    public const bool FailureIsBannerSlot = true;
    public const bool NoBlueThumbs = true;
    public const bool NoGlow = true;
    public const bool NoBoxyButtons = true;
    public const bool NoEmoji = true;
}
