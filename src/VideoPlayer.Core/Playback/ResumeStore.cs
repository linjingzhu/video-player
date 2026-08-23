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
        if (result.NextEpisodeAtZero is { } next)
        {
            _entries[next.Key] = next;
            _continue = new ContinueWatching(next.Path, next.Size, 0, next.Key);
            return;
        }

        if (!result.CurrentCompleted)
        {
            _continue = new ContinueWatching(
                result.Current.Path,
                result.Current.Size,
                result.Current.PositionSeconds,
                result.Current.Key);
        }
        else if (_continue?.Key == result.Current.Key)
        {
            _continue = null;
        }
    }

    public ResumeEntry? Find(string path, long size)
    {
        _entries.TryGetValue(ResumeKey.From(path, size), out var entry);
        return entry;
    }

    public double PositionOrZero(string path, long size)
        => Find(path, size) is { Completed: false } entry ? entry.PositionSeconds : 0;

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
            if (entry is null || string.IsNullOrWhiteSpace(entry.Key) || PathValidator.IsRemoteUri(entry.Path))
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
