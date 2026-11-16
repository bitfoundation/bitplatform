namespace Boilerplate.Client.Core.Components.Pages.Settings.Account;

public partial class AccountSection
{
    [CascadingParameter] public UserDto? CurrentUser { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "emailToken")]
    public string? EmailTokenQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneToken")]
    public string? PhoneNumberTokenQueryString { get; set; }

    private bool isPinSupported;

    private string? DefaultSelectedTab => string.IsNullOrWhiteSpace(EmailTokenQueryString) is false ? nameof(AppStrings.Email)
                                        : string.IsNullOrWhiteSpace(PhoneNumberTokenQueryString) is false ? nameof(AppStrings.Phone)
                                        : null;

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        isPinSupported = await AuthManager.IsPinSupported();

        StateHasChanged();
    }
}
