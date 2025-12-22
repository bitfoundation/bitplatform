using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Components;

public partial class ExternalIdentityProviders
{
    private bool isLoadingProviders = true;
    private string[] supportedProviders = [];

    [Parameter] public bool IsWaiting { get; set; }
    [Parameter] public bool IsInModal { get; set; }
    [Parameter] public EventCallback<string> OnClick { get; set; }


    [AutoInject] private IIdentityController IdentityController = default!;


    protected override async Task OnInitAsync()
    {
        try
        {
            var providers = await IdentityController.GetSupportedExternalAuthSchemes(CurrentCancellationToken);
            supportedProviders = providers;
        }
        finally
        {
            isLoadingProviders = false;
        }
    }

    private async Task HandleGoogle() => await OnClick.InvokeAsync("Google");
    private async Task HandleGitHub() => await OnClick.InvokeAsync("GitHub");
    private async Task HandleTwitter() => await OnClick.InvokeAsync("Twitter");
    private async Task HandleApple() => await OnClick.InvokeAsync("Apple");
    private async Task HandleAzureAD() => await OnClick.InvokeAsync("AzureAD");
    private async Task HandleFacebook() => await OnClick.InvokeAsync("Facebook");
    private async Task HandleKeycloak() => await OnClick.InvokeAsync("Keycloak");
}
