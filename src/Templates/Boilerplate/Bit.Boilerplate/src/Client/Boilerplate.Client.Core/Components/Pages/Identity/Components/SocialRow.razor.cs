namespace Boilerplate.Client.Core.Components.Pages.Identity.Components;

public partial class SocialRow
{
    [Parameter] public EventCallback<string> OnClick { get; set; }

    [Parameter] public bool IsWaiting { get; set; }

    private async Task HandleGoogle()
    {
        await OnClick.InvokeAsync("Google");
    }

    private async Task HandleGitHub()
    {
        await OnClick.InvokeAsync("GitHub");
    }

    private async Task HandleTwitter()
    {
        await OnClick.InvokeAsync("Twitter");
    }

    private async Task HandleApple()
    {
        await OnClick.InvokeAsync("Apple");
    }

    private async Task HandleAzureAD()
    {
        await OnClick.InvokeAsync("AzureAD");
    }

    private async Task HandleIdentityServerDemo()
    {
        await OnClick.InvokeAsync("IdentityServerDemo");
    }
}
