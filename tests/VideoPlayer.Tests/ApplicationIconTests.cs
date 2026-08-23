using System.Buffers.Binary;

namespace VideoPlayer.Tests;

public class ApplicationIconTests
{
    [Fact]
    public void Application_icon_is_ieseo_ico_with_required_sizes()
    {
        var csproj = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "VideoPlayer.App.csproj"));
        Assert.Contains("<ApplicationIcon>Assets\\Ieseo.ico</ApplicationIcon>", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("<ApplicationIcon />", csproj, StringComparison.Ordinal);
        Assert.Contains("<Resource Include=\"Assets\\Ieseo.ico\" />", csproj, StringComparison.Ordinal);

        var ico = ReadRepoBytes(Path.Combine("src", "VideoPlayer.App", "Assets", "Ieseo.ico"));
        Assert.True(ico.Length > 0);
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(0, 2)));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(2, 2)));
        var count = BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4, 2));
        Assert.Equal(4, count);

        var sizes = new int[count];
        for (var i = 0; i < count; i++)
        {
            var width = ico[6 + i * 16];
            sizes[i] = width == 0 ? 256 : width;
        }

        Assert.Equal(new[] { 16, 32, 48, 256 }, sizes);
    }

    [Fact]
    public void Window_icon_is_ieseo_ico()
    {
        var mainXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml"));
        var windowTagEnd = mainXaml.IndexOf('>');
        Assert.True(windowTagEnd > 0);
        var windowTag = mainXaml[..windowTagEnd];
        Assert.Contains("Icon=\"Assets/Ieseo.ico\"", windowTag, StringComparison.Ordinal);
        Assert.Contains("WindowStyle=\"None\"", windowTag, StringComparison.Ordinal);
    }

    [Fact]
    public void Logo_is_not_placed_on_caption_or_transport()
    {
        var mainXaml = ReadRepoFile(Path.Combine("src", "VideoPlayer.App", "MainWindow.xaml"));

        var captionFrom = mainXaml.IndexOf("x:Name=\"CaptionBar\"", StringComparison.Ordinal);
        Assert.True(captionFrom >= 0);
        var captionUntil = mainXaml.IndexOf("x:Name=\"StatusBar\"", captionFrom, StringComparison.Ordinal);
        Assert.True(captionUntil > captionFrom);
        var caption = mainXaml[captionFrom..captionUntil];
        Assert.DoesNotContain("<Image", caption, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Ieseo.ico", caption, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Icon=", caption, StringComparison.Ordinal);

        var transportFrom = mainXaml.IndexOf("x:Name=\"TransportBar\"", StringComparison.Ordinal);
        Assert.True(transportFrom >= 0);
        var transport = mainXaml[transportFrom..];
        Assert.DoesNotContain("<Image", transport, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Ieseo", transport, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Icon=", transport, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(string relative) => File.ReadAllText(FindRepoFile(relative));

    private static byte[] ReadRepoBytes(string relative) => File.ReadAllBytes(FindRepoFile(relative));

    private static string FindRepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(relative);
    }
}
