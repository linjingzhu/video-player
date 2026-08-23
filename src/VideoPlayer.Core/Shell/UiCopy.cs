namespace VideoPlayer.Core.Shell;

/// <summary>Korean chrome copy from the product wireframes.</summary>
public static class UiCopy
{
    public const string AppTitle = "영상 플레이어";
    public const string FileMenu = "파일";
    public const string PlayMenu = "재생";
    public const string SeriesMenu = "시리즈";
    public const string ViewMenu = "보기";
    public const string HelpMenu = "도움";

    public const string SidebarTitle = "최근 / 시리즈";
    public const string ContinueWatching = "이어보기";
    public const string SampleDrama = "드라마 S01";
    public const string SampleMovie = "영화 A";
    public const string SampleEpisode = "에피소드 03";
    public const string SubtitlePlaceholder = "자막 예시";

    public const string SkipBack = "-10초";
    public const string SkipForward = "+10초";
    public const string NextEpisode = "다음 화 >";
    public const string Captions = "CC";

    public const string OpenFolder = "폴더 열기";
    public const string AddToPlaylist = "재생목록에 담기";
    public const string SortByEpisode = "정렬: 회차";
    public const string ColumnEpisode = "회차";
    public const string ColumnFileName = "파일명";
    public const string ColumnDuration = "길이";
    public const string ColumnProgress = "진행";

    public const string OpenFile = "열기...";
    public const string LoadPlaylist = "재생목록 불러오기...";
    public const string SavePlaylist = "재생목록 저장...";
    public const string Exit = "종료";
    public const string PlayPause = "재생/일시정지";
    public const string Previous = "이전";
    public const string Next = "다음";
    public const string FrameStepForward = "한 프레임 앞";
    public const string FrameStepBack = "한 프레임 뒤";
    public const string Speed = "배속";
    public const string Subtitles = "자막";
    public const string SeriesPanel = "시리즈 패널";
    public const string AutoNext = "다음 화 자동 재생";
    public const string Fullscreen = "전체화면";
    public const string AlwaysOnTop = "항상 위";
    public const string FitContain = "맞춤";
    public const string FitCover = "채우기";
    public const string About = "정보";

    public static IReadOnlyList<string> MainMenus { get; } =
    [
        FileMenu, PlayMenu, SeriesMenu, ViewMenu, HelpMenu
    ];
}

public enum ShellScreen
{
    Main,
    Fullscreen,
    Series
}

public enum SeriesSortMode
{
    Episode,
    FileName,
    Duration,
    Progress
}
