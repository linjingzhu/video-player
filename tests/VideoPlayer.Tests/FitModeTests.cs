using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.Tests;

public class FitModeTests
{
    [Theory]
    [InlineData(null, "contain")]
    [InlineData("", "contain")]
    [InlineData("contain", "contain")]
    [InlineData("COVER", "cover")]
    [InlineData("fill", "contain")]
    public void Unknown_values_stay_on_contain(string? stored, string expected)
        => Assert.Equal(expected, FitModes.Normalize(stored));

    [Fact]
    public void Fit_mode_is_one_appdata_value_and_is_applied_on_launch()
    {
        Assert.Equal("화면 맞춤", FitModes.MenuHeader);
        Assert.Equal("맞춤", FitModes.ContainLabel);
        Assert.Equal("채우기", FitModes.CoverLabel);

        using var workspace = new TempWorkspace();
        var first = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Equal(FitModes.Contain, first.FitMode);
        Assert.Equal(FitModes.Contain, ((FakeMediaEngine)first.Engine).AppliedFitMode);

        first.SetFitMode("cover");
        Assert.Equal(FitModes.Cover, first.FitMode);
        Assert.Equal(FitModes.Cover, ((FakeMediaEngine)first.Engine).AppliedFitMode);

        var json = File.ReadAllText(Path.Combine(workspace.Data, "window.json"));
        Assert.Contains("\"fitMode\": \"cover\"", json, StringComparison.Ordinal);
        Assert.Equal(1, Count(json, "fitMode"));

        var reopened = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Equal(FitModes.Cover, reopened.FitMode);
        Assert.Equal(FitModes.Cover, ((FakeMediaEngine)reopened.Engine).AppliedFitMode);

        reopened.SetFitMode("nope");
        Assert.Equal(FitModes.Contain, reopened.FitMode);
        Assert.Equal(FitModes.Contain, WindowMemory.FromJson(File.ReadAllText(Path.Combine(workspace.Data, "window.json"))).FitMode);
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
