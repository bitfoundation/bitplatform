using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class UserMenu
{
    private bool isOpen;
    private bool showCultures;
    private UserDto user = new();
    private string? profileImageUrl;
    private bool isSignOutConfirmOpen;
    private Action unsubscribeUerDataUpdated = default!;
    private BitChoiceGroupItem<string>[] cultures = default!;

    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private ThemeService themeService { get; set; } = default!;
    [AutoInject] private CultureService cultureService { get; set; } = default!;


    [CascadingParameter] private BitDir? currentDir { get; set; }
    [CascadingParameter(Name = Parameters.CurrentTheme)] private AppThemeType? currentTheme { get; set; }


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

            user = (UserDto)payload;

            await InvokeAsync(StateHasChanged);
        });

        user = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync("api/User/GetCurrentUser", JsonSerializerOptions.GetTypeInfo<UserDto>(), CurrentCancellationToken)))!;

        var accessToken = await PrerenderStateService.GetValue(AuthTokenProvider.GetAccessToken);
        profileImageUrl = new Uri(AbsoluteServerAddress, $"/api/Attachment/GetProfileImage?access_token={accessToken}").ToString();

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
