using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class UserMenu
{
    private bool isOpen;
    private bool showCultures;
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

        user = await userController.GetCurrentUser(CurrentCancellationToken);

        await base.OnInitAsync();
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
