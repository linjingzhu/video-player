using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VideoPlayer.Core.Skip;

/// <summary>
/// Designer-locked chapter aliases only. No IntroDB, accounts, or extra synonyms.
/// </summary>
public static class ChapterAliases
{
    public static IReadOnlyList<string> Locked { get; } =
    [
        "intro", "opening", "recap", "credits", "outro",
        "오프닝", "도입", "리캡", "예고", "엔딩", "크레딧"
    ];

    private static readonly string[] Recap = ["recap", "리캡", "예고"];
    private static readonly string[] Intro = ["intro", "opening", "오프닝", "도입"];
    private static readonly string[] Credits = ["credits", "outro", "엔딩", "크레딧"];

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

            if (key == needle)
            {
                return true;
            }

            if (IsHangul(needle[0]))
            {
                if (key.Contains(needle, StringComparison.Ordinal))
                {
                    return true;
                }

                continue;
            }

            if (Regex.IsMatch(key, $@"\b{Regex.Escape(needle)}\b", RegexOptions.CultureInvariant))
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
