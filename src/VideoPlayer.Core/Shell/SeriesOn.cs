namespace VideoPlayer.Core.Shell;

/// <summary>
/// Locked SeriesOn main-shell chrome. Density matches Skin A (transport 40,
/// rail 28, body 12, spacing 4 8 12 16). Lime accent is title, volume fill,
/// and timecode only. Series C page tokens stay on <see cref="SkinC"/>.
/// </summary>
public static class SeriesOn
{
    public const string Background = "#050505";
    public const string Elevated = "#0E0E0E";
    public const string Panel = Elevated;
    public const string Border = "#222222";
    public const double BorderOpacity = 0.40;
    public const string Text = "#FFFFFF";
    public const string Secondary = "#8A8A8A";
    public const string Accent = "#C6FF00";
    public const string OnAccent = "#050505";
    public const string Thumb = Accent;
    public const string HoverWhite = "#14FFFFFF";
    public const string ChromeFill = Background;
    public const bool ChromeIsSolid = true;
    public const bool ChromeHasBlur = false;
    public const bool ChromeHasWhiteOverlay = false;
    public const string Divider = "#222222";
    public const double DividerOpacity = 0.40;

    public const string FontFamily = SkinA.FontFamily;
    public const string FontFallback = SkinA.FontFallback;
    public const int HeaderTitleSize = 16;
    public const int HeaderTitleWeight = 700;
    public const int BodySize = SkinA.BodySize;
    public const int BodyWeight = SkinA.BodyWeight;
    public const int MetaSize = SkinA.MetaSize;
    public const int MetaWeight = SkinA.MetaWeight;

    public static readonly int[] SpacingScale = SkinA.SpacingScale;
    public const int SpacingMin = SkinA.SpacingMin;
    public const int SpacingMax = SkinA.SpacingMax;
    public const int RadiusControl = SkinA.RadiusControl;
    public const int RadiusPanel = SkinA.RadiusPanel;
    public const int RadiusWindow = 0;
    public const int HeaderHeightPx = 36;
    public const int TransportHeightPx = SkinA.TransportHeightPx;
    public const int TransportSeparatorPx = 1;
    public const int VolumeSliderWidthPx = 88;
    public const int SidebarRailWidthPx = SkinA.SidebarRailWidthPx;
    public const int ButtonPadding = 4;
    public const int IoMarkSizePx = 2;

    public const bool TitleIsAccent = true;
    public const bool VolumeFillIsAccent = true;
    public const bool TimecodeIsAccent = true;
    public const bool VolumeThumbIsRound = true;
    public const bool PlayTriangleIsWhite = true;
    public const bool StopButtonExists = true;
    public const bool HasClear = true;
    public const bool ClearIsTextLabel = true;
    public const bool ClearUsesEjectIcon = false;
    public const bool SkipPlusMinusOnTransport = false;
    public const bool HorizontalVolumeSlider = true;
    public const bool VerticalVolumePopover = false;
    public const bool HasFileViewMenuBar = false;
    public const bool QuickMenuIsView = true;
    public const bool FileCommandsInHamburger = true;
    public const bool FileCommandsInQuickMenu = true;
    public const bool HamburgerIsView = true;
    public const bool CaptionsOnBar = false;
    public const bool FullscreenOnBar = false;
    public const bool IoMarksAreSquares = true;
    public const bool HasWindowControls = true;
    public const bool HasCastIcon = false;
    public const bool HasHdrIcon = false;
    public const bool HasEjectIcon = false;
    public const bool HasBrandWordmark = false;
    public const bool HasMenuPipe = false;
}
