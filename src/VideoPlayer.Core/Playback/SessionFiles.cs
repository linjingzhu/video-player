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

    public static string? TryRead(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            return File.ReadAllText(path);
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            return null;
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
