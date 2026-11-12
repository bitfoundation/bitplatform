namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The Workbench's own language, next to its theme on every page; a view keeps the language of its scenario.
/// </summary>
public partial class LanguageMenu
{
    private BitMenuButtonItem[] items = [];

    private static readonly BitMenuButtonClassStyles styles = new() { Callout = "--bit-MenuButton-callout-min-width: 10rem" };

    [AutoInject] private CultureService cultureService = default!;

    [Parameter] public BitSize? Size { get; set; }

    protected override void OnInitialized()
    {
        items = [.. CultureInfoManager.SupportedCultures.Select(sc => new BitMenuButtonItem
        {
            Key = sc.Culture.Name,
            Text = sc.DisplayName,
            RadioGroup = "language",
            IsChecked = sc.Culture.Name == CultureInfo.CurrentUICulture.Name
        })];
    }

    /// <summary>Saves the culture in the culture cookie and reloads, since a circuit keeps the culture it started with.</summary>
    private async Task SetCulture(BitMenuButtonItem? item)
    {
        if (item?.Key is not { } culture || culture == CultureInfo.CurrentUICulture.Name)
            return;

        await cultureService.ChangeCulture(culture);
    }
}
