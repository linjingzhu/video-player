using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class SkinCTokenTests
{
    [Fact]
    public void Density_matches_skin_a()
    {
        Assert.Equal(SkinA.BodySize, SkinC.BodySize);
        Assert.Equal(12, SkinC.BodySize);
        Assert.Equal(20, SkinC.TitleSize);
        Assert.Equal(11, SkinC.MetaSize);
        Assert.Equal(new[] { 4, 8, 12, 16 }, SkinC.SpacingScale);
        Assert.DoesNotContain(20, SkinC.SpacingScale);
        Assert.DoesNotContain(24, SkinC.SpacingScale);
        Assert.Equal(4, SkinC.SpacingMin);
        Assert.Equal(16, SkinC.SpacingMax);
        Assert.Equal(SkinA.SpacingScale, SkinC.SpacingScale);
    }

    [Fact]
    public void Colors_match_shared_lock()
    {
        Assert.Equal("#050505", SkinC.Background);
        Assert.Equal("#0E0E0E", SkinC.Panel);
        Assert.Equal("#222222", SkinC.Border);
        Assert.Equal(0.40, SkinC.BorderOpacity);
        Assert.Equal("#FFFFFF", SkinC.Text);
        Assert.Equal("#8A8A8A", SkinC.Secondary);
        Assert.Equal("#FFFFFF", SkinC.Accent);
        Assert.Equal(SkinA.Accent, SkinC.Accent);
        Assert.Equal(2, SkinC.RadiusControl);
        Assert.Equal(4, SkinC.RadiusPanel);
        Assert.Equal(2, SkinC.RadiusWindow);
        Assert.Equal("Segoe UI Variable", SkinC.FontFamily);
        Assert.True(SkinC.SelectionIsPanelPlusHairline);
        Assert.True(SkinC.BorderlessFolderIcons);
        Assert.False(SkinC.HoverIsCircularEightPercent);
        Assert.NotEqual("#0A84FF", SkinC.Accent);
    }
}
