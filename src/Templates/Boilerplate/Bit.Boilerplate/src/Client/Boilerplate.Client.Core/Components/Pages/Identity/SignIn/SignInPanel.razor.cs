using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

public partial class SignInPanel
{
    private const string EmailKey = nameof(EmailKey);
    private const string PhoneKey = nameof(PhoneKey);


    [AutoInject] private IWebAuthnService webAuthnService = default!;


    [Parameter, SupplyParameterFromQuery(Name = "return-url")]
    public string? ReturnUrlQueryString { get; set; }

    [Parameter] public bool IsWaiting { get; set; }

    [Parameter] public SignInRequestDto Model { get; set; } = default!;

    [Parameter] public EventCallback<string?> OnSocialSignIn { get; set; }

    [Parameter] public EventCallback OnPasswordlessSignIn { get; set; }

    [Parameter] public EventCallback OnSendOtp { get; set; }

    [Parameter] public EventCallback<SignInPanelTab> OnTabChange { get; set; }

    [Parameter] public SignInPanelType SignInPanelType { get; set; } // Check out SignInModalService for more details
    [Parameter] public EventCallback<SignInPanelType> SignInPanelTypeChanged { get; set; }


    private bool showWebAuthn;
    private string? selectedKey = EmailKey;
    private string ReturnUrl => ReturnUrlQueryString ?? NavigationManager.GetRelativePath() ?? Urls.HomePage;


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        showWebAuthn = await webAuthnService.IsWebAuthnAvailable() && (await webAuthnService.GetWebAuthnConfiguredUserIds()).Any();

        StateHasChanged();
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

    private async Task ChangeSignInPanelType(SignInPanelType type)
    {
        SignInPanelType = type;
        await SignInPanelTypeChanged.InvokeAsync(type);
    }
}
