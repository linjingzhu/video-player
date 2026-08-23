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
        Assert.Equal("#0B0B0D", SkinC.Background);
        Assert.Equal("#141418", SkinC.Panel);
        Assert.Equal("#2C2C2E", SkinC.Border);
        Assert.Equal(0.20, SkinC.BorderOpacity);
        Assert.Equal("#F5F5F7", SkinC.Text);
        Assert.Equal("#8E8E93", SkinC.Secondary);
        Assert.Equal("#0A84FF", SkinC.Accent);
        Assert.Equal("Segoe UI Variable", SkinC.FontFamily);
        Assert.True(SkinC.SelectionIsPanelPlusHairline);
        Assert.True(SkinC.BorderlessFolderIcons);
        Assert.True(SkinC.HoverIsCircularEightPercent);
    }
}
