namespace Boilerplate.Client.Core.Components.Pages;

public partial class HomePage
{
    protected override string? Title => Localizer[nameof(AppStrings.Home)];
    protected override string? Subtitle => string.Empty;

    [Parameter] public string? culture { get; set; }

    [CascadingParameter] private BitDir? currentDir { get; set; }

    protected async override Task OnAfterFirstRenderAsync()
    {
        if (string.IsNullOrEmpty(culture) is false)
        {
            if (CultureInfoManager.MultilingualEnabled is false || CultureInfoManager.SupportedCultures.Any(sc => string.Equals(sc.Culture.Name, culture, StringComparison.InvariantCultureIgnoreCase)) is false)
            {
                // Because Blazor router doesn't support regex, the HomePage.razor's route '/{culture?}/' captures some irrelevant routes
                // such as non existing routes like /some-invalid-url, we need to make sure that the first segment of the route is a valid culture name.
                NavigationManager.NavigateTo($"{Urls.NotFoundPage}?url={NavigationManager.ToBaseRelativePath(NavigationManager.Uri)}");
            }

            await base.OnAfterFirstRenderAsync();
        }
    }
}
