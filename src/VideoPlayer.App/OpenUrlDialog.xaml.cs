using System.Windows;
using System.Windows.Controls;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.App;

public partial class OpenUrlDialog : Window
{
    public OpenUrlDialog()
    {
        InitializeComponent();
        Title = UiCopy.OpenUrl;
        Loaded += (_, _) => UrlBox.Focus();
    }

    public string Url => UrlBox.Text.Trim();

    private void UrlBox_TextChanged(object sender, TextChangedEventArgs e)
        => Placeholder.Visibility = string.IsNullOrEmpty(UrlBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

    private void Open_Click(object sender, RoutedEventArgs e)
        => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
