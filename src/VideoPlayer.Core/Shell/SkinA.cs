namespace VideoPlayer.Core.Shell;

/// <summary>
/// Confirmed Skin Pack A visual tokens. Apply to MainWindow chrome,
/// and to this pack's skip capsule and subtitle sheet only.
/// Compact density: 40px transport, 28px rail, body 12. Spacing is 4 8 12 16.
/// Video is full-bleed. Radii: control 2 / panel 4 / window 2. Accent is white.
/// Only the play triangle is #E10600. Seek fill is white; thumb is square.
/// Transport and menus are solid #050505 chrome — no blur, no 8% white overlay.
/// Dividers are the 1px #222222 @ 40% hairline. I/O marks are 2px squares.
/// Icon and text buttons use Padding 4. A layout is unchanged.
/// Capture and clip-save sheet chrome also use these values.
/// </summary>
public static class SkinA
{
    public const string Background = "#050505";
    public const string Elevated = "#0E0E0E";
    public const string Panel = Elevated;
    public const string Border = "#222222";
    public const double BorderOpacity = 0.40;
    public const string Text = "#FFFFFF";
    public const string Secondary = "#8A8A8A";
    public const string Accent = "#FFFFFF";
    public const string PlayTriangle = "#E10600";
    public const string OnAccent = "#050505";
    public const string Thumb = "#FFFFFF";
    public const string HoverWhite = "#14FFFFFF";

    public const string FontFamily = "Segoe UI Variable";
    public const string FontFallback = "Segoe UI";
    public const int TitleSize = 20;
    public const int TitleWeight = 600;
    public const int BodySize = 12;
    public const int BodyWeight = 400;
    public const int MetaSize = 11;
    public const int MetaWeight = 400;

    public const string ChromeFill = Background;
    public const int ChromeBlurRadius = 0;
    public const bool ChromeIsBlurPlusWhite = false;
    public const bool ChromeIsSolid = true;
    public const int HairlineThicknessPx = 1;
    public const int ButtonPadding = 4;
    public const int IoTickSizePx = 2;
    public const bool IoTicksAreSquares = true;
    public const bool IoTicksAreEllipses = false;
    public const bool NoMenuPipeSeparator = true;
    public const bool NoWireframeWindowTitle = true;
    public const bool NoMockCaptionSentences = true;

    public const int RadiusControl = 2;
    public const int RadiusPanel = 4;
    public const int RadiusWindow = 2;
    public const int RadiusPill = RadiusControl;
    public static readonly int[] SpacingScale = [4, 8, 12, 16];
    public const int SpacingMin = 4;
    public const int SpacingMax = 16;
    public const int TransportHeightPx = 40;
    public const int SidebarRailWidthPx = ShellLayout.SidebarRailWidthPx;

    public const bool VideoIsFullBleed = true;
    public const bool NoLetterboxChrome = true;
    public const bool AccentOnTracksOnly = true;
    public const bool AccentIsWhite = true;
    public const bool WhiteThumbs = true;
    public const bool SquareThumbs = true;
    public const bool NoRoundThumbs = true;
    public const bool BorderlessIcons = true;
    public const bool CircularIconHover = false;
    public const bool HoverIsCircularEightPercent = false;
    public const bool HoverIsEightPercentWhite = true;
    public const bool PlayIsCircularCapsule = false;
    public const bool PlayTriangleIsOnlyRed = true;
    public const bool CtaIsCapsuleOverlay = true;
    public const bool FailureIsBannerSlot = true;
    public const bool NoBlueAccent = true;
    public const bool NoBlueThumbs = true;
    public const bool NoGlow = true;
    public const bool NoBoxyButtons = true;
    public const bool NoEmoji = true;
    public const bool CapsuleHasHairline = true;
}
