using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppMenu
{
    private bool isOpen;
    private bool showCultures;
    private bool isAuthenticated;
    private UserDto user = new();
    private bool isSignOutConfirmOpen;
    private Action unsubscribeUerDataUpdated = default!;
    private BitChoiceGroupItem<string>[] cultures = default!;

    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private CultureService cultureService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;


    [CascadingParameter] private BitDir? currentDir { get; set; }
    [CascadingParameter(Name = Parameters.CurrentTheme)] private AppThemeType? currentTheme { get; set; }


    private string? ProfileImageUrl => user.GetProfileImageUrl(AbsoluteServerAddress);


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        AuthManager.AuthenticationStateChanged += AuthManager_AuthenticationStateChanged;
        NavigationManager.LocationChanged += NavigationManager_LocationChanged;

        if (CultureInfoManager.EnglishUSOnly is false)
        {
            cultures = CultureInfoManager.SupportedCultures
                              .Select(sc => new BitChoiceGroupItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })
                              .ToArray();
        }

        unsubscribeUerDataUpdated = PubSubService.Subscribe(ClientPubSubMessages.PROFILE_UPDATED, async payload =>
        {
            if (payload is null) return;

            user = payload is JsonElement jsonDocument
                ? jsonDocument.Deserialize(JsonSerializerOptions.GetTypeInfo<UserDto>())! // PROFILE_UPDATED can be invoked from server through SignalR
                : (UserDto)payload;

            await InvokeAsync(StateHasChanged);
        });

        await GetCurrentUser(AuthenticationStateTask);
    }


    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // The sign-in and sign-up buttons href are bound to NavigationManager.GetRelativePath().
        // To ensure the bound values update with each route change, it's necessary to call StateHasChanged on location changes.
        StateHasChanged();
    }

    private async void AuthManager_AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            await GetCurrentUser(task);
        }
        catch (Exception ex)
        {
            ExceptionHandler.Handle(ex);
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task GetCurrentUser(Task<AuthenticationState> task)
    {
        isAuthenticated = (await task).User.IsAuthenticated();
        if (isAuthenticated)
        {
            user = await userController.GetCurrentUser(CurrentCancellationToken);
        }
    }

    private async Task OnCultureChanged(string? cultureName)
    {
        await cultureService.ChangeCulture(cultureName);
    }

    private async Task ToggleTheme()
    {
        await themeService.ToggleTheme();
    }

    private async Task GoToProfile()
    {
        NavigationManager.NavigateTo(Urls.SettingsPage);
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        unsubscribeUerDataUpdated?.Invoke();
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
        AuthManager.AuthenticationStateChanged -= AuthManager_AuthenticationStateChanged;
    }
}
