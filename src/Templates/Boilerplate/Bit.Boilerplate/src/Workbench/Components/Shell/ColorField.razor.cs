using System.Text.RegularExpressions;

namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// A color as text, with a filled circle that previews it and opens a color picker. The picker writes its color once a
/// drag ends, so a drag doesn't save a change at every step.
/// </summary>
public partial class ColorField
{
    private bool isOpen;
    // The picker's color while it is dragged, previewed in the circle until the drag ends.
    private string? draft;

    [Parameter] public string? Value { get; set; }

    [Parameter] public EventCallback<string?> OnChange { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public string? AriaLabel { get; set; }

    [Parameter] public string? Placeholder { get; set; }

    private string? Shown => draft ?? Value;

    /// <summary>Whether a text is a color the circle and the picker can show: hex, rgb() or hsl(), or transparent.</summary>
    public static bool IsColor(string? value) => value is not null && ColorPattern().IsMatch(value.Trim());

    protected override void OnParametersSet()
    {
        // The saved color came back from the code; the draft has done its job.
        if (draft is not null && string.Equals(draft, Value, StringComparison.OrdinalIgnoreCase))
        {
            draft = null;
        }
    }

    private void Toggle() => isOpen = isOpen is false;

    private Task Commit(BitColorChangeEventArgs args)
    {
        draft = args.Color;

        return Change(args.Color);
    }

    private Task Change(string? value) => OnChange.InvokeAsync(string.IsNullOrWhiteSpace(value) ? null : value.Trim());

    [GeneratedRegex(@"^(#(?:[0-9a-f]{3,4}|[0-9a-f]{6}|[0-9a-f]{8})|(?:rgb|rgba|hsl|hsla)\([^()]*\)|transparent)$", RegexOptions.IgnoreCase)]
    private static partial Regex ColorPattern();
}
