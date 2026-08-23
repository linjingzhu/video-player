using System.Windows;
using System.Windows.Controls;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.App;

public partial class OpenUrlDialog : Window
{
    private readonly OpenUrlDialogState _state = new();

    public OpenUrlDialog()
    {
        InitializeComponent();
        Title = UiCopy.OpenUrl;
        Loaded += (_, _) =>
        {
            SyncOpenButton();
            UrlBox.Focus();
        };
        SyncOpenButton();
    }

    public OpenUrlDialogState State => _state;

    public string Url => UrlBox.Text.Trim();

    private void UrlBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Placeholder.Visibility = string.IsNullOrEmpty(UrlBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
        _state.SetText(UrlBox.Text);
        SyncOpenButton();
    }

    private void SyncOpenButton()
        => OpenButton.IsEnabled = _state.CanOpen;

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!_state.CanOpen)
        {
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
