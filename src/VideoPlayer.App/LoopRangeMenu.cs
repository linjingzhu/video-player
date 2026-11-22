using System.Runtime.CompilerServices;
using System.Windows;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.App;

public partial class MainWindow
{
    [ModuleInitializer]
    internal static void RegisterLoopRangeMenu()
        => ShellMenuHooks.Register(window => window.ApplyLoopMenu());

    private void ApplyLoopMenu()
    {
        _session.SyncLoopAvailability();
        foreach (var menu in new[] { QuickMenuButton.ContextMenu, HamburgerButton.ContextMenu })
        {
            var item = FindMenuByTag(menu, "loopRange");
            if (item is null)
            {
                continue;
            }

            var enabled = _session.LoopMenuEnabled;
            item.Header = enabled ? LoopRange.ItemHeader : LoopRange.NeedsMarksHeader;
            item.IsEnabled = enabled;
            item.IsCheckable = enabled;
            item.IsChecked = enabled && _session.LoopEnabled;
            ShellMenuHooks.Style(item, muted: !enabled);
        }
    }

    private void LoopRange_Click(object sender, RoutedEventArgs e)
    {
        _session.ToggleLoopRange();
        RefreshShell();
    }
}
