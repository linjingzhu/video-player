using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class SkinATokenTests
{
    [Fact]
    public void Colors_match_confirmed_skin_a()
    {
        Assert.Equal("#050505", SkinA.Background);
        Assert.Equal("#0E0E0E", SkinA.Elevated);
        Assert.Equal("#0E0E0E", SkinA.Panel);
        Assert.Equal("#222222", SkinA.Border);
        Assert.Equal(0.40, SkinA.BorderOpacity);
        Assert.Equal("#FFFFFF", SkinA.Text);
        Assert.Equal("#8A8A8A", SkinA.Secondary);
        Assert.Equal("#FFFFFF", SkinA.Accent);
        Assert.Equal("#E10600", SkinA.PlayTriangle);
        Assert.Equal("#050505", SkinA.OnAccent);
        Assert.Equal("#FFFFFF", SkinA.Thumb);
        Assert.Equal("#14FFFFFF", SkinA.HoverWhite);
        Assert.NotEqual("#0A84FF", SkinA.Accent);
        Assert.NotEqual("#0A84FF", SkinA.PlayTriangle);
    }

    [Fact]
    public void Type_scale_is_title_20_600_body_12_400_meta_11_400()
    {
        Assert.Equal("Segoe UI Variable", SkinA.FontFamily);
        Assert.Equal("Segoe UI", SkinA.FontFallback);
        Assert.Equal(20, SkinA.TitleSize);
        Assert.Equal(600, SkinA.TitleWeight);
        Assert.Equal(12, SkinA.BodySize);
        Assert.Equal(400, SkinA.BodyWeight);
        Assert.Equal(11, SkinA.MetaSize);
        Assert.Equal(400, SkinA.MetaWeight);
    }

    [Fact]
    public void Chrome_material_is_solid_background_without_blur()
    {
        Assert.Equal("#050505", SkinA.ChromeFill);
        Assert.Equal(SkinA.Background, SkinA.ChromeFill);
        Assert.Equal("#14FFFFFF", SkinA.HoverWhite);
        Assert.Equal(0, SkinA.ChromeBlurRadius);
        Assert.False(SkinA.ChromeIsBlurPlusWhite);
        Assert.True(SkinA.ChromeIsSolid);
        Assert.Equal(1, SkinA.HairlineThicknessPx);
        Assert.Equal("#222222", SkinA.Border);
        Assert.Equal(0.40, SkinA.BorderOpacity);
        Assert.Equal(4, SkinA.ButtonPadding);
        Assert.Equal(SkinA.SpacingMin, SkinA.ButtonPadding);
        Assert.Equal(2, SkinA.IoTickSizePx);
        Assert.True(SkinA.IoTicksAreSquares);
        Assert.False(SkinA.IoTicksAreEllipses);
        Assert.True(SkinA.NoMenuPipeSeparator);
        Assert.True(SkinA.NoWireframeWindowTitle);
        Assert.True(SkinA.NoMockCaptionSentences);
        Assert.Equal("이어서", UiCopy.AppTitle);
        Assert.DoesNotContain("caption", UiCopy.AppTitle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("영상 플레이어", UiCopy.AppTitle, StringComparison.Ordinal);
    }

    [Fact]
    public void Display_name_is_ieseo()
    {
        var mainXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml"));
        var csproj = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "VideoPlayer.App.csproj"));
        var appCs = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "App.xaml.cs"));
        Assert.Equal("이어서", UiCopy.AppTitle);
        Assert.Contains("Title=\"이어서\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("영상 플레이어", mainXaml, StringComparison.Ordinal);
        Assert.Contains("<AssemblyName>Ieseo</AssemblyName>", csproj, StringComparison.Ordinal);
        Assert.Contains("<AssemblyTitle>이어서</AssemblyTitle>", csproj, StringComparison.Ordinal);
        Assert.Contains("<Product>이어서</Product>", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("영상 플레이어", csproj, StringComparison.Ordinal);
        Assert.Contains(@"Local\Ieseo.SingleInstance", appCs, StringComparison.Ordinal);
        Assert.Contains("Ieseo.HandOff", appCs, StringComparison.Ordinal);
        Assert.DoesNotContain("SeriesOn", appCs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SERIESON", UiCopy.AppTitle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SeriesOn", UiCopy.AppTitle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SeriesOn", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SpaceX", UiCopy.AppTitle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("xAI", UiCopy.AppTitle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Grok", UiCopy.AppTitle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Type_radii_spacing_and_chrome_sizes_match_confirmed_skin_a()
    {
        Assert.Equal("Segoe UI Variable", SkinA.FontFamily);
        Assert.Equal("Segoe UI", SkinA.FontFallback);
        Assert.Equal(2, SkinA.RadiusControl);
        Assert.Equal(4, SkinA.RadiusPanel);
        Assert.Equal(2, SkinA.RadiusWindow);
        Assert.Equal(SkinA.RadiusControl, SkinA.RadiusPill);
        Assert.Equal(new[] { 4, 8, 12, 16 }, SkinA.SpacingScale);
        Assert.DoesNotContain(20, SkinA.SpacingScale);
        Assert.DoesNotContain(24, SkinA.SpacingScale);
        Assert.Equal(4, SkinA.SpacingMin);
        Assert.Equal(16, SkinA.SpacingMax);
        Assert.Equal(40, SkinA.TransportHeightPx);
        Assert.Equal(28, SkinA.SidebarRailWidthPx);
        Assert.Equal(ShellLayout.SidebarRailWidthPx, SkinA.SidebarRailWidthPx);
        Assert.Equal(40, ShellLayout.TransportHeightPx);
        Assert.True(SkinA.VideoIsFullBleed);
        Assert.True(SkinA.NoLetterboxChrome);
    }

    [Fact]
    public void Accent_is_white_with_square_thumbs_and_red_play_triangle_only()
    {
        Assert.True(SkinA.AccentIsWhite);
        Assert.True(SkinA.AccentOnTracksOnly);
        Assert.True(SkinA.WhiteThumbs);
        Assert.True(SkinA.SquareThumbs);
        Assert.True(SkinA.NoRoundThumbs);
        Assert.True(SkinA.NoBlueAccent);
        Assert.True(SkinA.NoBlueThumbs);
        Assert.True(SkinA.PlayTriangleIsOnlyRed);
        Assert.Equal("#E10600", SkinA.PlayTriangle);
        Assert.NotEqual(SkinA.PlayTriangle, SkinA.Accent);
        Assert.False(SkinA.CircularIconHover);
        Assert.False(SkinA.HoverIsCircularEightPercent);
        Assert.True(SkinA.HoverIsEightPercentWhite);
        Assert.Equal("#14FFFFFF", SkinA.HoverWhite);
        Assert.False(SkinA.PlayIsCircularCapsule);
        Assert.True(SkinA.CtaIsCapsuleOverlay);
        Assert.True(SkinA.FailureIsBannerSlot);
        Assert.True(SkinA.NoGlow);
        Assert.True(SkinA.NoBoxyButtons);
        Assert.True(SkinA.NoEmoji);
        Assert.True(SkinA.BorderlessIcons);
    }

    [Fact]
    public void App_resources_drop_blue_accent_and_round_thumbs()
    {
        var appXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "App.xaml"));
        var mainXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml"));
        var seriesXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "SeriesPage.xaml"));
        var urlXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "OpenUrlDialog.xaml"));
        var converter = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "SkinCProgressBrushConverter.cs"));
        var codeBehind = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml.cs"));

        Assert.DoesNotContain("0A84FF", appXaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0A84FF", mainXaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0A84FF", seriesXaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0A84FF", urlXaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0A84FF", converter, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0A84FF", codeBehind, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Text=\"|\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("<Rectangle Width=\"12\" Height=\"12\" Fill=\"{StaticResource SkinAThumbBrush}\"/>", appXaml, StringComparison.Ordinal);
        Assert.Contains("<Ellipse Width=\"10\" Height=\"10\" Fill=\"{StaticResource SeriesOnAccentBrush}\"/>", appXaml, StringComparison.Ordinal);
        Assert.Contains("C6FF00", appXaml, StringComparison.OrdinalIgnoreCase);
        var iconStyle = SliceBetween(appXaml, "x:Key=\"IconButton\"", "x:Key=\"SkinATextButton\"");
        var textStyle = SliceBetween(appXaml, "x:Key=\"SkinATextButton\"", "x:Key=\"SkinAPlayButton\"");
        Assert.Contains("<Setter Property=\"Padding\" Value=\"4\"/>", iconStyle, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Padding\" Value=\"4\"/>", textStyle, StringComparison.Ordinal);
        Assert.Contains("MinWidth\" Value=\"52\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("MinWidth\" Value=\"72\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("Padding\" Value=\"16,8\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("Padding\" Value=\"8,4\"", seriesXaml, StringComparison.Ordinal);
        Assert.Contains("Padding\" Value=\"14,6\"", seriesXaml, StringComparison.Ordinal);
        Assert.Contains("BorderBrush=\"#66222222\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"NextCtaButton\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("E10600", appXaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CornerRadius=\"2\"", urlXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("CornerRadius=\"4\"", urlXaml, StringComparison.Ordinal);
        Assert.Contains("Stop", Enum.GetNames<TransportControl>());
        Assert.Contains("Hamburger", Enum.GetNames<TransportControl>());
        Assert.DoesNotContain("SpaceX", mainXaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("xAI", mainXaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Grok", mainXaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Skin_a_sheet_tokens_stay_white_while_main_shell_is_serieson()
    {
        var shell = PlayerShell.Boot();
        Assert.Equal("#FFFFFF", SkinA.Accent);
        Assert.True(SkinA.AccentIsWhite);
        Assert.Equal(new[] { "퀵메뉴" }, shell.Menus);
        Assert.Equal(28, shell.Sidebar.RailWidthPx);
        Assert.False(shell.CenterPlayIcon);
        Assert.True(shell.VideoFullBleed);
        Assert.True(shell.NoLetterboxChrome);
        Assert.True(shell.NextEpisode.EndRegionOnly);
        Assert.False(shell.NextEpisode.OnTransport);
        Assert.Equal("다음 화 >", shell.NextEpisode.Label);
        Assert.Equal("-10초", shell.Transport.SkipBackLabel);
        Assert.Equal("+10초", shell.Transport.SkipForwardLabel);
        Assert.False(shell.Transport.SkipLabelsOnBar);
        Assert.Equal("1.0x", UiCopy.SpeedDefault);
        Assert.Equal("CC", UiCopy.Captions);
        Assert.False(shell.Volume.VerticalPopover);
        Assert.True(shell.Volume.HorizontalSliderOnTransport);
        Assert.True(shell.Status.FailureOnly);
        Assert.True(shell.Status.HideWhenIdle);
        Assert.False(shell.Status.Visible);
        Assert.Equal(
            new[]
            {
                TransportControl.Rewind,
                TransportControl.PlayPause,
                TransportControl.Stop,
                TransportControl.Clear,
                TransportControl.FastForward,
                TransportControl.Seek,
                TransportControl.Volume,
                TransportControl.Time,
                TransportControl.Hamburger
            },
            shell.Transport.Order);
    }

    private static string ReadRepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(relative);
    }

    private static string SliceBetween(string text, string start, string end)
    {
        var from = text.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, start);
        var until = text.IndexOf(end, from, StringComparison.Ordinal);
        Assert.True(until > from, end);
        return text[from..until];
    }
}
