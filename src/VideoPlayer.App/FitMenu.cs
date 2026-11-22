using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.App;

public partial class MainWindow
{
    [ModuleInitializer]
    internal static void RegisterFitMenu()
        => ShellMenuHooks.Register(window => window.ApplyFitMenu());

    private void ApplyFitMenu()
    {
        var cover = FitModes.IsCover(_session.FitMode);
        foreach (var menu in new[] { QuickMenuButton.ContextMenu, HamburgerButton.ContextMenu })
        {
            var parent = FindMenuByTag(menu, "fit");
            if (parent is null)
            {
                continue;
            }

            parent.Header = FitModes.MenuHeader;
            ShellMenuHooks.Style(parent, muted: false);
            foreach (var child in parent.Items.OfType<MenuItem>())
            {
                child.IsChecked = child.Tag as string == (cover ? "fit:cover" : "fit:contain");
                ShellMenuHooks.Style(child, muted: false);
            }
        }
    }

    private void FitMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string tag } || !tag.StartsWith("fit:", StringComparison.Ordinal))
        {
            return;
        }

        _session.SetFitMode(tag["fit:".Length..]);
        RefreshShell();
    }
}
