using System.Windows.Controls;
using System.Windows.Media;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.App;

internal static class ShellMenuHooks
{
    private static readonly List<Action<MainWindow>> Hooks = [];

    internal static SolidColorBrush MenuBackground { get; } = Freeze(0xFF, 0x0E, 0x0E, 0x0E);
    internal static SolidColorBrush MenuForeground { get; } = Freeze(0xFF, 0xFF, 0xFF, 0xFF);
    internal static SolidColorBrush MenuMuted { get; } = Freeze(0xFF, 0x8A, 0x8A, 0x8A);

    internal static void Register(Action<MainWindow> hook) => Hooks.Add(hook);

    internal static void Apply(MainWindow window)
    {
        foreach (var hook in Hooks)
        {
            hook(window);
        }

        if (!window.SharedMenusAreOpen)
        {
            window.ArrangeSharedMenus();
        }
    }

    internal static void Style(MenuItem item, bool muted)
    {
        item.Background = MenuBackground;
        item.Foreground = muted ? MenuMuted : MenuForeground;
    }

    private static SolidColorBrush Freeze(byte a, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }
}

public partial class MainWindow
{
    internal bool SharedMenusAreOpen
        => QuickMenuButton.ContextMenu?.IsOpen == true
           || HamburgerButton.ContextMenu?.IsOpen == true;

    internal void ArrangeSharedMenus()
    {
        ArrangeMenu(QuickMenuButton.ContextMenu);
        ArrangeMenu(HamburgerButton.ContextMenu);
    }

    private static void ArrangeMenu(ContextMenu? menu)
    {
        if (menu is null || menu.Items.Count < 2)
        {
            return;
        }

        var snapshot = menu.Items.Cast<object>().ToList();
        var keys = snapshot.Select((item, index) => MenuKey(item, index)).ToList();
        var arranged = MenuOrder.Arrange(keys);
        if (keys.SequenceEqual(arranged))
        {
            return;
        }

        var ranked = snapshot
            .Select((item, index) => (item, index, rank: MenuOrder.Rank(keys[index], index)))
            .OrderBy(entry => entry.rank)
            .ThenBy(entry => entry.index)
            .ToList();
        menu.Items.Clear();
        foreach (var entry in ranked)
        {
            menu.Items.Add(entry.item);
        }
    }

    private static string MenuKey(object item, int index)
        => item switch
        {
            MenuItem { Tag: string tag } when tag.Length > 0 => tag,
            MenuItem { Header: string header } when header.Length > 0 => header,
            Separator { Tag: string tag } when tag.Length > 0 => tag,
            _ => $"#{index}"
        };
}
