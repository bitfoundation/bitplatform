namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The theme's colors: each role in all its slots, the text, background and border tiers, the grays, the special colors and the intent tier.</summary>
public partial class ColorsSection
{
    private (string Title, ThemeToken[] Tokens)[] families = [];

    protected override void OnInitialized()
    {
        families =
        [
            .. ThemeTokens.Roles.Concat(ThemeTokens.Neutrals).Select(family => ((string)Localizer[family.NameKey], family.Tokens)),
            (Localizer[nameof(WorkbenchStrings.ThemeColorsGrays)], ThemeTokens.Grays),
            (Localizer[nameof(WorkbenchStrings.ThemeColorsSpecialPurpose)], ThemeTokens.SpecialColors),
            (Localizer[nameof(WorkbenchStrings.ThemeColorsIntent)], ThemeTokens.Semantic)
        ];
    }
}
