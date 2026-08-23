using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class SkinATokenTests
{
    [Fact]
    public void Colors_match_confirmed_skin_a()
    {
        Assert.Equal("#0B0B0D", SkinA.Background);
        Assert.Equal("#141418", SkinA.Panel);
        Assert.Equal("#2C2C2E", SkinA.Border);
        Assert.Equal(0.20, SkinA.BorderOpacity);
        Assert.Equal("#F5F5F7", SkinA.Text);
        Assert.Equal("#8E8E93", SkinA.Secondary);
        Assert.Equal("#0A84FF", SkinA.Accent);
        Assert.Equal("#FFFFFF", SkinA.Thumb);
        Assert.Equal("#14FFFFFF", SkinA.HoverWhite);
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
    public void Chrome_material_is_blur_plus_eight_percent_white()
    {
        Assert.Equal("#14FFFFFF", SkinA.ChromeFill);
        Assert.Equal("#14FFFFFF", SkinA.HoverWhite);
        Assert.Equal(20, SkinA.ChromeBlurRadius);
        Assert.True(SkinA.ChromeIsBlurPlusWhite);
        Assert.True(SkinA.NoWireframeWindowTitle);
        Assert.True(SkinA.NoMockCaptionSentences);
        Assert.Equal("영상 플레이어", UiCopy.AppTitle);
        Assert.DoesNotContain("caption", UiCopy.AppTitle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Type_radii_spacing_and_chrome_sizes_match_confirmed_skin_a()
    {
        Assert.Equal("Segoe UI Variable", SkinA.FontFamily);
        Assert.Equal("Segoe UI", SkinA.FontFallback);
        Assert.Equal(999, SkinA.RadiusPill);
        Assert.Equal(12, SkinA.RadiusPanel);
        Assert.Equal(10, SkinA.RadiusControl);
        Assert.Equal(new[] { 4, 8, 12, 16, 20 }, SkinA.SpacingScale);
        Assert.DoesNotContain(24, SkinA.SpacingScale);
        Assert.Equal(4, SkinA.SpacingMin);
        Assert.Equal(20, SkinA.SpacingMax);
        Assert.NotEqual(24, SkinA.SpacingMax);
        Assert.Equal(40, SkinA.TransportHeightPx);
        Assert.Equal(28, SkinA.SidebarRailWidthPx);
        Assert.Equal(ShellLayout.SidebarRailWidthPx, SkinA.SidebarRailWidthPx);
    }

    [Fact]
    public void Accent_is_tracks_only_with_white_thumbs_and_capsule_chrome()
    {
        Assert.True(SkinA.AccentOnTracksOnly);
        Assert.True(SkinA.WhiteThumbs);
        Assert.True(SkinA.NoBlueThumbs);
        Assert.True(SkinA.BorderlessIcons);
        Assert.True(SkinA.CircularIconHover);
        Assert.True(SkinA.PlayIsCircularCapsule);
        Assert.True(SkinA.CtaIsCapsuleOverlay);
        Assert.True(SkinA.FailureIsBannerSlot);
        Assert.True(SkinA.NoGlow);
        Assert.True(SkinA.NoBoxyButtons);
        Assert.True(SkinA.NoEmoji);
    }

    [Fact]
    public void Skin_a_does_not_change_av2_shell_structure()
    {
        var shell = PlayerShell.Boot();
        Assert.Equal(new[] { "파일", "보기" }, shell.Menus);
        Assert.Equal(28, shell.Sidebar.RailWidthPx);
        Assert.False(shell.CenterPlayIcon);
        Assert.True(shell.NextEpisode.EndRegionOnly);
        Assert.False(shell.NextEpisode.OnTransport);
        Assert.Equal("다음 화 >", shell.NextEpisode.Label);
        Assert.True(shell.Status.FailureOnly);
        Assert.True(shell.Status.HideWhenIdle);
        Assert.False(shell.Status.Visible);
        Assert.Equal(
            new[]
            {
                TransportControl.PreviousEpisode,
                TransportControl.SkipBack,
                TransportControl.PlayPause,
                TransportControl.SkipForward,
                TransportControl.NextEpisode,
                TransportControl.Seek,
                TransportControl.Volume,
                TransportControl.Speed,
                TransportControl.Captions,
                TransportControl.Fullscreen
            },
            shell.Transport.Order);
    }
}
