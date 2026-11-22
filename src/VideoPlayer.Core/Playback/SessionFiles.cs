namespace VideoPlayer.Core.Playback;

/// <summary>
/// Settings and resume files live under AppData. A full disk, a locked file,
/// or a directory the user cannot write must not take the player down.
/// </summary>
public static class SessionFiles
{
    public static bool IsStorageFailure(Exception ex)
        => ex is IOException or UnauthorizedAccessException or NotSupportedException;

    public static bool TryEnsureDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            return true;
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            return false;
        }
    }

    public readonly record struct SessionRead(bool Readable, string? Text)
    {
        public static SessionRead Missing => new(true, null);

        public static SessionRead Unreadable => new(false, null);
    }

    /// <summary>
    /// Missing is readable (the store may be created). A lock or permission
    /// error is not missing: callers must not replace that file with defaults.
    /// </summary>
    public static SessionRead TryRead(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return SessionRead.Missing;
            }

            return new SessionRead(true, File.ReadAllText(path));
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            return SessionRead.Unreadable;
        }
    }

    public static bool TryWrite(string directory, string name, string contents)
    {
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name), contents);
            return true;
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            return false;
        }
    }
}
