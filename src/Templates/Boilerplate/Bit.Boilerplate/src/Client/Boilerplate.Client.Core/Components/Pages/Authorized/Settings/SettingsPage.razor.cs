//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class SettingsPage
{
    protected override string? Title => Localizer[nameof(AppStrings.Settings)];
    protected override string? Subtitle => string.Empty;


    private bool showPasswordless;


    [Parameter] public string? Section { get; set; }


    [AutoInject] protected HttpClient HttpClient = default!;
    [AutoInject] private IUserController userController = default!;


    private UserDto? user;
    private bool isLoading;
    private string? openedAccordion;
    private string? accountSelectedPivot;


    protected override async Task OnInitAsync()
    {
        openedAccordion = Section?.ToLower();

        isLoading = true;

        try
        {
            user = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync("api/User/GetCurrentUser", JsonSerializerOptions.GetTypeInfo<UserDto>(), CurrentCancellationToken)))!;
            await CheckShowPasswordless();
        }
        finally
        {
            isLoading = false;
        }

        await base.OnInitAsync();
    }


    private async Task HandleOnCredentialCreated()
    {
        await CheckShowPasswordless();

        if(showPasswordless is false)
        {
            accountSelectedPivot = nameof(AppStrings.Email);
            StateHasChanged();
        }
    }

    private async Task CheckShowPasswordless()
    {
        if (user?.UserName is null) return;

        var isAvailable = await JSRuntime.IsWebAuthnAvailable();
        var isConfigured = await JSRuntime.IsWebAuthnConfigured(user.UserName);

        showPasswordless = isAvailable && isConfigured is false;
    }
}
