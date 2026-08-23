using Microsoft.Win32;
using VideoPlayer.Core.Media;
using System.IO;

namespace VideoPlayer.App.Hosting;

public static class ExplorerRegistration
{
    public static void Register()
    {
        var exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "VideoPlayer.exe");
        foreach (var ext in SupportedFormats.Containers)
        {
            using var key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\SystemFileAssociations\{ext}\shell\VideoPlayer");
            key?.SetValue(null, "영상 플레이어로 재생");
            using var command = key?.CreateSubKey("command");
            command?.SetValue(null, $"\"{exe}\" \"%1\"");
        }
    }
}
