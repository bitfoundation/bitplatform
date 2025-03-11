using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

public partial class SignInPanel
{
    private const string EmailKey = nameof(EmailKey);
    private const string PhoneKey = nameof(PhoneKey);


    private bool isWebAuthnAvailable;
    private string? selectedKey = EmailKey;


    [Parameter] public bool IsWaiting { get; set; }

    [Parameter] public SignInRequestDto Model { get; set; } = default!;

    [Parameter] public EventCallback<string?> OnSocialSignIn { get; set; }

    [Parameter] public EventCallback OnPasswordlessSignIn { get; set; }

    [Parameter] public EventCallback OnSendOtp { get; set; }

    [Parameter] public EventCallback<SignInPanelTab> OnTabChange { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "return-url")]
    public string? ReturnUrlQueryString { get; set; }


    protected override async Task OnAfterFirstRenderAsync()
    {
        isWebAuthnAvailable = await JSRuntime.IsWebAuthnAvailable();
        StateHasChanged();

        if (await JSRuntime.IsWebAuthnConfigured())
        {
            await OnPasswordlessSignIn.InvokeAsync();
        }

        await base.OnAfterFirstRenderAsync();
    }


    private async Task HandleOnPivotChange(BitPivotItem item)
    {
        selectedKey = item.Key;

        if (item.Key is EmailKey)
        {
            await OnTabChange.InvokeAsync(SignInPanelTab.Email);
        }

        if (item.Key is PhoneKey)
        {
            await OnTabChange.InvokeAsync(SignInPanelTab.Phone);
        }
    }
}
