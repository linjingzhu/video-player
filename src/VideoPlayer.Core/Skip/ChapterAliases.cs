using System.Globalization;
using System.Text;

namespace VideoPlayer.Core.Skip;

/// <summary>Maps local chapter titles to skip kinds. No external skip database.</summary>
public static class ChapterAliases
{
    private static readonly string[] Recap =
    [
        "recap", "previously", "previously on", "last time", "previouslyon",
        "리캡", "지난이야기", "지난 이야기", "이전 줄거리", "지난줄거리"
    ];

    private static readonly string[] Intro =
    [
        "intro", "introduction", "opening", "opening credits", "op", "cold open",
        "title sequence", "main title",
        "인트로", "오프닝", "오프닝곡", "오프닝 크레딧", "오프닝크레딧"
    ];

    private static readonly string[] Credits =
    [
        "credits", "end credits", "ending", "ending credits", "ed", "outro",
        "closing credits", "end titles",
        "크레딧", "엔딩", "엔딩곡", "엔딩 크레딧", "엔딩크레딧"
    ];

    public static SkipKind? Classify(string? title)
    {
        var key = Normalize(title);
        if (key.Length == 0)
        {
            return null;
        }

        if (Matches(key, Recap))
        {
            return SkipKind.Recap;
        }

        if (Matches(key, Intro))
        {
            return SkipKind.Intro;
        }

        if (Matches(key, Credits))
        {
            return SkipKind.Credits;
        }

        return null;
    }

    public static string Normalize(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "";
        }

        var builder = new StringBuilder(title.Length);
        foreach (var ch in title.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ')
            {
                builder.Append(ch);
            }
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool Matches(string key, IReadOnlyList<string> aliases)
    {
        foreach (var alias in aliases)
        {
            var needle = Normalize(alias);
            if (needle.Length == 0)
            {
                continue;
            }

            if (key == needle || key.StartsWith(needle + " ", StringComparison.Ordinal)
                || key.EndsWith(" " + needle, StringComparison.Ordinal)
                || key.Contains(" " + needle + " ", StringComparison.Ordinal))
            {
                return true;
            }

            if (!needle.Contains(' ', StringComparison.Ordinal) && IsHangul(needle[0]) && key.Contains(needle, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHangul(char ch)
        => ch >= 0xAC00 && ch <= 0xD7A3
           || CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.OtherLetter;
}
