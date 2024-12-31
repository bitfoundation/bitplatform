
namespace Boilerplate.Client.Core.Components.Pages;

public abstract partial class AppPageBase : AppComponentBase
{
    protected virtual string? Title { get; }
    protected virtual string? Subtitle { get; }

    [Parameter] public string? culture { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            if (string.IsNullOrEmpty(culture) is false)
            {
                if (CultureInfoManager.MultilingualEnabled is false || CultureInfoManager.SupportedCultures.Any(sc => string.Equals(sc.Culture.Name, culture, StringComparison.InvariantCultureIgnoreCase)) is false)
                {
                    // Because Blazor router doesn't support regex, the '/{culture?}/' captures some irrelevant routes
                    // such as non existing routes like /some-invalid-url, we need to make sure that the first segment of the route is a valid culture name.
                    NavigationManager.NavigateTo($"{Urls.NotFoundPage}?url={NavigationManager.GetRelativePath()}", replace: true);
                }
            }

            PubSubService.Publish(ClientPubSubMessages.PAGE_TITLE_CHANGED, (Title, Subtitle), persistent: true);
        }
    }
}
