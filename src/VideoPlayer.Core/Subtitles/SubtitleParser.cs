using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VideoPlayer.Core.Subtitles;

public sealed record SubtitleCue(TimeSpan Start, TimeSpan End, string Text);

public sealed class SubtitleParseResult
{
    public required IReadOnlyList<SubtitleCue> Cues { get; init; }
    public required bool Truncated { get; init; }
    public string? Warning { get; init; }
}

public static class SubtitleParser
{
    public const int MaxFileBytes = 2 * 1024 * 1024;
    public const int MaxCues = 5000;
    public const int MaxLineChars = 500;

    public static SubtitleParseResult ParseFile(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            return new SubtitleParseResult { Cues = [], Truncated = false, Warning = "자막 파일이 없습니다." };
        }

        if (info.Length > MaxFileBytes)
        {
            return new SubtitleParseResult
            {
                Cues = [],
                Truncated = true,
                Warning = "자막 파일이 너무 커서 불러오지 않았습니다."
            };
        }

        string text;
        try
        {
            text = ReadTextLimited(path, MaxFileBytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DecoderFallbackException)
        {
            return new SubtitleParseResult { Cues = [], Truncated = false, Warning = "자막을 읽을 수 없습니다." };
        }

        var ext = Path.GetExtension(path);
        return ext.Equals(".smi", StringComparison.OrdinalIgnoreCase) || ext.Equals(".sami", StringComparison.OrdinalIgnoreCase)
            ? ParseSmi(text)
            : ParseSrt(text);
    }

    public static SubtitleParseResult ParseSrt(string text)
    {
        var cues = new List<SubtitleCue>();
        var blocks = Regex.Split(text.Replace("\r\n", "\n"), @"\n\s*\n");
        var truncated = false;

        foreach (var block in blocks)
        {
            if (cues.Count >= MaxCues)
            {
                truncated = true;
                break;
            }

            if (TryParseSrtBlock(block, out var cue) && cue is not null)
            {
                cues.Add(cue);
            }
        }

        return new SubtitleParseResult
        {
            Cues = cues,
            Truncated = truncated,
            Warning = truncated ? "자막 항목이 너무 많아 일부를 건너뛰었습니다." : null
        };
    }

    public static SubtitleParseResult ParseSmi(string text)
    {
        var cues = new List<SubtitleCue>();
        var truncated = false;
        var matches = Regex.Matches(
            text,
            @"<sync\s+start\s*=\s*""?(?<start>\d+)""?\s*(?:end\s*=\s*""?(?<end>\d+)""?)?[^>]*>(?<body>.*?)(?=<sync|</body|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        for (var i = 0; i < matches.Count; i++)
        {
            if (cues.Count >= MaxCues)
            {
                truncated = true;
                break;
            }

            if (!int.TryParse(matches[i].Groups["start"].Value, out var startMs))
            {
                continue;
            }

            var endMs = 0;
            if (matches[i].Groups["end"].Success)
            {
                _ = int.TryParse(matches[i].Groups["end"].Value, out endMs);
            }
            else if (i + 1 < matches.Count)
            {
                _ = int.TryParse(matches[i + 1].Groups["start"].Value, out endMs);
            }

            if (endMs <= startMs)
            {
                endMs = startMs + 3000;
            }

            var body = SanitizeCueText(StripHtml(matches[i].Groups["body"].Value));
            if (string.IsNullOrWhiteSpace(body))
            {
                continue;
            }

            cues.Add(new SubtitleCue(TimeSpan.FromMilliseconds(startMs), TimeSpan.FromMilliseconds(endMs), body));
        }

        return new SubtitleParseResult
        {
            Cues = cues,
            Truncated = truncated,
            Warning = truncated ? "자막 항목이 너무 많아 일부를 건너뛰었습니다." : null
        };
    }

    public static string CueAt(IReadOnlyList<SubtitleCue> cues, TimeSpan position)
    {
        foreach (var cue in cues)
        {
            if (position >= cue.Start && position < cue.End)
            {
                return cue.Text;
            }
        }

        return "";
    }

    private static bool TryParseSrtBlock(string block, out SubtitleCue? cue)
    {
        cue = null;
        var lines = block.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            return false;
        }

        var timeIndex = lines[0].Contains("-->", StringComparison.Ordinal) ? 0 : 1;
        if (timeIndex >= lines.Length || !TryParseSrtRange(lines[timeIndex], out var start, out var end))
        {
            return false;
        }

        var text = SanitizeCueText(string.Join("\n", lines.Skip(timeIndex + 1)));
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        cue = new SubtitleCue(start, end, text);
        return true;
    }

    private static bool TryParseSrtRange(string line, out TimeSpan start, out TimeSpan end)
    {
        start = default;
        end = default;
        var parts = line.Split("-->", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 && TryParseSrtTime(parts[0], out start) && TryParseSrtTime(parts[1], out end) && end > start;
    }

    private static bool TryParseSrtTime(string value, out TimeSpan time)
    {
        value = value.Trim();
        var formats = new[] { @"hh\:mm\:ss\,fff", @"h\:mm\:ss\,fff", @"hh\:mm\:ss\.fff", @"h\:mm\:ss\.fff" };
        return TimeSpan.TryParseExact(value, formats, CultureInfo.InvariantCulture, out time);
    }

    private static string StripHtml(string html)
    {
        var noScript = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var noTags = Regex.Replace(noScript, @"<[^>]+>", " ");
        return Regex.Replace(noTags, @"&nbsp;|&amp;|&lt;|&gt;|&quot;", match => match.Value switch
        {
            "&nbsp;" => " ",
            "&amp;" => "&",
            "&lt;" => "<",
            "&gt;" => ">",
            "&quot;" => "\"",
            _ => ""
        }, RegexOptions.IgnoreCase);
    }

    private static string SanitizeCueText(string text)
    {
        var builder = new StringBuilder();
        foreach (var ch in text)
        {
            if (ch is '\n' or '\t' || !char.IsControl(ch))
            {
                builder.Append(ch);
            }

            if (builder.Length >= MaxLineChars)
            {
                break;
            }
        }

        return Regex.Replace(builder.ToString(), @"[ \t]+", " ").Trim();
    }

    private static string ReadTextLimited(string path, int maxBytes)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var length = (int)Math.Min(stream.Length, maxBytes);
        var buffer = new byte[length];
        var read = stream.Read(buffer, 0, length);
        if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(buffer, 3, read - 3);
        }

        if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(buffer, 2, read - 2);
        }

        return Encoding.UTF8.GetString(buffer, 0, read);
    }
}
