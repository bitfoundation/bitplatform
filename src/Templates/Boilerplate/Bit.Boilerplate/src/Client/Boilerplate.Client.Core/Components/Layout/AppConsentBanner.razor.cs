namespace Boilerplate.Client.Core.Components.Layout;

/// <summary>
/// Asks, once, about the categories this deployment can act on. Renders nothing when
/// <see cref="ConsentService.AskableCategories"/> is empty - the shipped default - because a banner whose answer
/// changes nothing teaches people to dismiss the one that matters.
/// </summary>
public partial class AppConsentBanner
{
    private bool isOpen;
    private bool isHeightTracked;
    private BitStack? bannerStack;

    [AutoInject] private ConsentService consentService = default!;

    /// <summary>
    /// After the first render: the answer lives in <c>IStorageService</c>, unreachable while prerendering, so
    /// deciding earlier flashes the banner at somebody who already answered.
    /// </summary>
    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        isOpen = await consentService.IsPending();

        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (isOpen is false)
        {
            isHeightTracked = false;
        }
        else if (isHeightTracked is false && bannerStack is not null)
        {
            isHeightTracked = true;
            await JSRuntime.InvokeVoidAsync("App.trackHeight", bannerStack.RootElement, "--app-consent-banner-height");
        }
    }

    private async Task AcceptAll()
    {
        await consentService.SetAll(true);
        isOpen = false;
    }

    private async Task RejectAll()
    {
        await consentService.SetAll(false);
        isOpen = false;
    }
}
