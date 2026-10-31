namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

public partial class ThemePage
{
    private static readonly string[] variants = ["", "-hover", "-active", "-dark", "-light", "-text"];

    private static readonly (string NameKey, string[] Tokens)[] families =
    [
        .. new (string NameKey, string Code)[]
        {
            (nameof(WorkbenchStrings.ThemePrimary), "pri"),
            (nameof(WorkbenchStrings.ThemeSecondary), "sec"),
            (nameof(WorkbenchStrings.ThemeTertiary), "ter"),
            (nameof(WorkbenchStrings.ThemeInfo), "inf"),
            (nameof(WorkbenchStrings.ThemeSuccess), "suc"),
            (nameof(WorkbenchStrings.ThemeWarning), "wrn"),
            (nameof(WorkbenchStrings.ThemeSevereWarning), "swr"),
            (nameof(WorkbenchStrings.ThemeError), "err")
        }.Select(family => (family.NameKey, variants.Select(variant => $"--bit-clr-{family.Code}{variant}").ToArray())),
        (nameof(WorkbenchStrings.ThemeForeground), ["--bit-clr-fg-pri", "--bit-clr-fg-sec", "--bit-clr-fg-ter", "--bit-clr-fg-dis"]),
        (nameof(WorkbenchStrings.ThemeBackground), ["--bit-clr-bg-pri", "--bit-clr-bg-sec", "--bit-clr-bg-ter", "--bit-clr-bg-dis", "--bit-clr-bg-overlay"]),
        (nameof(WorkbenchStrings.ThemeBorder), ["--bit-clr-brd-pri", "--bit-clr-brd-sec", "--bit-clr-brd-ter", "--bit-clr-brd-dis"])
    ];

    private static readonly BitTypography[] typographies = [.. Enum.GetValues<BitTypography>().Where(t => t is not BitTypography.Inherit)];

    private static readonly string[] radii = ["xs", "sm", "md", "lg", "xl", "2xl", "control", "surface", "popup", "dialog", "full"];

    private static readonly BitColor[] buttonColors = [BitColor.Primary, BitColor.Secondary, BitColor.Tertiary, BitColor.Info, BitColor.Success, BitColor.Warning, BitColor.SevereWarning, BitColor.Error];

    private static readonly BitColor[] messageColors = [BitColor.Info, BitColor.Success, BitColor.Warning, BitColor.SevereWarning, BitColor.Error];

    private Dictionary<string, string> values = [];
    private List<BitDropdownItem<string>> options = [];
    private List<BitChoiceGroupItem<string>> choices = [];

    protected override void OnInitialized()
    {
        options = [.. Enumerable.Range(1, 4).Select(index => new BitDropdownItem<string> { Value = index.ToString(CultureInfo.InvariantCulture), Text = Localizer[nameof(WorkbenchStrings.ThemeOption), index] })];
        choices = [.. Enumerable.Range(1, 3).Select(index => new BitChoiceGroupItem<string> { Value = index.ToString(CultureInfo.InvariantCulture), Text = Localizer[nameof(WorkbenchStrings.ThemeOption), index] })];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
            return;

        values = await JSRuntime.InvokeAsync<Dictionary<string, string>>("workbench.readTokens", [families.SelectMany(family => family.Tokens).ToArray()]);

        StateHasChanged();
    }
}
