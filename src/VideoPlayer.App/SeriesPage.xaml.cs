using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Safety;
using VideoPlayer.Core.Series;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.App;

public partial class SeriesPage : UserControl
{
    private SeriesDrillDown? _drill;
    private ResumeStore? _resume;
    private string? _currentPath;
    private string? _treeKey;
    private bool _binding;

    public SeriesPage()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? BackRequested;
    public event RoutedEventHandler? OpenFolderRequested;
    public event EventHandler<SeriesListItem>? EpisodeActivated;

    public void Bind(SeriesDrillDown drill, ResumeStore resume, string? currentPath)
    {
        _drill = drill;
        _resume = resume;
        _currentPath = currentPath;
        _binding = true;
        try
        {
            TitleText.Text = drill.Heading();
            FooterLeftText.Text = drill.FooterLeft();
            FooterRightText.Text = drill.FooterRight();
            SeriesGrid.ItemsSource = drill.ListItems(resume, currentPath);
            var key = TreeKey(drill);
            if (key != _treeKey)
            {
                _treeKey = key;
                RebuildTree(drill);
            }
        }
        finally
        {
            _binding = false;
        }
    }

    private static string TreeKey(SeriesDrillDown drill)
        => string.Join('\n', drill.Tree().SelectMany(show =>
            show.Children.Select(season => $"{show.Path}\t{season.Path}\t{season.Selected}").DefaultIfEmpty($"{show.Path}\t\t{show.Selected}")));

    private void RebuildTree(SeriesDrillDown drill)
    {
        SeriesTree.Items.Clear();
        foreach (var show in drill.Tree())
        {
            var showItem = new TreeViewItem
            {
                Header = FolderHeader(show.Label),
                Tag = show,
                IsExpanded = true
            };

            foreach (var season in show.Children)
            {
                var seasonItem = new TreeViewItem
                {
                    Header = FolderHeader(season.Label),
                    Tag = season,
                    IsSelected = season.Selected
                };
                showItem.Items.Add(seasonItem);
            }

            SeriesTree.Items.Add(showItem);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
        => BackRequested?.Invoke(this, e);

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
        => OpenFolderRequested?.Invoke(this, e);

    private void SeriesTree_Selected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_binding || _drill is null || SeriesTree.SelectedItem is not TreeViewItem item
            || item.Tag is not SeriesTreeNode node)
        {
            return;
        }

        if (node.Kind == "season")
        {
            var season = FindSeason(node.Path);
            if (season is not null)
            {
                _drill.OpenSeason(season);
            }
        }
        else if (node.Kind == "show")
        {
            var show = _drill.Shows.FirstOrDefault(s =>
                string.Equals(s.RootPath, node.Path, PathValidator.PathComparison));
            if (show is not null)
            {
                _drill.OpenShow(show);
            }
        }

        if (_resume is not null)
        {
            Bind(_drill, _resume, _currentPath);
        }
    }

    private void SeriesGrid_Activate(object sender, MouseButtonEventArgs e)
    {
        if (SeriesGrid.SelectedItem is SeriesListItem item)
        {
            EpisodeActivated?.Invoke(this, item);
        }
    }

    private SeriesSeason? FindSeason(string folderPath)
        => _drill?.Shows
            .SelectMany(s => s.Seasons)
            .FirstOrDefault(s => string.Equals(s.FolderPath, folderPath, PathValidator.PathComparison));

    private static FrameworkElement FolderHeader(string label)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(new Path
        {
            Data = Geometry.Parse("M3.2,6.4 H8.1 L9.7,8.2 H20.8 V17.2 H3.2 Z"),
            Stroke = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF7)),
            StrokeThickness = 1.15,
            Fill = Brushes.Transparent,
            Width = 15,
            Height = 12,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        });
        row.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF7)),
            VerticalAlignment = VerticalAlignment.Center
        });
        return row;
    }
}
