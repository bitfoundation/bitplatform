using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.App.Components;

public partial class Header : IAsyncDisposable
{
    [AutoInject] private HttpClient httpClient = default!;

#if BlazorServer || BlazorHybrid
    [AutoInject] private IConfiguration configuration = default!;
#endif

    [AutoInject] private IAuthTokenProvider authTokenProvider = default!;

    [AutoInject] private IStateService stateService = default!;

    [AutoInject] private AppAuthenticationStateProvider authStateProvider = default!;

    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

    [Parameter] public EventCallback OnToggleMenu { get; set; }

    public UserDto? User { get; set; } = new();

    public string? ProfileImageUrl { get; set; }

    public bool IsUserAuthenticated { get; set; }

    public bool IsHeaderDrpDownOpen { get; set; }

    public bool IsSignOutModalOpen { get; set; }

    protected override async Task OnInitAsync()
    {
        User = await stateService.GetValue($"{nameof(NavMenu)}-{nameof(User)}", async () =>
            await httpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto));

        var access_token = await stateService.GetValue($"{nameof(NavMenu)}-access_token", async () =>
            await authTokenProvider.GetAcccessToken());

        ProfileImageUrl = $"{GetBaseUrl()}Attachment/GetProfileImage?access_token={access_token}&file={User!.ProfileImageName}";

        authStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        IsUserAuthenticated = await stateService.GetValue($"{nameof(Header)}-{nameof(IsUserAuthenticated)}", authStateProvider.IsUserAuthenticated);

        await base.OnInitAsync();
    }

    string GetBaseUrl()
    {
#if BlazorWebAssembly
        return "/api/";
#else
        return configuration.GetValue<string>("ApiServerAddress");
#endif
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            IsUserAuthenticated = await authStateProvider.IsUserAuthenticated();
        }
        catch (Exception ex)
        {
            exceptionHandler.Handle(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task ToggleMenu()
    {
        await OnToggleMenu.InvokeAsync();
    }

    private void OpenSignOutModal()
    {
        IsSignOutModalOpen = true;
        StateHasChanged();
    }

    private void ToggleHeaderDrpDown()
    {
        IsHeaderDrpDownOpen = !IsHeaderDrpDownOpen;
    }

    private void OpenEditProfilePage()
    {
        ToggleHeaderDrpDown();
        navigationManager.NavigateTo("/edit-profile");
    }

    public async ValueTask DisposeAsync()
    {
        authStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}
