namespace VideoPlayer.Core.Shell;

/// <summary>Confirmed A v2 P0 chrome copy. Old wireframe A discarded.</summary>
public static class UiCopy
{
    public const string AppTitle = "영상 플레이어";
    public const string FileMenu = "파일";
    public const string ViewMenu = "보기";
    public const string MenuSeparator = "|";

    public const string SidebarTitle = "최근 / 시리즈";
    public const string ContinueWatching = "이어보기";
    public const string RecentSeries = "최근 시리즈";
    public const string SkipBack = "-10초";
    public const string SkipForward = "+10초";
    public const string NextEpisode = "다음 화";
    public const string NextEpisodeCta = "다음 화 >";
    public const string NextEpisodeCancel = "취소";
    public const string PreviousEpisodeIcon = "⏮";
    public const string NextEpisodeIcon = "⏭";
    public const string Captions = "CC";
    public const string SpeedDefault = "1.0x";

    public const string OpenFolder = "폴더 열기";
    public const string OpenFile = "열기...";
    public const string Exit = "종료";
    public const string SeriesPanel = "시리즈";
    public const string ToggleSidebar = "사이드바";
    public const string AutoNext = "다음 화 자동 재생";
    public const string Fullscreen = "전체화면";
    public const string PreviousEpisode = "이전 화";
    public const string ColumnEpisode = "회차";
    public const string ColumnTitle = "제목";
    public const string ColumnProgress = "진행";
    public const string Back = "뒤로";

    public const string Unsupported = "미지원";
    public const string SoftwareFallback = "SW 폴백";

    public static IReadOnlyList<string> MainMenus { get; } = [FileMenu, ViewMenu];
}

public enum ShellScreen
{
    Main,
    Fullscreen,
    Series
}

public enum SeriesDrillLevel
{
    Shows,
    Seasons,
    Episodes
}
