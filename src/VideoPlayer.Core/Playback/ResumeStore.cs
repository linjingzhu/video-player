using System.Text.Json;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Core.Playback;

public sealed class ResumeStore
{
    private readonly Dictionary<string, ResumeEntry> _entries = new(StringComparer.Ordinal);
    private ContinueWatching? _continue;

    public IReadOnlyDictionary<string, ResumeEntry> Entries => _entries;

    public ContinueWatching? Continue => _continue;

    public void Apply(CheckpointResult result)
    {
        _entries[result.Current.Key] = result.Current;
        if (!result.CurrentCompleted)
        {
            _continue = new ContinueWatching(
                result.Current.Path,
                result.Current.Size,
                result.Current.PositionSeconds,
                result.Current.Key);
            return;
        }

        if (_continue?.Key == result.Current.Key)
        {
            _continue = null;
        }
    }

    public ResumeEntry? Find(string path, long size)
        => FindKey(ResumeKey.From(path, size));

    public ResumeEntry? Find(MediaIdentity identity)
        => FindKey(identity.Key);

    public ResumeEntry? FindUrl(string url)
        => FindKey(ResumeKey.FromUrl(url));

    public double PositionOrZero(string path, long size)
        => Find(path, size) is { Completed: false } entry ? entry.PositionSeconds : 0;

    public double PositionOrZero(MediaIdentity identity)
        => Find(identity) is { Completed: false } entry ? entry.PositionSeconds : 0;

    private ResumeEntry? FindKey(string key)
        => _entries.TryGetValue(key, out var entry) ? entry : null;

    public string ToJson()
    {
        var state = new ResumeState
        {
            Entries = [.. _entries.Values],
            Continue = _continue
        };
        return JsonSerializer.Serialize(state, JsonOptions);
    }

    public static ResumeStore FromJson(string? json)
    {
        var store = new ResumeStore();
        if (string.IsNullOrWhiteSpace(json))
        {
            return store;
        }

        ResumeState? state;
        try
        {
            state = JsonSerializer.Deserialize<ResumeState>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return store;
        }

        if (state?.Entries is null)
        {
            return store;
        }

        foreach (var entry in state.Entries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Path))
            {
                continue;
            }

            if (OpenUrlRules.IsAcceptedHttpUrl(entry.Path))
            {
                if (!string.Equals(entry.Key, entry.Path, StringComparison.Ordinal))
                {
                    continue;
                }

                store._entries[entry.Key] = entry;
                continue;
            }

            if (PathValidator.IsRemoteUri(entry.Path) || PathValidator.LooksLikeUnc(entry.Path))
            {
                continue;
            }

            store._entries[entry.Key] = entry;
        }

        if (state.Continue is { } pointer && store._entries.ContainsKey(pointer.Key))
        {
            store._continue = pointer;
        }

        return store;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private sealed class ResumeState
    {
        public List<ResumeEntry> Entries { get; set; } = [];
        public ContinueWatching? Continue { get; set; }
    }
}

public sealed record ContinueWatching(string Path, long Size, double PositionSeconds, string Key);
