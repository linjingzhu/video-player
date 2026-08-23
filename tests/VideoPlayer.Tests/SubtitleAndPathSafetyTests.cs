using VideoPlayer.Core.Safety;
using VideoPlayer.Core.Subtitles;

namespace VideoPlayer.Tests;

public class SubtitleAndPathSafetyTests
{
    [Fact]
    public void Same_folder_same_name_srt_and_smi_are_found()
    {
        var dir = Directory.CreateTempSubdirectory("subs-match-");
        try
        {
            var video = Path.Combine(dir.FullName, "S01E01.mkv");
            File.WriteAllBytes(video, [1, 2, 3]);
            File.WriteAllText(Path.Combine(dir.FullName, "S01E01.srt"), "1\n00:00:00,000 --> 00:00:01,000\n안녕\n");
            File.WriteAllText(Path.Combine(dir.FullName, "S01E01.smi"), "<SAMI><BODY><SYNC Start=0>안녕</BODY>");
            var found = SubtitleLocator.FindSidecars(video);
            Assert.Equal(2, found.Count);
            Assert.All(found, path => Assert.True(PathValidator.IsSameDirectory(video, path)));
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Fact]
    public void Subtitle_lookup_does_not_follow_traversal_names()
    {
        var dir = Directory.CreateTempSubdirectory("subs-trav-");
        try
        {
            var video = Path.Combine(dir.FullName, "episode.mkv");
            File.WriteAllBytes(video, [1]);
            var outside = Path.Combine(dir.FullName, "..", "secret.srt");
            var rejected = SubtitleLocator.AcceptExternalSubtitle(video, outside);
            Assert.False(rejected.Success);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://example.com/movie.mkv")]
    [InlineData("https://cdn.example/a.mkv")]
    [InlineData("rtsp://camera/1")]
    [InlineData(@"\\evil\share\movie.mkv")]
    [InlineData("../outside.mkv")]
    [InlineData("foo/../../etc/passwd")]
    public void Remote_and_traversal_paths_are_rejected(string? path)
    {
        var result = PathValidator.ValidateLocalFilePath(path);
        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void Null_byte_in_path_is_rejected()
        => Assert.False(PathValidator.ValidateLocalFilePath("/tmp/ok\0.mkv").Success);

    [Fact]
    public void Oversized_srt_is_not_parsed()
    {
        var dir = Directory.CreateTempSubdirectory("subs-big-");
        try
        {
            var path = Path.Combine(dir.FullName, "big.srt");
            File.WriteAllBytes(path, new byte[SubtitleParser.MaxFileBytes + 16]);
            var parsed = SubtitleParser.ParseFile(path);
            Assert.Empty(parsed.Cues);
            Assert.True(parsed.Truncated);
            Assert.Contains("너무 커서", parsed.Warning);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Fact]
    public void Malformed_srt_skips_bad_blocks()
    {
        const string srt = """
            not-a-number
            broken --> time
            nope

            2
            00:00:01,000 --> 00:00:02,000
            정상 자막
            """;
        var parsed = SubtitleParser.ParseSrt(srt);
        Assert.Single(parsed.Cues);
        Assert.Equal("정상 자막", parsed.Cues[0].Text);
    }

    [Fact]
    public void Smi_strips_markup_and_script_like_tags()
    {
        const string smi = """
            <SAMI>
            <BODY>
            <SYNC Start=1000>
            <P>안녕<script>alert(1)</script><b>친구</b>
            <SYNC Start=2000>
            <P>&nbsp;
            """;
        var parsed = SubtitleParser.ParseSmi(smi);
        Assert.Contains(parsed.Cues, c => c.Text.Contains("안녕", StringComparison.Ordinal));
        Assert.DoesNotContain(parsed.Cues, c => c.Text.Contains("alert", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(parsed.Cues, c => c.Text.Contains("<script", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Malicious_filename_is_sanitized_for_display()
    {
        var raw = "../secret\u202Etxt.exe.mkv";
        Assert.True(FileNameSanitizer.LooksMalicious(raw));
        var display = FileNameSanitizer.ForDisplay(raw);
        Assert.DoesNotContain("..", display);
        Assert.DoesNotContain('\u202E', display);
        Assert.DoesNotContain('/', display);
        Assert.DoesNotContain('\\', display);
    }

    [Fact]
    public void External_subtitle_must_share_the_video_folder()
    {
        var a = Directory.CreateTempSubdirectory("media-a-");
        var b = Directory.CreateTempSubdirectory("media-b-");
        try
        {
            var video = Path.Combine(a.FullName, "a.mkv");
            var sub = Path.Combine(b.FullName, "a.srt");
            File.WriteAllBytes(video, [1]);
            File.WriteAllText(sub, "1\n00:00:00,000 --> 00:00:01,000\nHi\n");
            Assert.False(SubtitleLocator.AcceptExternalSubtitle(video, sub).Success);
        }
        finally
        {
            a.Delete(true);
            b.Delete(true);
        }
    }
}
