using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.App;

public partial class MainWindow
{
    [ModuleInitializer]
    internal static void RegisterChapterMenu()
        => ShellMenuHooks.Register(window => window.ApplyChapterMenu());

    private void ApplyChapterMenu()
    {
        if (SharedMenusAreOpen)
        {
            return;
        }

        var entries = ChapterMenu.Build(_session.Chapters);
        foreach (var menu in new[] { QuickMenuButton.ContextMenu, HamburgerButton.ContextMenu })
        {
            var parent = FindMenuByTag(menu, "chapters");
            if (parent is null)
            {
                continue;
            }

            parent.Header = ChapterMenu.MenuHeader;
            parent.IsEnabled = true;
            ShellMenuHooks.Style(parent, muted: false);
            parent.Items.Clear();
            foreach (var entry in entries)
            {
                var item = new MenuItem
                {
                    Header = entry.Label,
                    IsEnabled = entry.Enabled,
                    Tag = entry.Start?.ToString("0.###", CultureInfo.InvariantCulture)
                };
                ShellMenuHooks.Style(item, muted: !entry.Enabled);
                if (entry.Enabled)
                {
                    item.Click += Chapter_Click;
                }

                parent.Items.Add(item);
            }
        }
    }

    private void Chapter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string text }
            || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var start))
        {
            return;
        }

        _session.SeekAbsolute(start);
        RefreshShell();
    }
}
