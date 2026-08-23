namespace VideoPlayer.Core.Playback;

public readonly record struct MediaSubtitleTrack(
    int Id,
    string? Language,
    string? Title,
    bool Embedded);
