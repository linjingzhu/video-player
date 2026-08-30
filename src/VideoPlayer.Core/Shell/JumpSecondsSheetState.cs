using VideoPlayer.Core.Library;

namespace VideoPlayer.Core.Shell;

/// <summary>
/// 보기 / 퀵메뉴 점프 초 sheet. One global integer, same value for forward and back.
/// No transport-bar control.
/// </summary>
public sealed class JumpSecondsSheetState
{
    public bool Open { get; set; }
    public int Draft { get; set; } = JumpInterval.DefaultSeconds;
    public bool HasTransportControl { get; } = false;
    public bool OpensFromViewMenuOnly { get; } = true;
    public bool SameValueForwardBack { get; } = true;
    public bool SeparateForwardBackFields { get; } = false;

    public string Title { get; } = UiCopy.JumpSeconds;
    public string SameValueLabel { get; } = UiCopy.JumpSecondsSameValue;
    public string Hint { get; } = UiCopy.JumpSecondsHint;
    public string Footer { get; } = UiCopy.JumpSecondsFooter;
    public string CancelLabel { get; } = UiCopy.JumpSecondsCancel;
    public string ConfirmLabel { get; } = UiCopy.JumpSecondsConfirm;
    public string MinusLabel { get; } = UiCopy.JumpSecondsMinus;
    public string PlusLabel { get; } = UiCopy.JumpSecondsPlus;
    public string PanelColor { get; } = SeriesOn.Panel;
    public string BackgroundColor { get; } = SeriesOn.Background;
    public string AccentColor { get; } = SeriesOn.Accent;
    public string OnAccentColor { get; } = SeriesOn.OnAccent;
    public string TextColor { get; } = SeriesOn.Text;
    public string SecondaryColor { get; } = SeriesOn.Secondary;
    public int PanelRadius { get; } = SeriesOn.RadiusPanel;

    public string ValueText => Draft.ToString();
    public string QuickMenuPreview => UiCopy.JumpSecondsQuickMenuPreview(Draft);
    public string OsdPreview => UiCopy.JumpSecondsOsdPreview(Draft);
    public string ArrowPreview => UiCopy.JumpSecondsArrowPreview(Draft);

    public void Bind(int current)
        => Draft = JumpInterval.ClampDraft(JumpInterval.IsInRange(current) ? current : JumpInterval.DefaultSeconds);

    public void Nudge(int delta)
        => Draft = JumpInterval.ClampDraft(Draft + delta);

    public void Close() => Open = false;
}
