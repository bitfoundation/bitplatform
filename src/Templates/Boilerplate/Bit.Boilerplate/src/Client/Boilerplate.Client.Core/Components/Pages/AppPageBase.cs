namespace Boilerplate.Client.Core.Components.Pages;

public abstract partial class AppPageBase : AppComponentBase
{
    [Parameter] public string? culture { get; set; }

    protected BitDataGridStrings DataGridStrings { get; private set; } = default!;

    protected override async Task OnInitAsync()
    {
        DataGridStrings = new()
        {
            PagerPageFormat = $"{Localizer[nameof(AppStrings.Page)]} {{0}} {Localizer[nameof(AppStrings.Of)]} {{1}}",
            PagerRangeFormat = $"{{0}}–{{1}} {Localizer[nameof(AppStrings.Of)]} {{2}}"
        };

        await base.OnInitAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            if (string.IsNullOrEmpty(culture) is false)
            {
                if (CultureInfoManager.InvariantGlobalization || CultureInfoManager.SupportedCultures.Any(sc => string.Equals(sc.Culture.Name, culture, StringComparison.InvariantCultureIgnoreCase)) is false)
                {
                    // Because Blazor router doesn't support regex, the '/{culture?}/' captures some irrelevant routes
                    // such as non existing routes like /some-invalid-url, we need to make sure that the first segment of the route is a valid culture name.
                    NavigationManager.NavigateTo($"{PageUrls.NotFound}?url={Uri.EscapeDataString(NavigationManager.GetRelativePath())}", replace: true);
                }
            }
        }
    }
}
