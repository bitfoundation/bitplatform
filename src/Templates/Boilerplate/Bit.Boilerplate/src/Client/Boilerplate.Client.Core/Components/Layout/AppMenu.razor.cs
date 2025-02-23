using System.Threading.Tasks;
using Boilerplate.Client.Core.Services;
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

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
        if (CultureInfoManager.MultilingualEnabled)
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

        AuthManager.AuthenticationStateChanged += AuthenticationStateChanged;
        await GetCurrentUser(AuthenticationStateTask);

        await base.OnInitAsync();
    }


    private async void AuthenticationStateChanged(Task<AuthenticationState> task)
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
    }
}
