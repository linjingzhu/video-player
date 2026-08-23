using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VideoPlayer.App.Clip;
using VideoPlayer.App.Playback;
using VideoPlayer.Core.Capture;
using VideoPlayer.Core.Clip;
using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;
using VideoPlayer.Core.Subtitles;
using Path = System.IO.Path;

namespace VideoPlayer.App;

public partial class MainWindow : Window
{
    private readonly PlaybackSession _session;
    private readonly DispatcherTimer _timer;
    private readonly MpvMediaEngine _engine;
    private readonly FfmpegClipRunner _clipRunner = new();
    private bool _fullscreen;
    private bool _syncingVolumeUi;
    private WindowState _windowedState = WindowState.Normal;
    private WindowStyle _windowedStyle = WindowStyle.None;

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
        _engine.Host.MouseDoubleClick += (_, _) => Dispatcher.BeginInvoke(new Action(ToggleFullscreen));
        _engine.Host.MouseUp += OnVideoHostMouseUp;
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
            ? $"{UiCopy.AppTitle} — {(_session.IsUrlSource ? OpenUrlRules.DisplayName(cur.Path) : Path.GetFileName(cur.Path))}"
            : UiCopy.AppTitle;
        StatusText.Text = shell.Status.Text;
        StatusBar.Visibility = shell.Status.Visible ? Visibility.Visible : Visibility.Collapsed;
        OverlayTime.Text = shell.OverlayTime;
        OverlaySubtitle.Text = shell.OverlaySubtitle;
        OverlaySecondarySubtitle.Text = shell.OverlaySecondarySubtitle;
        EmptyStageCover.Visibility = shell.StageEmpty ? Visibility.Visible : Visibility.Collapsed;
        PlayIcon.Visibility = shell.IsPaused ? Visibility.Visible : Visibility.Collapsed;
        PauseIcon.Visibility = shell.IsPaused ? Visibility.Collapsed : Visibility.Visible;
        ApplyChromeMenus(shell);
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
        if (shell.StageEmpty || shell.Transport.Duration <= 0)
        {
            SeekSlider.Maximum = 1;
            SeekSlider.Value = 0;
        }
        else
        {
            SeekSlider.Maximum = shell.Transport.Duration;
            SeekSlider.Value = shell.Transport.Position;
        }

        BindSidebar();
        SeriesPage.IsEnabled = shell.Series.Enabled;
        SeriesPage.Bind(
            _session.Series,
            _session.Resume,
            _session.Current is { Kind: MediaSourceKind.LocalFile } file
                ? file.Path
                : null);
        ApplySidebar();
        ApplyChromeVisibility();
        ApplyCaptureChrome();
        RefreshClipChrome();
        SyncVolumeUi();
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
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xE6, 0x0E, 0x0E, 0x0E));
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
        button.Foreground = selected
            ? (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("CaptureOnAccentBrush")
            : (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("CaptureTextBrush");
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

    private void ApplyChromeMenus(PlayerShell shell)
    {
        foreach (var menu in new[] { QuickMenuButton.ContextMenu, HamburgerButton.ContextMenu })
        {
            SetMenuHeader(menu, "speed", shell.Transport.SpeedText);
            SetMenuEnabled(menu, "prev", shell.Transport.HasPrevious);
            SetMenuEnabled(menu, "next", shell.Transport.HasNext);
            SetMenuEnabled(menu, "series", shell.Series.Enabled);
            SetMenuEnabled(menu, "autoNext", !_session.IsUrlSource);
            SetMenuChecked(menu, "autoNext", _session.AutoNext);
            SetMenuChecked(menu, "skipAuto", _session.SkipAutoEnabled);
            SetMenuChecked(menu, "hdrAuto", _session.HdrMode == HdrMode.Auto);
            SetMenuChecked(menu, "hdrOff", _session.HdrMode == HdrMode.Off);
            SetMenuEnabled(menu, "capture", !_session.IsUrlSource);
            SetMenuEnabled(menu, "clip", !_session.IsUrlSource);
            SetMenuEnabled(menu, "saveAs", _session.CanSaveAs);
        }
    }

    private void ApplyChromeVisibility()
    {
        var showTransport = !_fullscreen || _session.Shell.ChromeVisible;
        TransportBar.Visibility = showTransport ? Visibility.Visible : Visibility.Collapsed;
        CaptionBar.Visibility = _fullscreen ? Visibility.Collapsed : Visibility.Visible;

        var statusLift = StatusBar.Visibility == Visibility.Visible ? StatusBar.Height : 0;
        if (_fullscreen)
        {
            TransportDockSlot.Height = 0;
            TransportDockSlot.Visibility = Visibility.Collapsed;
            TransportBar.Background = (Brush)FindResource("SeriesOnFullscreenTransportBrush");
            TransportBar.CornerRadius = new CornerRadius(SeriesOn.FullscreenTransportRadiusPx);
            TransportBar.BorderThickness = new Thickness(0);
            var inset = SeriesOn.FullscreenTransportInsetPx;
            TransportBar.Margin = new Thickness(inset, 0, inset, inset + statusLift);
        }
        else
        {
            TransportDockSlot.Height = 40;
            TransportDockSlot.Visibility = Visibility.Visible;
            TransportBar.Background = (Brush)FindResource("SeriesOnChromeBrush");
            TransportBar.CornerRadius = new CornerRadius(0);
            TransportBar.BorderThickness = new Thickness(0, 1, 0, 0);
            TransportBar.Margin = new Thickness(0, 0, 0, statusLift);
        }
    }

    private static MenuItem? FindMenuByTag(ContextMenu? menu, string tag)
        => menu?.Items.OfType<MenuItem>().FirstOrDefault(item => tag.Equals(item.Tag as string, StringComparison.Ordinal));

    private static void SetMenuEnabled(ContextMenu? menu, string tag, bool enabled)
    {
        var item = FindMenuByTag(menu, tag);
        if (item is not null)
        {
            item.IsEnabled = enabled;
        }
    }

    private static void SetMenuChecked(ContextMenu? menu, string tag, bool isChecked)
    {
        var item = FindMenuByTag(menu, tag);
        if (item is not null)
        {
            item.IsChecked = isChecked;
        }
    }

    private static void SetMenuHeader(ContextMenu? menu, string tag, string header)
    {
        var item = FindMenuByTag(menu, tag);
        if (item is not null)
        {
            item.Header = header;
        }
    }

    private void OpenContextMenu(Button button)
    {
        if (button.ContextMenu is not { } menu)
        {
            return;
        }

        menu.PlacementTarget = button;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void OpenStageMenuAtCursor()
    {
        if (QuickMenuButton.ContextMenu is not { } menu)
        {
            return;
        }

        ApplyChromeMenus(_session.Shell);
        menu.PlacementTarget = VideoHostBorder;
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private void OnVideoHostMouseUp(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button != System.Windows.Forms.MouseButtons.Right)
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (FullscreenChromeController.ShouldOpenStageMenuFromRightClick(
                    onVideoStage: true,
                    onTransportOrMenu: false))
            {
                OpenStageMenuAtCursor();
            }
        }));
    }

    private void Video_RightClick(object sender, MouseButtonEventArgs e)
    {
        var onTransportOrMenu = OriginatesOnTransportOrMenu(e.OriginalSource as DependencyObject);
        if (!FullscreenChromeController.ShouldOpenStageMenuFromRightClick(
                onVideoStage: !onTransportOrMenu,
                onTransportOrMenu: onTransportOrMenu))
        {
            return;
        }

        OpenStageMenuAtCursor();
        e.Handled = true;
    }

    private void QuickMenuButton_Click(object sender, RoutedEventArgs e) => OpenContextMenu(QuickMenuButton);

    private void Hamburger_Click(object sender, RoutedEventArgs e) => OpenContextMenu(HamburgerButton);

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();

    private void OpenUrl_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenUrlDialog { Owner = this };
        if (dialog.ShowDialog() == true && dialog.State.CanOpen)
        {
            _session.OpenUrl(dialog.Url);
            ShowMainPage();
            RefreshShell();
        }
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

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (!_session.CanSaveAs || _session.Current is not { } current)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = UiCopy.SaveAs,
            FileName = UrlSaveAs.SuggestedFileName(current.Path),
            Filter = "Videos|*.mp4;*.mkv;*.avi;*.wmv;*.mov|All files|*.*"
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        _session.SaveAs(dialog.FileName);
        RefreshShell();
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

    private void Stop_Click(object sender, RoutedEventArgs e) => _session.Stop();

    private void Clear_Click(object sender, RoutedEventArgs e) => _session.Clear();

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

    private void HdrAuto_Click(object sender, RoutedEventArgs e)
    {
        _session.SetHdrMode(HdrMode.Auto);
        RefreshShell();
    }

    private void HdrOff_Click(object sender, RoutedEventArgs e)
    {
        _session.SetHdrMode(HdrMode.Off);
        RefreshShell();
    }

    private void OverlayChrome_MouseDown(object sender, MouseButtonEventArgs e)
        => e.Handled = true;

    private void IgnoreChromeDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            e.Handled = true;
        }
    }

    private bool OriginatesOnTransportOrMenu(DependencyObject? source)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, TransportBar)
                || ReferenceEquals(source, QuickMenuButton)
                || source is ContextMenu or MenuItem or System.Windows.Controls.Menu)
            {
                return true;
            }

            source = source is Visual visual
                ? VisualTreeHelper.GetParent(visual)
                : LogicalTreeHelper.GetParent(source);
        }

        return false;
    }

    private void ClipSaveMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_session.IsUrlSource)
        {
            return;
        }

        ShowMainPage();
        _session.OpenClipSheet();
        RefreshShell();
    }

    private void ClipCancel_Click(object sender, RoutedEventArgs e)
    {
        _session.CloseClipSheet();
        RefreshShell();
    }

    private void ClipMarkStart_Click(object sender, RoutedEventArgs e)
    {
        _session.SetInMark();
        RefreshShell();
    }

    private void ClipMarkEnd_Click(object sender, RoutedEventArgs e)
    {
        _session.SetOutMark();
        RefreshShell();
    }

    private void ClipSave_Click(object sender, RoutedEventArgs e)
    {
        _session.RunClipSave(_clipRunner);
        RefreshShell();
    }

    private void ClipFormat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag })
        {
            return;
        }

        _session.SetClipFormat(ClipFormats.Parse(tag == "copy" ? "copy" : tag));
        RefreshShell();
    }

    private void ClipFpsMinus_Click(object sender, RoutedEventArgs e)
    {
        _session.NudgeClipFps(-1);
        RefreshShell();
    }

    private void ClipFpsPlus_Click(object sender, RoutedEventArgs e)
    {
        _session.NudgeClipFps(+1);
        RefreshShell();
    }

    private void ClipPingPong_Click(object sender, RoutedEventArgs e)
    {
        _session.SetClipPingPong(!_session.Shell.Clip.PingPong);
        RefreshShell();
    }

    private void ClipChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            SelectedPath = _session.Shell.Clip.FolderPath
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _session.SetClipFolder(dialog.SelectedPath);
            RefreshShell();
        }
    }

    private void ClipOverlay_Click(object sender, MouseButtonEventArgs e)
    {
        _session.CloseClipSheet();
        RefreshShell();
        e.Handled = true;
    }

    private void ClipSheet_EatClick(object sender, MouseButtonEventArgs e)
        => e.Handled = true;

    private void SeekHost_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        PlaceClipTicks();
        PlaceClipHandles();
    }

    private void ShowSeries_Click(object sender, RoutedEventArgs e)
    {
        if (!_session.CanUseSeriesTree)
        {
            return;
        }

        ShowSeriesPage();
    }

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
        if (_session.IsUrlSource)
        {
            return;
        }

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
        var onTransportOrMenu = OriginatesOnTransportOrMenu(e.OriginalSource as DependencyObject);
        if (e.ClickCount >= 2)
        {
            if (FullscreenChromeController.ShouldToggleFromDoubleClick(
                    onVideoStage: !onTransportOrMenu,
                    onTransportOrMenu: onTransportOrMenu))
            {
                ToggleFullscreen();
            }

            e.Handled = true;
            return;
        }

        if (!onTransportOrMenu)
        {
            _session.PlayPause();
        }
    }

    private void SeekSlider_Committed(object sender, MouseButtonEventArgs e)
        => _session.SeekAbsolute(SeekSlider.Value);

    private void SyncVolumeUi()
    {
        _syncingVolumeUi = true;
        try
        {
            var volume = _session.Shell.Volume;
            VolumeSlider.Value = volume.Level;
            VolumePercent.Text = volume.PercentText;
        }
        finally
        {
            _syncingVolumeUi = false;
        }
    }

    private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncingVolumeUi || _session is null)
        {
            return;
        }

        _session.SetVolume(e.NewValue);
        VolumePercent.Text = _session.Shell.Volume.PercentText;
    }

    private void VolumeButton_Click(object sender, RoutedEventArgs e)
    {
        _session.ToggleMute();
        SyncVolumeUi();
    }

    private void Volume_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _session.NudgeVolumeFromWheel(e.Delta > 0 ? 0.05 : -0.05);
        SyncVolumeUi();
        e.Handled = true;
    }

    private void VolumeButton_RightClick(object sender, MouseButtonEventArgs e)
    {
        _session.SpeakerRightClick();
        e.Handled = true;
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
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
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
        if (sender is not Button button || button.Tag is not ValueTuple<bool, string?> tagged)
        {
            return;
        }

        var (secondary, path) = tagged;
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
            case Key.I:
                _session.SetInMark();
                RefreshShell();
                e.Handled = true;
                break;
            case Key.O:
                _session.SetOutMark();
                RefreshShell();
                e.Handled = true;
                break;
            case Key.Escape when _session.Shell.Clip.Open:
                _session.CloseClipSheet();
                RefreshShell();
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
            case Key.Enter when Keyboard.Modifiers == ModifierKeys.None:
            case Key.F11:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Key.M when Keyboard.Modifiers == ModifierKeys.None:
                _session.ToggleMute();
                RefreshShell();
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
        _session.RememberVolume();
        _session.RememberWindow(new WindowBounds(Left, Top, Width, Height));
        _engine.Dispose();
    }

    private void RefreshClipChrome()
    {
        var clip = _session.Shell.Clip;
        ClipOverlay.Visibility = clip.Open ? Visibility.Visible : Visibility.Collapsed;
        ClipStartText.Text = clip.StartText;
        ClipEndText.Text = clip.EndText;
        ClipDurationText.Text = clip.DurationText;
        ClipFpsText.Text = clip.FpsText;
        ClipFolderText.Text = clip.FolderLabel;
        ClipPreviewName.Text = clip.PreviewFileName;
        ClipSaveButton.IsEnabled = clip.CanSave;
        ClipMarkStartButton.IsEnabled = clip.CanMarkCurrent;
        ClipMarkEndButton.IsEnabled = clip.CanMarkCurrent;
        ClipFpsMinus.IsEnabled = clip.FpsEnabled;
        ClipFpsPlus.IsEnabled = clip.FpsEnabled;
        ClipFpsRow.Opacity = clip.FpsEnabled ? 1 : 0.45;
        ClipPingPongToggle.IsEnabled = clip.PingPongEnabled;
        ClipPingPongRow.Opacity = clip.PingPongEnabled ? 1 : 0.45;
        ClipPingPongToggle.Background = clip.PingPong && clip.PingPongEnabled
            ? (Brush)FindResource("ClipAccentBrush")
            : new SolidColorBrush(Color.FromRgb(0x0E, 0x0E, 0x0E));
        ClipPingPongToggle.Content = "";
        ClipPaletteRow.Visibility = clip.PaletteNoticeVisible ? Visibility.Visible : Visibility.Collapsed;
        ClipEncodingHint.Visibility = clip.EncodingLockHintVisible ? Visibility.Visible : Visibility.Collapsed;
        ClipKeyframeNotice.Visibility = clip.KeyframeNoticeVisible ? Visibility.Visible : Visibility.Collapsed;
        ApplyFormatButton(ClipFormatCopy, clip.Format == ClipFormat.StreamCopy);
        ApplyFormatButton(ClipFormatWebp, clip.Format == ClipFormat.Webp);
        ApplyFormatButton(ClipFormatGif, clip.Format == ClipFormat.Gif);
        var banner = _session.Shell.ClipBanner;
        ClipBanner.Visibility = banner.Visible ? Visibility.Visible : Visibility.Collapsed;
        ClipBannerText.Text = banner.Text;
        PlaceClipTicks();
        PlaceClipHandles();
    }

    private static void ApplyFormatButton(Button button, bool selected)
    {
        button.BorderBrush = selected
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF))
            : new SolidColorBrush(Color.FromArgb(0x66, 0x22, 0x22, 0x22));
        button.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
    }

    private void PlaceClipTicks()
    {
        var clip = _session.Shell.Clip;
        var duration = _session.Shell.Transport.Duration;
        PlaceTick(InTick, clip.ShowInTick, ClipSave.TickRatio(clip.InMark, duration));
        PlaceTick(OutTick, clip.ShowOutTick, ClipSave.TickRatio(clip.OutMark, duration));
    }

    private void PlaceClipHandles()
    {
        var clip = _session.Shell.Clip;
        var duration = _session.Shell.Transport.Duration;
        PlaceHandle(StartHandle, clip.ShowStartHandle, ClipSave.TickRatio(clip.InMark, duration));
        PlaceHandle(EndHandle, clip.ShowEndHandle, ClipSave.TickRatio(clip.OutMark, duration));
    }

    private void ClipHandle_DragDelta(object sender, DragDeltaEventArgs e)
    {
        _ = e;
        if (sender is not Thumb thumb)
        {
            return;
        }

        var handle = ReferenceEquals(thumb, StartHandle) ? ClipHandle.Start : ClipHandle.End;
        var duration = _session.Shell.Transport.Duration;
        var seconds = ClipSave.TimeFromSeekX(
            Mouse.GetPosition(SeekHost).X,
            SeekHost.ActualWidth,
            thumb.Width,
            duration);
        _session.MoveClipHandle(handle, seconds);
        RefreshClipChrome();
    }

    private void PlaceTick(FrameworkElement tick, bool show, double ratio)
    {
        tick.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show || SeekHost.ActualWidth <= 0)
        {
            return;
        }

        System.Windows.Controls.Canvas.SetLeft(tick, ClipSave.HandleLeft(ratio, SeekHost.ActualWidth, tick.Width));
        System.Windows.Controls.Canvas.SetTop(tick, Math.Max(0, (MarkCanvas.ActualHeight - tick.Height) / 2));
    }

    private void PlaceHandle(Thumb handle, bool show, double ratio)
    {
        handle.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show || SeekHost.ActualWidth <= 0)
        {
            return;
        }

        System.Windows.Controls.Canvas.SetLeft(handle, ClipSave.HandleLeft(ratio, SeekHost.ActualWidth, handle.Width));
        System.Windows.Controls.Canvas.SetTop(handle, Math.Max(0, (HandleCanvas.ActualHeight - handle.Height) / 2));
    }
}
