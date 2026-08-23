using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using VideoPlayer.App.Playback;
using VideoPlayer.Core.Capture;
using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;
using VideoPlayer.Core.Subtitles;

namespace VideoPlayer.App;

public partial class MainWindow : Window
{
    private readonly PlaybackSession _session;
    private readonly DispatcherTimer _timer;
    private readonly MpvMediaEngine _engine;
    private bool _fullscreen;
    private WindowState _windowedState = WindowState.Normal;
    private WindowStyle _windowedStyle = WindowStyle.SingleBorderWindow;

    public MainWindow()
    {
        InitializeComponent();
        var data = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VideoPlayer");
        _engine = new MpvMediaEngine();
        _session = new PlaybackSession(_engine, data);
        ApplyWindowMemory(_session.Window.Bounds);
        PlayerHost.Child = _engine.Host;
        ApplySidebar();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
        RefreshShell();
    }

    public void OpenFromCommandLine(IEnumerable<string> paths)
    {
        Activate();
        _session.Drop(paths);
        RefreshShell();
    }

    private void OnTick()
    {
        _session.Tick(DateTimeOffset.UtcNow);
        RefreshShell();
    }

    private void RefreshShell()
    {
        var shell = _session.Shell;
        Title = _session.Current is { } cur
            ? $"{UiCopy.AppTitle} — {Path.GetFileName(cur.Path)}"
            : UiCopy.AppTitle;
        StatusText.Text = shell.Status.Text;
        StatusBar.Visibility = shell.Status.Visible ? Visibility.Visible : Visibility.Collapsed;
        OverlayTime.Text = shell.OverlayTime;
        OverlayTime.Visibility = !_fullscreen || shell.ChromeVisible ? Visibility.Visible : Visibility.Collapsed;
        OverlaySubtitle.Text = shell.OverlaySubtitle;
        OverlaySecondarySubtitle.Text = shell.OverlaySecondarySubtitle;
        PlayIcon.Visibility = shell.IsPaused ? Visibility.Visible : Visibility.Collapsed;
        PauseIcon.Visibility = shell.IsPaused ? Visibility.Collapsed : Visibility.Visible;
        SpeedButton.Content = shell.Transport.SpeedText;
        PrevButton.IsEnabled = shell.Transport.HasPrevious;
        NextButton.IsEnabled = shell.Transport.HasNext;
        CaptionsButton.Style = shell.Transport.CaptionsOn
            ? (Style)FindResource("CcOnButton")
            : (Style)FindResource("SkinATextButton");
        SkipCapsule.Visibility = shell.Skip.Visible ? Visibility.Visible : Visibility.Collapsed;
        SkipCapsuleButton.Content = shell.Skip.Label;
        SkipCancelButton.Content = shell.Skip.CancelLabel;
        SkipCancelButton.Visibility = shell.Skip.TwoLine ? Visibility.Visible : Visibility.Collapsed;
        SubtitleSheet.Visibility = shell.Subtitles.Open ? Visibility.Visible : Visibility.Collapsed;
        if (shell.Subtitles.Open)
        {
            BindSubtitleRows();
        }
        NextCtaButton.Content = shell.NextEpisode.Label;
        NextCtaPanel.Visibility = shell.NextEpisode.ShowCta ? Visibility.Visible : Visibility.Collapsed;
        CancelAutoNextButton.Visibility = shell.NextEpisode.AutoNextPending ? Visibility.Visible : Visibility.Collapsed;
        if (shell.Transport.Duration > 0)
        {
            SeekSlider.Maximum = shell.Transport.Duration;
            SeekSlider.Value = shell.Transport.Position;
        }

        BindSidebar();
        SeriesPage.Bind(_session.Series, _session.Resume, _session.Current?.Path);
        ApplySidebar();
        ApplyChromeVisibility();
        ApplyCaptureChrome();
    }

    private void ApplyCaptureChrome()
    {
        var sheet = _session.Shell.Capture;
        CaptureSheetTitle.Text = sheet.Title;
        CaptureSheet.Visibility = sheet.Open ? Visibility.Visible : Visibility.Collapsed;
        CaptureCountText.Text = sheet.Count.ToString();
        CaptureIntervalText.Text = sheet.IntervalText;
        CaptureFolderText.Text = sheet.FolderLabel;
        ApplyFormatSelection(sheet.Format);

        var banner = _session.Shell.CaptureBanner;
        CaptureBanner.Visibility = banner.Visible ? Visibility.Visible : Visibility.Collapsed;
        CaptureBannerText.Text = banner.Text;
        CaptureBanner.Background = banner.Kind == CaptureBannerKind.Failure
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xE6, 0x3A, 0x14, 0x18))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xE6, 0x14, 0x14, 0x18));
    }

    private void ApplyFormatSelection(CaptureFormat format)
    {
        StyleFormat(FormatPng, format == CaptureFormat.Png);
        StyleFormat(FormatJpg, format == CaptureFormat.Jpg);
        StyleFormat(FormatWebp, format == CaptureFormat.Webp);
    }

    private static void StyleFormat(System.Windows.Controls.Button button, bool selected)
    {
        button.Background = selected
            ? (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("CaptureAccentBrush")
            : System.Windows.Media.Brushes.Transparent;
        button.BorderThickness = selected ? new Thickness(0) : new Thickness(1);
    }

    private void BindSidebar()
    {
        SidebarList.Items.Clear();
        if (_session.Shell.Sidebar.Resume is { } resume)
        {
            SidebarList.Items.Add(new ListBoxItem { Content = $"{UiCopy.ContinueWatching} · {resume.Label}", Tag = resume });
        }
        else
        {
            SidebarList.Items.Add(new ListBoxItem { Content = UiCopy.ContinueWatching, Tag = "resume-empty" });
        }

        foreach (var series in _session.Shell.Sidebar.RecentSeries)
        {
            SidebarList.Items.Add(new ListBoxItem { Content = series.Title, Tag = series });
        }
    }

    private void ApplyWindowMemory(WindowBounds bounds)
    {
        Left = bounds.X;
        Top = bounds.Y;
        Width = bounds.Width;
        Height = bounds.Height;
        WindowState = WindowState.Normal;
    }

    private void ApplySidebar()
    {
        if (_fullscreen)
        {
            SidebarRailColumn.Width = new GridLength(0);
            SidebarContentColumn.Width = new GridLength(0);
            SidebarRail.Visibility = Visibility.Collapsed;
            SidebarPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var open = _session.Shell.Sidebar.Open;
        SidebarRailColumn.Width = new GridLength(ShellLayout.SidebarRailWidthPx);
        SidebarContentColumn.Width = open
            ? new GridLength(ShellLayout.SidebarOpenPanelWidthPx)
            : new GridLength(0);
        SidebarRail.Visibility = Visibility.Visible;
        SidebarPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyChromeVisibility()
    {
        var showTransport = !_fullscreen || _session.Shell.ChromeVisible;
        TransportBar.Visibility = showTransport ? Visibility.Visible : Visibility.Collapsed;
        MainMenu.Visibility = _fullscreen ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Videos|*.mp4;*.mkv;*.avi;*.wmv;*.mov|All files|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) == true)
        {
            _session.Drop(dialog.FileNames);
            ShowMainPage();
            RefreshShell();
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _session.OpenSeriesFolder(dialog.SelectedPath);
            ShowSeriesPage();
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void PlayPause_Click(object sender, RoutedEventArgs e) => _session.PlayPause();

    private void Prev_Click(object sender, RoutedEventArgs e) => _session.PlayPreviousEpisode();

    private void Next_Click(object sender, RoutedEventArgs e) => _session.PlayNextEpisode();

    private void CancelAutoNext_Click(object sender, RoutedEventArgs e) => _session.CancelAutoNext();

    private void SkipBack_Click(object sender, RoutedEventArgs e) => _session.SkipBack();

    private void SkipForward_Click(object sender, RoutedEventArgs e) => _session.SkipForward();

    private void CycleSpeed_Click(object sender, RoutedEventArgs e)
    {
        var presets = PlaybackSpeed.Presets;
        var index = 0;
        for (var i = 0; i < presets.Count; i++)
        {
            if (Math.Abs(presets[i] - _session.Speed) < 0.01)
            {
                index = (i + 1) % presets.Count;
                break;
            }
        }

        _session.SetSpeed(presets[index]);
        RefreshShell();
    }

    private void Captions_Click(object sender, RoutedEventArgs e)
    {
        _session.ToggleCaptions();
        RefreshShell();
    }

    private void SubtitlesMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_session.Shell.Subtitles.Open)
        {
            _session.CloseSubtitleSheet();
        }
        else
        {
            _session.OpenSubtitleSheet();
        }

        RefreshShell();
    }

    private void SkipCapsule_Click(object sender, RoutedEventArgs e)
    {
        _session.SkipActiveSegment();
        RefreshShell();
    }

    private void SkipCancel_Click(object sender, RoutedEventArgs e)
    {
        _session.CancelSkipAuto();
        RefreshShell();
    }

    private void SkipToHere_Click(object sender, RoutedEventArgs e)
    {
        _session.MarkSkipToHere();
        RefreshShell();
    }

    private void SkipAuto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            _session.SkipAutoEnabled = item.IsChecked;
        }
    }

    private void OverlayChrome_MouseDown(object sender, MouseButtonEventArgs e)
        => e.Handled = true;

    private void ShowSeries_Click(object sender, RoutedEventArgs e) => ShowSeriesPage();

    private void AutoNext_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            _session.AutoNext = item.IsChecked;
        }
    }

    private void CaptureMenu_Click(object sender, RoutedEventArgs e) => OpenCaptureSheet();

    private void OpenCaptureSheet()
    {
        ShowMainPage();
        _session.OpenCaptureSheet();
        RefreshShell();
    }

    private void CaptureCancel_Click(object sender, RoutedEventArgs e)
    {
        _session.CloseCaptureSheet();
        RefreshShell();
    }

    private void CaptureStart_Click(object sender, RoutedEventArgs e)
    {
        if (_session.Shell.Capture.NeedsConfirm)
        {
            var answer = MessageBox.Show(
                this,
                UiCopy.CaptureConfirm,
                UiCopy.Capture,
                MessageBoxButton.OKCancel,
                MessageBoxImage.None);
            if (answer != MessageBoxResult.OK)
            {
                return;
            }
        }

        _session.RunStillCapture();
        RefreshShell();
    }

    private void CaptureCountMinus_Click(object sender, RoutedEventArgs e)
    {
        _session.NudgeCaptureCount(-1);
        RefreshShell();
    }

    private void CaptureCountPlus_Click(object sender, RoutedEventArgs e)
    {
        _session.NudgeCaptureCount(1);
        RefreshShell();
    }

    private void CaptureIntervalMinus_Click(object sender, RoutedEventArgs e)
    {
        _session.NudgeCaptureInterval(-1);
        RefreshShell();
    }

    private void CaptureIntervalPlus_Click(object sender, RoutedEventArgs e)
    {
        _session.NudgeCaptureInterval(1);
        RefreshShell();
    }

    private void CaptureFormat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string tag)
        {
            _session.SetCaptureFormat(CaptureFormats.Parse(tag));
            RefreshShell();
        }
    }

    private void CaptureChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = UiCopy.CaptureFolder,
            SelectedPath = Directory.Exists(_session.Shell.Capture.FolderPath)
                ? _session.Shell.Capture.FolderPath
                : StillFrameCapture.DefaultFolderPath()
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _session.SetCaptureFolder(dialog.SelectedPath);
            RefreshShell();
        }
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        _session.ToggleSidebar();
        ApplySidebar();
    }

    private void Sidebar_Activate(object sender, MouseButtonEventArgs e)
    {
        if (SidebarList.SelectedItem is not ListBoxItem item)
        {
            return;
        }

        if (item.Tag is SidebarResumeItem)
        {
            _session.ContinueWatching();
            ShowMainPage();
        }
        else if (item.Tag is SidebarSeriesItem series)
        {
            _session.OpenSeriesFolder(series.FolderPath);
            ShowSeriesPage();
        }
    }

    private void Video_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            ToggleFullscreen();
        }
        else
        {
            _session.PlayPause();
        }
    }

    private void SeekSlider_Committed(object sender, MouseButtonEventArgs e)
        => _session.SeekAbsolute(SeekSlider.Value);

    private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_session is null)
        {
            return;
        }

        _session.AdjustVolume(e.NewValue - _session.Engine.Volume);
    }

    private void SeriesPage_EpisodeActivated(object sender, SeriesListItem item)
    {
        _session.DrillInto(item);
        if (_session.Shell.Screen == ShellScreen.Main)
        {
            ShowMainPage();
        }
        else
        {
            RefreshShell();
        }
    }

    private void SeriesBack_Click(object sender, RoutedEventArgs e) => ShowMainPage();

    private void ShowSeriesPage()
    {
        SeriesPage.Visibility = Visibility.Visible;
        VideoPage.Visibility = Visibility.Collapsed;
        _session.Shell.ShowSeries();
        RefreshShell();
    }

    private void ShowMainPage()
    {
        SeriesPage.Visibility = Visibility.Collapsed;
        VideoPage.Visibility = Visibility.Visible;
        _session.Shell.Screen = ShellScreen.Main;
        RefreshShell();
    }

    private void BindSubtitleRows()
    {
        FillSubtitleRows(SecondarySubtitleRows, _session.Shell.Subtitles.SecondaryRows, secondary: true);
        FillSubtitleRows(PrimarySubtitleRows, _session.Shell.Subtitles.PrimaryRows, secondary: false);
    }

    private void FillSubtitleRows(StackPanel host, IReadOnlyList<SubtitleTrackRow> rows, bool secondary)
    {
        host.Children.Clear();
        foreach (var row in rows)
        {
            var label = new TextBlock { Text = row.Label, VerticalAlignment = VerticalAlignment.Center };
            var mark = new TextBlock
            {
                Text = row.Selected ? "✓" : "",
                Foreground = (System.Windows.Media.Brush)FindResource("PackAccentBrush"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(mark, 1);
            grid.Children.Add(label);
            grid.Children.Add(mark);

            var button = new Button
            {
                Content = grid,
                Tag = (secondary, row.Path),
                Style = (Style)FindResource("SubtitleRowButton")
            };
            if (row.Selected)
            {
                button.BorderBrush = (System.Windows.Media.Brush)FindResource("PackAccentBrush");
                button.BorderThickness = new Thickness(1.5);
            }

            button.Click += SubtitleRow_Click;
            host.Children.Add(button);
        }
    }

    private void SubtitleRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: (bool secondary, string? path) })
        {
            return;
        }

        if (secondary)
        {
            _session.SelectSecondarySubtitle(path);
        }
        else
        {
            _session.SelectPrimarySubtitle(path);
        }

        RefreshShell();
    }

    private void ToggleFullscreen()
    {
        if (_fullscreen)
        {
            ExitFullscreen();
        }
        else
        {
            EnterFullscreen();
        }
    }

    private void EnterFullscreen()
    {
        _windowedState = WindowState;
        _windowedStyle = WindowStyle;
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Normal;
        WindowState = WindowState.Maximized;
        _fullscreen = true;
        _session.EnterFullscreen();
        _session.NoteActivity(DateTimeOffset.UtcNow);
        RefreshShell();
    }

    private void ExitFullscreen()
    {
        WindowStyle = _windowedStyle;
        WindowState = WindowState.Normal;
        if (_windowedState != WindowState.Minimized)
        {
            WindowState = _windowedState == WindowState.Maximized ? WindowState.Normal : _windowedState;
        }

        _fullscreen = false;
        _session.ExitFullscreen();
        RefreshShell();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        _session.NoteActivity(DateTimeOffset.UtcNow);
        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
            && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift
            && e.Key == Key.C)
        {
            OpenCaptureSheet();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Space:
                _session.PlayPause();
                e.Handled = true;
                break;
            case Key.Left:
                _session.SkipBack();
                e.Handled = true;
                break;
            case Key.Right:
                _session.SkipForward();
                e.Handled = true;
                break;
            case Key.Escape when _session.Shell.Subtitles.Open:
                _session.CloseSubtitleSheet();
                RefreshShell();
                e.Handled = true;
                break;
            case Key.Escape when _session.Shell.Capture.Open:
                _session.CloseCaptureSheet();
                RefreshShell();
                e.Handled = true;
                break;
            case Key.Escape when _fullscreen:
                ExitFullscreen();
                e.Handled = true;
                break;
            case Key.F11:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Key.C when Keyboard.Modifiers == ModifierKeys.None:
                _session.ToggleCaptions();
                e.Handled = true;
                break;
        }
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        => _session.NoteActivity(DateTimeOffset.UtcNow);

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _session.AdjustVolume(e.Delta > 0 ? 0.05 : -0.05);
        VolumeSlider.Value = _session.Engine.Volume;
        e.Handled = true;
    }

    protected override void OnDrop(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop)
            && e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            _session.Drop(files);
            RefreshShell();
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _session.Checkpoint("exit");
        _session.RememberWindow(new WindowBounds(Left, Top, Width, Height));
        _engine.Dispose();
    }
}
