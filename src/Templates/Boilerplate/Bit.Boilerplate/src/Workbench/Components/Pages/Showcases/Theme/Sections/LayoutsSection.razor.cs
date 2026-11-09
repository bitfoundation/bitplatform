namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The Layouts category: headers, footers, the grid, a whole layout, the spacer and the stack, each in a box of its own.</summary>
public partial class LayoutsSection
{
    /// <summary>How many of the grid's twelve columns each item spans, row by row.</summary>
    private static readonly int[] spans = [12, 6, 6, 4, 4, 4, 3, 3, 3, 3];

    private string[] stackItems = [];

    protected override void OnInitialized()
    {
        stackItems = [Localizer[nameof(WorkbenchStrings.ThemeLayoutsBread)], Localizer[nameof(WorkbenchStrings.ThemeLayoutsPastry)], Localizer[nameof(WorkbenchStrings.ThemeLayoutsCoffee)], Localizer[nameof(WorkbenchStrings.ThemeLayoutsTea)], Localizer[nameof(WorkbenchStrings.ThemeLayoutsCakes)]];
    }
}
