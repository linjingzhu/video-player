using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.App;

public partial class MainWindow
{
    [ModuleInitializer]
    internal static void RegisterSpeedMenu()
        => ShellMenuHooks.Register(window => window.ApplySpeedMenu());

    private void ApplySpeedMenu()
    {
        foreach (var menu in new[] { QuickMenuButton.ContextMenu, HamburgerButton.ContextMenu })
        {
            var parent = FindMenuByTag(menu, "speed");
            if (parent is null)
            {
                continue;
            }

            foreach (var child in parent.Items.OfType<MenuItem>())
            {
                if (child.Tag is not string tag
                    || !tag.StartsWith("speed:", StringComparison.Ordinal)
                    || !double.TryParse(tag.AsSpan("speed:".Length), NumberStyles.Float, CultureInfo.InvariantCulture, out var preset))
                {
                    continue;
                }

                child.IsChecked = PlaybackSpeed.IsPresetSelected(_session.Speed, preset);
                ShellMenuHooks.Style(child, muted: false);
            }
        }
    }

    private void SpeedPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string tag }
            || !tag.StartsWith("speed:", StringComparison.Ordinal)
            || !double.TryParse(tag.AsSpan("speed:".Length), NumberStyles.Float, CultureInfo.InvariantCulture, out var speed))
        {
            return;
        }

        _session.SetSpeed(speed, showOsd: true);
        RefreshShell();
    }
}
