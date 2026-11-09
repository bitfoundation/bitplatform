namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The theme's type: BitText's ramp, the font families, the size ramp, the weights and the text settings of controls.</summary>
public partial class TypographySection
{
    /// <summary>Every typography except Inherit, which has no look of its own.</summary>
    private static readonly BitTypography[] typographies = [.. Enum.GetValues<BitTypography>().Where(t => t is not BitTypography.Inherit)];

    private string SampleText => Localizer[nameof(WorkbenchStrings.ThemeSampleText)];
}
