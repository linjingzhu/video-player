namespace VideoPlayer.Core.Media;

public static class SupportedFormats
{
    public static readonly IReadOnlySet<string> Containers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".wmv", ".mov"
    };

    public static readonly IReadOnlySet<string> VideoCodecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "h264", "avc", "avc1", "hevc", "h265", "hev1", "hvc1",
        "vp9", "vp09", "av1", "av01", "mpeg4", "mpeg-4", "xvid", "divx"
    };

    public static readonly IReadOnlySet<string> AudioCodecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "aac", "mp4a", "ac3", "ac-3", "eac3", "eac-3", "ec-3",
        "mp3", "mpga", "flac", "opus", "pcm", "pcm_s16le", "pcm_s24le", "pcm_s32le", "pcm_bluray"
    };

    public static readonly IReadOnlySet<string> SubtitleExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".srt", ".smi", ".sami"
    };

    public static readonly IReadOnlySet<string> OutOfScopeCodecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "prores", "apcn", "apch", "apco", "ap4h",
        "dnxhd", "dnxhr",
        "braw", "r3d", "ari", "cinema dng", "raw",
        "wmv-drm", "wmv_drm", "mss2"
    };

    public static readonly IReadOnlySet<string> OutOfScopeContainers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".iso", ".ifo", ".vob", ".braw", ".r3d", ".dng"
    };

    public static bool IsSupportedContainer(string? pathOrExtension)
    {
        var ext = NormalizeExtension(pathOrExtension);
        return ext is not null && Containers.Contains(ext);
    }

    public static bool IsOutOfScopeContainer(string? pathOrExtension)
    {
        var ext = NormalizeExtension(pathOrExtension);
        return ext is not null && OutOfScopeContainers.Contains(ext);
    }

    public static bool IsSupportedVideoCodec(string? codec)
        => codec is not null && VideoCodecs.Contains(NormalizeCodec(codec));

    public static bool IsSupportedAudioCodec(string? codec)
        => codec is not null && AudioCodecs.Contains(NormalizeCodec(codec));

    public static bool IsOutOfScopeCodec(string? codec)
        => codec is not null && OutOfScopeCodecs.Contains(NormalizeCodec(codec));

    public static string NormalizeCodec(string codec)
    {
        var value = codec.Trim().ToLowerInvariant();
        value = value.Replace("video/", "", StringComparison.Ordinal)
            .Replace("audio/", "", StringComparison.Ordinal);
        if (value.StartsWith("v_", StringComparison.Ordinal))
        {
            value = value[2..];
        }

        if (value.StartsWith("a_", StringComparison.Ordinal))
        {
            value = value[2..];
        }

        return value switch
        {
            "avc1" or "avc" or "x264" => "h264",
            "hev1" or "hvc1" or "h265" => "hevc",
            "vp09" => "vp9",
            "av01" => "av1",
            "xvid" or "divx" or "mp4v" or "mpeg-4 visual" or "mpeg-4 asp" => "mpeg4",
            "ac-3" => "ac3",
            "eac-3" or "ec-3" or "e-ac-3" => "eac3",
            "mp4a" => "aac",
            _ => value
        };
    }

    public static string DisplayCodecName(string? codec)
    {
        if (string.IsNullOrWhiteSpace(codec))
        {
            return "알 수 없음";
        }

        return NormalizeCodec(codec) switch
        {
            "h264" => "H.264",
            "hevc" => "HEVC",
            "vp9" => "VP9",
            "av1" => "AV1",
            "mpeg4" => "MPEG-4 ASP",
            "aac" => "AAC",
            "ac3" => "AC-3",
            "eac3" => "E-AC-3",
            "mp3" => "MP3",
            "flac" => "FLAC",
            "opus" => "Opus",
            "pcm" or "pcm_s16le" or "pcm_s24le" or "pcm_s32le" => "PCM",
            var other => other.ToUpperInvariant()
        };
    }

    public static string? NormalizeExtension(string? pathOrExtension)
    {
        if (string.IsNullOrWhiteSpace(pathOrExtension))
        {
            return null;
        }

        var value = pathOrExtension.Trim();
        if (value.Contains('.', StringComparison.Ordinal) && !value.StartsWith('.'))
        {
            value = Path.GetExtension(value);
        }

        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value.StartsWith('.') ? value.ToLowerInvariant() : "." + value.ToLowerInvariant();
    }
}
