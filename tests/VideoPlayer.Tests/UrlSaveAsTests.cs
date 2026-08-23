using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class UrlSaveAsTests
{
    [Fact]
    public void Save_as_is_file_menu_os_dialog_only_and_url_gated()
    {
        Assert.Equal("다른 이름으로 저장", UiCopy.SaveAs);
        Assert.Contains(UiCopy.SaveAs, UiCopy.FileMenuItems);
        Assert.True(UrlSaveAs.UsesOsDialog);
        Assert.False(UrlSaveAs.HasInAppSheet);
        Assert.False(UrlSaveAs.PromptsForCookies);
        Assert.False(UrlSaveAs.PromptsForKeys);
        Assert.False(UrlSaveAs.PromptsForHeaders);

        var shell = PlayerShell.Boot();
        Assert.False(shell.HasSaveAsSheet);
        Assert.True(shell.SaveAsUsesOsDialog);
        Assert.False(shell.HasCookieAuthUi);
        Assert.False(shell.HasDrmUi);
        Assert.True(shell.HasHeaderUi);
        Assert.DoesNotContain(
            shell.Transport.Order,
            control => control.ToString().Contains("Save", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Local_file_cannot_save_as_and_does_not_get()
    {
        using var workspace = new TempWorkspace();
        var video = workspace.File("local.mkv", [1, 2, 3]);
        var dest = Path.Combine(workspace.Root, "copy.mkv");
        var client = new RecordingGetClient();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.Open(video);

        Assert.False(session.CanSaveAs);
        Assert.False(UrlSaveAs.CanSave(session.SourceKind, session.Current?.Path));
        var saved = session.SaveAs(dest, client);
        Assert.False(saved.Success);
        Assert.Empty(client.Calls);
        Assert.False(File.Exists(dest));
        Assert.True(session.Shell.Status.DashedSlot);
        Assert.True(StatusText.IsConfirmedFailureLine(session.Shell.Status.Text));
        Assert.False(session.Shell.HasCookieAuthUi);
        Assert.False(session.Shell.HasSaveAsSheet);
    }

    [Fact]
    public void Url_save_as_plain_gets_the_same_url_to_the_chosen_path()
    {
        using var workspace = new TempWorkspace();
        const string url = "https://cdn.example.com/show/E01.mp4";
        var dest = Path.Combine(workspace.Root, "saved.mp4");
        var client = new RecordingGetClient { Payload = [9, 8, 7, 6] };
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.True(session.OpenUrl(url).Success);
        Assert.True(session.CanSaveAs);
        Assert.False(session.CanCapture);
        Assert.False(session.CanClipSave);
        Assert.False(session.CanUseSeriesTree);

        var saved = session.SaveAs(dest, client);
        Assert.True(saved.Success);
        Assert.Equal(dest, saved.Path);
        Assert.Single(client.Calls);
        Assert.Equal(url, client.Calls[0].Url);
        Assert.Equal(dest, client.Calls[0].Destination);
        Assert.Equal("GET", client.Calls[0].Method);
        Assert.Empty(client.Calls[0].Headers);
        Assert.False(client.Calls[0].UseCookies);
        Assert.False(client.Calls[0].UseRange);
        Assert.Equal(new byte[] { 9, 8, 7, 6 }, File.ReadAllBytes(dest));
        Assert.False(session.Shell.Status.Visible);
        Assert.False(session.Shell.HasCookieAuthUi);
        Assert.False(session.Shell.HasDrmUi);
        Assert.Equal("E01.mp4", UrlSaveAs.SuggestedFileName(url));
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(407)]
    public void Credentialed_get_fails_with_banner_and_never_prompts(int status)
    {
        using var workspace = new TempWorkspace();
        var dest = Path.Combine(workspace.Root, "denied.mp4");
        var client = new RecordingGetClient
        {
            Result = new UrlGetResult(false, status, true, UiCopy.SaveAsNeedsSecrets)
        };
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.OpenUrl("https://example.com/paywall.mp4");

        var saved = session.SaveAs(dest, client);
        Assert.False(saved.Success);
        Assert.False(File.Exists(dest));
        Assert.True(session.Shell.Status.Visible);
        Assert.True(session.Shell.Status.DashedSlot);
        Assert.True(StatusText.IsConfirmedFailureLine(session.Shell.Status.Text));
        Assert.StartsWith("저장 실패", session.Shell.Status.Text);
        Assert.Contains("쿠키", session.Shell.Status.Text);
        Assert.False(session.Shell.HasCookieAuthUi);
        Assert.False(session.Shell.HasDrmUi);
        Assert.True(session.Shell.HasHeaderUi);
        Assert.False(session.Shell.HasPaidUnlockUi);
        Assert.False(UrlSaveAs.PromptsForCookies);
        Assert.False(UrlSaveAs.PromptsForKeys);
        Assert.False(UrlSaveAs.PromptsForHeaders);
    }

    [Fact]
    public void Network_failure_uses_the_dashed_banner()
    {
        using var workspace = new TempWorkspace();
        var dest = Path.Combine(workspace.Root, "down.mp4");
        var client = new RecordingGetClient
        {
            Result = new UrlGetResult(false, 502, false, UiCopy.NetworkFailed)
        };
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.OpenUrl("https://example.com/video.mp4");

        var saved = session.SaveAs(dest, client);
        Assert.False(saved.Success);
        Assert.True(StatusText.IsConfirmedFailureLine(session.Shell.Status.Text));
        Assert.Contains("연결할 수 없습니다.", session.Shell.Status.Text);
        Assert.False(session.CanCapture);
        Assert.False(session.CanClipSave);
    }

    [Fact]
    public void Keyed_hls_playlist_fails_banner_without_unwrapping_or_prompt()
    {
        using var workspace = new TempWorkspace();
        var dest = Path.Combine(workspace.Root, "master.m3u8");
        var playlist = """
            #EXTM3U
            #EXT-X-KEY:METHOD=AES-128,URI="https://pay.example/key"
            #EXTINF:4,
            seg.ts
            """;
        Assert.True(UrlSaveAs.LooksLikeKeyedHls(System.Text.Encoding.UTF8.GetBytes(playlist)));
        Assert.False(UrlSaveAs.LooksLikeKeyedHls("#EXTM3U\n#EXTINF:4,\nseg.ts"u8));
        Assert.False(UrlSaveAs.LooksLikeKeyedHls("ftypisom"u8));

        var client = new RecordingGetClient
        {
            Result = new UrlGetResult(false, 200, true, UiCopy.SaveAsNeedsSecrets)
        };
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.True(session.OpenUrl("https://example.com/video.mp4").Success);

        var saved = session.SaveAs(dest, client);
        Assert.False(saved.Success);
        Assert.False(File.Exists(dest));
        Assert.True(StatusText.IsConfirmedFailureLine(session.Shell.Status.Text));
        Assert.Contains("키", session.Shell.Status.Text);
        Assert.False(UrlSaveAs.PromptsForKeys);
        Assert.False(session.Shell.HasCookieAuthUi);
        Assert.False(session.Shell.HasSaveAsSheet);
        Assert.False(session.CanCapture);
        Assert.False(session.CanClipSave);
        Assert.True(UrlSaveAs.UsesOsDialog);
        Assert.False(UrlSaveAs.HasInAppSheet);
    }

    [Fact]
    public void Designer_lock_never_adds_a_save_as_sheet()
    {
        Assert.False(UrlSaveAs.HasInAppSheet);
        Assert.True(UrlSaveAs.UsesOsDialog);
        Assert.False(PlayerShell.Boot().HasSaveAsSheet);
        Assert.True(PlayerShell.Boot().SaveAsUsesOsDialog);
        Assert.DoesNotContain(
            Enum.GetNames<TransportControl>(),
            name => name.Contains("SaveAs", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RecordingGetClient : IUrlGetClient
    {
        public List<RecordedGet> Calls { get; } = [];
        public byte[] Payload { get; set; } = [1];
        public UrlGetResult? Result { get; set; }

        public UrlGetResult Get(string url, string destinationPath)
        {
            Calls.Add(new RecordedGet("GET", url, destinationPath, [], UseCookies: false, UseRange: false));
            if (Result is { } preset)
            {
                return preset;
            }

            File.WriteAllBytes(destinationPath, Payload);
            return new UrlGetResult(true, 200, false, null);
        }
    }

    private sealed record RecordedGet(
        string Method,
        string Url,
        string Destination,
        IReadOnlyList<string> Headers,
        bool UseCookies,
        bool UseRange);
}