using VideoPlayer.Core.Safety;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.Core.Subtitles;

public sealed record SubtitleTrackRow(string Label, string? Path, bool Selected, bool Suggested);

public sealed class SubtitleSheetState
{
    public bool Open { get; set; }
    public string? PrimaryPath { get; set; }
    public string? SecondaryPath { get; set; }
    public string? SuggestedSecondaryPath { get; set; }
    public List<string> AvailablePaths { get; } = [];

    public string Title { get; } = UiCopy.Subtitles;
    public string SecondaryHeading { get; } = UiCopy.SecondarySubtitles;
    public string PrimaryHeading { get; } = UiCopy.PrimarySubtitles;
    public string OffLabel { get; } = UiCopy.SubtitleOff;
    public string Footer { get; } = UiCopy.SubtitleFooter;
    public string PanelColor { get; } = SkinA.Panel;
    public string BackgroundColor { get; } = SkinA.Background;
    public string AccentColor { get; } = SkinA.Accent;
    public int PanelRadius { get; } = SkinA.RadiusPanel;
    public bool CcOpensSheet { get; } = false;
    public bool CcHasLongPress { get; } = false;
    public bool HasDelaySheet { get; } = false;
    public bool SecondaryNeverAutoOn { get; } = true;
    public bool PrimaryIsBottom { get; } = true;
    public bool SecondaryIsTop { get; } = true;

    public IReadOnlyList<SubtitleTrackRow> SecondaryRows => RowsFor(SecondaryPath, suggestEnglish: true);

    public IReadOnlyList<SubtitleTrackRow> PrimaryRows => RowsFor(PrimaryPath, suggestEnglish: false);

    public void Close() => Open = false;

    public void Bind(IReadOnlyList<string> paths, string? suggestedSecondary)
    {
        AvailablePaths.Clear();
        foreach (var path in paths)
        {
            if (!string.IsNullOrWhiteSpace(path) && !AvailablePaths.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                AvailablePaths.Add(path);
            }
        }

        SuggestedSecondaryPath = suggestedSecondary;
        if (PrimaryPath is not null && !AvailablePaths.Contains(PrimaryPath, StringComparer.OrdinalIgnoreCase))
        {
            PrimaryPath = null;
        }

        if (SecondaryPath is not null && !AvailablePaths.Contains(SecondaryPath, StringComparer.OrdinalIgnoreCase))
        {
            SecondaryPath = null;
        }
    }

    public void SelectPrimary(string? path)
        => PrimaryPath = NormalizeSelection(path);

    public void SelectSecondary(string? path)
        => SecondaryPath = NormalizeSelection(path);

    private string? NormalizeSelection(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return AvailablePaths.FirstOrDefault(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyList<SubtitleTrackRow> RowsFor(string? selected, bool suggestEnglish)
    {
        var rows = new List<SubtitleTrackRow>
        {
            new(OffLabel, null, selected is null, false)
        };

        IEnumerable<string> ordered = AvailablePaths;
        if (suggestEnglish && SuggestedSecondaryPath is { } hint)
        {
            ordered = AvailablePaths
                .OrderBy(path => string.Equals(path, hint, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);
        }

        foreach (var path in ordered)
        {
            rows.Add(new SubtitleTrackRow(
                FileNameSanitizer.ForDisplay(Path.GetFileName(path)),
                path,
                selected is not null && string.Equals(path, selected, StringComparison.OrdinalIgnoreCase),
                suggestEnglish && SuggestedSecondaryPath is not null
                    && string.Equals(path, SuggestedSecondaryPath, StringComparison.OrdinalIgnoreCase)));
        }

        return rows;
    }
}
