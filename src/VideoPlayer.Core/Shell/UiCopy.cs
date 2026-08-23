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
    public const string OpenUrl = "URL 열기";
    public const string OpenUrlPlaceholder = "https://";
    public const string OpenUrlExample = "예: https://example.com/video.mp4";
    public const string OpenUrlHttpOnly = "http(s)만";
    public const string OpenUrlHttpOnlyReason = "http(s)만 열 수 있습니다.";
    public const string OpenUrlEmpty = "주소를 입력하세요.";
    public const string OpenUrlNoFileScheme = "file: 주소는 열 수 없습니다. http(s)만 지원합니다.";
    public const string OpenUrlNoRtmp = "rtmp는 열 수 없습니다. http(s)만 지원합니다.";
    public const string OpenUrlNoCookiesOrHeaders = "쿠키·헤더는 지원하지 않습니다. 주소만 입력하세요.";
    public const string OpenUrlNoLogin = "로그인 정보는 지원하지 않습니다. http(s) 주소만 입력하세요.";
    public const string OpenUrlAction = "열기";
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

    public const string ClipSave = "구간 저장";
    public const string ClipStart = "시작";
    public const string ClipEnd = "끝";
    public const string ClipDuration = "길이";
    public const string ClipFormat = "형식";
    public const string ClipFormatStreamCopy = "원본복사";
    public const string ClipFormatWebp = "webp";
    public const string ClipFormatGif = "gif";
    public const string ClipFps = "fps";
    public const string ClipFpsSource = "원본";
    public const string ClipPingPong = "핑퐁";
    public const string ClipPalette = "팔레트";
    public const string ClipPaletteValue = "256색";
    public const string ClipEncodingOff = "원본복사에서는 꺼짐";
    public const string ClipKeyframeNotice = "키프레임 단위로 저장됩니다.";
    public const string ClipFolder = "폴더";
    public const string ClipChangeFolder = "변경";
    public const string ClipSaveAction = "저장";
    public const string ClipCancel = "취소";
    public const string ClipNoMedia = "재생 중인 영상이 없습니다.";
    public const string ClipTooShort = "구간은 1초 이상이어야 합니다.";
    public const string ClipSaveFailed = "저장할 수 없습니다.";
    public const string ClipFfmpegMissing = "ffmpeg를 찾을 수 없습니다.";

    public static IReadOnlyList<string> MainMenus { get; } = [FileMenu, ViewMenu];

    public static IReadOnlyList<string> FileMenuItems { get; } = [OpenFile, OpenUrl, OpenFolder, Exit];
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
