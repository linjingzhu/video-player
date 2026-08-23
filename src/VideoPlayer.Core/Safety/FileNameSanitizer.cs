using System.Text;

namespace VideoPlayer.Core.Safety;

/// <summary>
/// Display-only sanitization for untrusted file and subtitle names.
/// Never used to open a path the user did not select.
/// </summary>
public static class FileNameSanitizer
{
    public const int MaxDisplayLength = 180;

    private static readonly HashSet<char> Dangerous =
    [
        '\0', '\u202A', '\u202B', '\u202C', '\u202D', '\u202E',
        '\u2066', '\u2067', '\u2068', '\u2069'
    ];

    public static string ForDisplay(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "(이름 없음)";
        }

        var builder = new StringBuilder(Math.Min(name.Length, MaxDisplayLength + 1));
        foreach (var ch in name)
        {
            if (Dangerous.Contains(ch) || char.IsControl(ch))
            {
                builder.Append('_');
                continue;
            }

            if (ch is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
            {
                builder.Append('_');
                continue;
            }

            builder.Append(ch);
            if (builder.Length >= MaxDisplayLength)
            {
                builder.Append('…');
                break;
            }
        }

        var result = builder.ToString().Trim();
        while (result.Contains("..", StringComparison.Ordinal))
        {
            result = result.Replace("..", "·", StringComparison.Ordinal);
        }

        return result.Length == 0 ? "(이름 없음)" : result;
    }

    public static bool LooksMalicious(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (name.IndexOf('\0') >= 0)
        {
            return true;
        }

        if (name.Contains("..", StringComparison.Ordinal))
        {
            return true;
        }

        if (name.IndexOfAny(['/', '\\']) >= 0)
        {
            return true;
        }

        return name.Any(Dangerous.Contains);
    }
}
