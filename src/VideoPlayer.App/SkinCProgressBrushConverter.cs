using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VideoPlayer.App;

/// <summary>Skin C: in-progress play+percent is accent blue; check and dash stay primary.</summary>
public sealed class SkinCProgressBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Accent = Freeze("#0A84FF");
    private static readonly SolidColorBrush Primary = Freeze("#F5F5F7");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string ?? string.Empty;
        return text.StartsWith('▶') ? Accent : Primary;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;

    private static SolidColorBrush Freeze(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }
}
