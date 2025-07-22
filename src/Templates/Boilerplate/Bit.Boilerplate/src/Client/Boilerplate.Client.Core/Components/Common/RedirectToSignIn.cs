namespace Boilerplate.Client.Core.Components.Common;

public partial class RedirectToSignIn : AppComponentBase
{
    [Parameter] public string? ReturnUrl { get; set; }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await AuthManager.SignOut(CurrentCancellationToken);
        var returnUrl = ReturnUrl ?? NavigationManager.GetRelativePath();
        NavigationManager.NavigateTo($"{PageUrls.SignIn}?return-url={Uri.EscapeDataString(returnUrl)}");
    }
}
