using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using VideoPlayer.Core.Library;

namespace VideoPlayer.App;

public partial class MainWindow
{
    [ModuleInitializer]
    internal static void RegisterRecentFilesMenu()
        => ShellMenuHooks.Register(window => window.ApplyRecentMenu());

    private void ApplyRecentMenu()
    {
        if (SharedMenusAreOpen)
        {
            return;
        }

        var entries = RecentFilesMenu.Build(_session.Recent.Items, File.Exists);
        foreach (var menu in new[] { QuickMenuButton.ContextMenu, HamburgerButton.ContextMenu })
        {
            var parent = FindMenuByTag(menu, "recent");
            if (parent is null)
            {
                continue;
            }

            parent.Items.Clear();
            foreach (var entry in entries)
            {
                var item = new MenuItem
                {
                    Header = entry.Label,
                    Tag = "recent-file",
                    Uid = entry.Path
                };
                ShellMenuHooks.Style(item, entry.Missing);
                item.Click += RecentFile_Click;
                parent.Items.Add(item);
            }

            parent.Items.Add(new Separator());
            var clear = new MenuItem
            {
                Header = RecentFilesMenu.ClearLabel,
                Tag = "recentClear"
            };
            ShellMenuHooks.Style(clear, muted: false);
            clear.Click += ClearRecent_Click;
            parent.Items.Add(clear);
        }
    }

    private void RecentFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Uid: { Length: > 0 } path })
        {
            return;
        }

        if (!File.Exists(path))
        {
            _session.RemoveRecentFile(path);
        }
        else
        {
            _session.Open(path);
            ShowMainPage();
        }

        RefreshShell();
    }

    private void ClearRecent_Click(object sender, RoutedEventArgs e)
    {
        _session.ClearRecentFiles();
        RefreshShell();
    }
}
