namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// An icon name with its glyph; the glyph opens a picker of bit BlazorUI's icons with the search of its demo's icon page.
/// </summary>
public partial class IconField
{
    // As many icons as the column shows without the grid getting slow; a search narrows the rest down.
    private const int MaxShown = 120;

    private bool isOpen;
    private string? term;
    private string[] shown = [];
    private int total;

    [Parameter] public string? Value { get; set; }

    [Parameter] public EventCallback<string?> OnChange { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public string? AriaLabel { get; set; }

    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Opens the picker on the icons the current term finds, or closes it.</summary>
    private void Toggle()
    {
        isOpen = isOpen is false;

        if (isOpen)
        {
            Search();
        }
    }

    private void Search()
    {
        var matches = WorkbenchIconSearch.Search(term).ToArray();

        total = matches.Length;
        shown = matches[..Math.Min(MaxShown, matches.Length)];
    }

    private async Task Change(string? value)
    {
        isOpen = false;

        await OnChange.InvokeAsync(string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }
}
