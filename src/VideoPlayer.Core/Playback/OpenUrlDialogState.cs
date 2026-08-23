using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Playback;

/// <summary>
/// File &gt; URL 열기 dialog state. 열기 stays disabled until the field is a parseable http(s) URL.
/// </summary>
public sealed class OpenUrlDialogState
{
    public string Text { get; private set; } = "";
    public bool CanOpen { get; private set; }
    public string Title { get; } = UiCopy.OpenUrl;
    public string Placeholder { get; } = UiCopy.OpenUrlPlaceholder;
    public string Example { get; } = UiCopy.OpenUrlExample;
    public string HttpOnly { get; } = UiCopy.OpenUrlHttpOnly;
    public string OpenLabel { get; } = UiCopy.OpenUrlAction;
    public string CancelLabel { get; } = UiCopy.NextEpisodeCancel;
    public bool HasCookieAuthUi { get; } = false;
    public bool HasDrmUi { get; } = false;
    public bool HasPaidUnlockUi { get; } = false;
    public bool HasHeaderUi { get; } = false;

    public void SetText(string? value)
    {
        Text = value ?? "";
        CanOpen = OpenUrlRules.IsAcceptedHttpUrl(Text);
    }
}
