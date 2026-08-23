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

    public const string Capture = "캡처";
    public const string CaptureSheetTitle = Capture;
    public const string CaptureCount = "장수";
    public const string CaptureInterval = "간격";
    public const string CaptureFormatLabel = "포맷";
    public const string CaptureFolder = "폴더";
    public const string CaptureCountRange = "1-999";
    public const string CaptureChangeFolder = "변경";
    public const string CaptureStart = "시작";
    public const string CaptureCancel = "취소";
    public const string CaptureFooter = "현재 위치부터 · 캡처 중 일시정지";
    public const string CaptureShortcut = "Ctrl+Shift+C";
    public const string CaptureConfirm = "60장 이상을 캡처합니다. 계속할까요?";
    public const string CaptureNoMedia = "재생 중인 영상이 없습니다.";
    public const string CaptureSaveFailed = "저장할 수 없습니다.";
    public const string CaptureEofBanner = "{0}장 중 {1}장";
    public const string CapturePartialFailBanner = "캡처 실패 · {0}장 저장됨 (요청 {1}장)";

    public const string Subtitles = "자막";
    public const string SecondarySubtitles = "보조 자막 (상단)";
    public const string PrimarySubtitles = "주 자막 (하단)";
    public const string SubtitleOff = "꺼짐";
    public const string SubtitleFooter = "주 · 상단에 보조";

    public const string SkipIntro = "인트로 건너뛰기";
    public const string SkipRecap = "리캡 건너뛰기";
    public const string SkipCredits = "크레딧 건너뛰기";
    public const string SkipCancel = "취소";
    public const string SkipCancelCountdown = "취소 ({0})";
    public const string SkipToHere = "여기까지 스킵";
    public const string SkipAuto = "건너뛰기 자동";

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
