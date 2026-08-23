using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using VideoPlayer.App.Playback;
using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Series;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.App;

public partial class MainWindow : Window
{
    private readonly PlaybackSession _session;
    private readonly DispatcherTimer _timer;
    private readonly MpvMediaEngine _engine;
    private bool _fullscreen;
    private WindowState _windowedState = WindowState.Normal;
    private WindowStyle _windowedStyle = WindowStyle.SingleBorderWindow;
    private SeriesShow? _series;

    public MainWindow()
    {
        InitializeComponent();
        var data = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VideoPlayer");
        _engine = new MpvMediaEngine();
        _session = new PlaybackSession(_engine, data);
        ApplyWindowMemory(_session.Window.Bounds);
        Topmost = _session.Window.AlwaysOnTop;
        AlwaysOnTopItem.IsChecked = Topmost;
        PlayerHost.Child = _engine.Host;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
        RefreshShell();
        SetupTaskbar();
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
        if (_fullscreen)
        {
            ApplyFullscreenChrome(_session.Shell.ChromeVisible);
        }
    }

    private void RefreshShell()
    {
        var shell = _session.Shell;
        Title = CurrentTitle();
        StatusText.Text = shell.Status.Text;
        SeriesStatusText.Text = shell.Status.SeriesSummary;
        PositionText.Text = shell.Transport.PositionText;
        DurationText.Text = shell.Transport.DurationText;
        OverlayTime.Text = shell.OverlayTime;
        OverlaySubtitle.Text = string.IsNullOrEmpty(shell.OverlaySubtitle)
            ? (shell.Transport.CaptionsOn ? UiCopy.SubtitlePlaceholder : "")
            : shell.OverlaySubtitle;
        PlayButton.Content = shell.IsPaused ? "▶" : "❚❚";
        CenterPlay.Visibility = shell.IsPaused ? Visibility.Visible : Visibility.Collapsed;
        SpeedButton.Content = shell.Transport.SpeedText;
        FsSpeed.Content = shell.Transport.SpeedText;
        FsTitle.Text = shell.Fullscreen.Title;
        FsPosition.Text = shell.Transport.PositionText;
        FsDuration.Text = shell.Transport.DurationText;
        if (shell.Transport.Duration > 0)
        {
            SeekSlider.Maximum = shell.Transport.Duration;
            SeekSlider.Value = shell.Transport.Position;
            FsSeek.Maximum = shell.Transport.Duration;
            FsSeek.Value = shell.Transport.Position;
        }

        SidebarList.Items.Clear();
        foreach (var item in shell.Sidebar.Items)
        {
            SidebarList.Items.Add(new ListBoxItem { Content = item });
        }
    }

    private string CurrentTitle()
        => _session.Current is { } cur
            ? $"{UiCopy.AppTitle} — {Path.GetFileName(cur.Path)}"
            : UiCopy.AppTitle;

    private void ApplyWindowMemory(WindowBounds bounds)
    {
        Left = bounds.X;
        Top = bounds.Y;
        Width = bounds.Width;
        Height = bounds.Height;
        WindowState = WindowState.Normal;
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
            RefreshShell();
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _series = _session.OpenSeriesFolder(dialog.SelectedPath);
            BindSeries();
            ShowSeriesPage();
        }
    }

    private void LoadPlaylist_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Playlist|*.json" };
        if (dialog.ShowDialog(this) == true)
        {
            var loaded = PlaylistStore.FromJson(File.ReadAllText(dialog.FileName));
            foreach (var item in loaded.Items)
            {
                _session.Playlist.Add(item.Path, item.Size);
            }

            if (loaded.Items.Count > 0)
            {
                _session.Open(loaded.Items[0].Path);
            }

            RefreshShell();
        }
    }

    private void SavePlaylist_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Playlist|*.json", FileName = "playlist.json" };
        if (dialog.ShowDialog(this) == true)
        {
            File.WriteAllText(dialog.FileName, _session.Playlist.ToJson());
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void PlayPause_Click(object sender, RoutedEventArgs e) => _session.PlayPause();

    private void Prev_Click(object sender, RoutedEventArgs e) => _session.PlayPreviousEpisode();

    private void Next_Click(object sender, RoutedEventArgs e) => _session.PlayNextEpisode();

    private void SkipBack_Click(object sender, RoutedEventArgs e) => _session.SkipBack();

    private void SkipForward_Click(object sender, RoutedEventArgs e) => _session.SkipForward();

    private void FrameForward_Click(object sender, RoutedEventArgs e) => _session.FrameStep(1);

    private void FrameBack_Click(object sender, RoutedEventArgs e) => _session.FrameStep(-1);

    private void Speed_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string tag } && double.TryParse(tag, out var speed))
        {
            _session.SetSpeed(speed);
            RefreshShell();
        }
    }

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

    private void Captions_Click(object sender, RoutedEventArgs e) => _session.ToggleCaptions();

    private void ShowSeries_Click(object sender, RoutedEventArgs e) => ShowSeriesPage();

    private void ShowMain_Click(object sender, RoutedEventArgs e) => ShowMainPage();

    private void AddPlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (_session.Current is { } cur)
        {
            _session.Playlist.Add(cur.Path, cur.Size);
        }
    }

    private void AutoNext_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            _session.AutoNext = item.IsChecked;
        }
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
    {
        Topmost = AlwaysOnTopItem.IsChecked;
        _session.Window.AlwaysOnTop = Topmost;
    }

    private void Contain_Click(object sender, RoutedEventArgs e) => _session.SetFitMode("contain");

    private void Cover_Click(object sender, RoutedEventArgs e) => _session.SetFitMode("cover");

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        => SidebarColumn.Width = SidebarColumn.Width.Value > 0 ? new GridLength(0) : new GridLength(240);

    private void About_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show(
            this,
            "영상 플레이어\n로컬 파일 전용 · libmpv/FFmpeg\nMedia Foundation 단독 디코더는 사용하지 않습니다.",
            UiCopy.AppTitle);

    private void Sidebar_Activate(object sender, MouseButtonEventArgs e)
    {
        if (SidebarList.SelectedItem is ListBoxItem { Content: string text }
            && text == UiCopy.ContinueWatching)
        {
            _session.ContinueWatching();
            ShowMainPage();
        }
        else if (SidebarList.SelectedIndex > 0
                 && SidebarList.SelectedIndex - 1 < _session.Recent.Items.Count)
        {
            _session.Open(_session.Recent.Items[SidebarList.SelectedIndex - 1].Path);
            ShowMainPage();
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

    private void Sort_Changed(object sender, SelectionChangedEventArgs e) => BindSeries();

    private void SeasonTree_Selected(object sender, RoutedPropertyChangedEventArgs<object> e) => BindSeries();

    private void EpisodeGrid_Activate(object sender, MouseButtonEventArgs e)
    {
        if (EpisodeGrid.SelectedItem is SeriesRow row)
        {
            var episode = _series?.Seasons.SelectMany(s => s.Episodes)
                .FirstOrDefault(ep => ep.FileName == row.FileName);
            if (episode is not null)
            {
                _session.Open(episode.Path);
                ShowMainPage();
            }
        }
    }

    private void ShowSeriesPage()
    {
        SeriesPage.Visibility = Visibility.Visible;
        VideoPage.Visibility = Visibility.Collapsed;
        _session.Shell.ShowSeries();
        BindSeries();
    }

    private void ShowMainPage()
    {
        SeriesPage.Visibility = Visibility.Collapsed;
        VideoPage.Visibility = Visibility.Visible;
        _session.Shell.Screen = ShellScreen.Main;
    }

    private void BindSeries()
    {
        if (_series is null)
        {
            return;
        }

        if (SeasonTree.Items.Count == 0)
        {
            var root = new TreeViewItem { Header = _series.Name, IsExpanded = true };
            foreach (var season in _series.Seasons)
            {
                root.Items.Add(new TreeViewItem { Header = season.Name, Tag = season });
            }

            SeasonTree.Items.Add(root);
        }

        var selected = (SeasonTree.SelectedItem as TreeViewItem)?.Tag as SeriesSeason
                       ?? _series.Seasons.FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        var rows = selected.Episodes.Select(ep =>
        {
            var saved = _session.Resume.Find(ep.Path, ep.Size);
            var progress = saved switch
            {
                { Completed: true } => "✓",
                { DurationSeconds: > 0 } e => $"{Math.Clamp(e.PositionSeconds / e.DurationSeconds * 100, 0, 100):0}%",
                _ => "-"
            };
            return new SeriesRow(Core.Series.EpisodeParser.EpisodeLabel(ep.SortKey), ep.FileName, "--:--:--", progress, false);
        }).ToList();

        rows = SortCombo.SelectedIndex switch
        {
            1 => [.. rows.OrderBy(r => r.FileName)],
            2 => [.. rows.OrderBy(r => r.Duration)],
            3 => [.. rows.OrderBy(r => r.Progress)],
            _ => rows
        };
        EpisodeGrid.ItemsSource = rows;
        SeriesStatusText.Text = $"{selected.Name} {rows.Count}개 파일";
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
        MainMenu.Visibility = Visibility.Collapsed;
        StatusBar.Visibility = Visibility.Collapsed;
        TransportBar.Visibility = Visibility.Collapsed;
        SidebarColumn.Width = new GridLength(0);
        FullscreenChrome.Visibility = Visibility.Visible;
        FullscreenChrome.IsHitTestVisible = true;
        _fullscreen = true;
        _session.EnterFullscreen();
        _session.NoteActivity(DateTimeOffset.UtcNow);
    }

    private void ExitFullscreen()
    {
        WindowStyle = _windowedStyle;
        WindowState = WindowState.Normal;
        if (_windowedState != WindowState.Minimized)
        {
            WindowState = _windowedState == WindowState.Maximized ? WindowState.Normal : _windowedState;
        }

        MainMenu.Visibility = Visibility.Visible;
        StatusBar.Visibility = Visibility.Visible;
        TransportBar.Visibility = Visibility.Visible;
        SidebarColumn.Width = new GridLength(240);
        FullscreenChrome.Visibility = Visibility.Collapsed;
        _fullscreen = false;
        _session.ExitFullscreen();
    }

    private void ApplyFullscreenChrome(bool visible)
    {
        FullscreenChrome.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        FullscreenChrome.IsHitTestVisible = visible;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        _session.NoteActivity(DateTimeOffset.UtcNow);
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
            case Key.Up:
                _session.PlayPreviousEpisode();
                e.Handled = true;
                break;
            case Key.Down:
            case Key.N:
                _session.PlayNextEpisode();
                e.Handled = true;
                break;
            case Key.Escape when _fullscreen:
                ExitFullscreen();
                e.Handled = true;
                break;
            case Key.F:
            case Key.F11:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Key.OemPeriod:
                _session.FrameStep(1);
                e.Handled = true;
                break;
            case Key.OemComma:
                _session.FrameStep(-1);
                e.Handled = true;
                break;
            case Key.C:
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

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var source = PresentationSource.FromVisual(this) as HwndSource;
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int wmAppCommand = 0x0319;
        if (msg == wmAppCommand)
        {
            var cmd = ((int)wParam.ToInt64() >> 16) & 0xFFFF;
            switch (cmd)
            {
                case 14: // MEDIA_PLAY_PAUSE? actually APPCOMMAND_MEDIA_PLAY_PAUSE = 14
                    _session.PlayPause();
                    handled = true;
                    break;
                case 11:
                    _session.PlayNextEpisode();
                    handled = true;
                    break;
                case 12:
                    _session.PlayPreviousEpisode();
                    handled = true;
                    break;
            }
        }

        return IntPtr.Zero;
    }

    private void SetupTaskbar()
    {
        var chrome = new TaskbarItemInfo();
        chrome.ThumbButtonInfos.Add(Thumb("−10", () => _session.SkipBack()));
        chrome.ThumbButtonInfos.Add(Thumb("▶", () => _session.PlayPause()));
        chrome.ThumbButtonInfos.Add(Thumb("+10", () => _session.SkipForward()));
        TaskbarItemInfo = chrome;
    }

    private static ThumbButtonInfo Thumb(string label, Action action)
    {
        var button = new ThumbButtonInfo { Description = label, DismissWhenClicked = false };
        button.Click += (_, _) => action();
        return button;
    }
}
