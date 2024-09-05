using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Layout.Main;

public partial class MainHeader : AppComponentBase
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private IPubSubService pubSubService = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;

    private UserDto user = new();
    private string? SelectedCulture;
    private string? profileImageUrl;
    private Action unsubscribe = default!;
    private BitDropdownItem<string>[] cultures = default!;

    protected override async Task OnInitAsync()
    {
        if (CultureInfoManager.MultilingualEnabled)
        {
            cultures = CultureInfoManager.SupportedCultures
                              .Select(sc => new BitDropdownItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })
                              .ToArray();

            SelectedCulture = CultureInfo.CurrentUICulture.Name;
        }

        unsubscribe = PubSubService.Subscribe(PubSubMessages.USER_DATA_UPDATED, async payload =>
        {
            if (payload is null) return;

            user = (UserDto)payload;

            await InvokeAsync(StateHasChanged);
        });

        user = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync("api/User/GetCurrentUser", AppJsonContext.Default.UserDto, CurrentCancellationToken)))!;

        var serverAddress = Configuration.GetServerAddress();
        var access_token = await PrerenderStateService.GetValue(() => AuthTokenProvider.GetAccessTokenAsync());
        profileImageUrl = $"{serverAddress}/api/Attachment/GetProfileImage?access_token={access_token}";

        await base.OnInitAsync();
    }

    private async Task OnCultureChanged()
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            await StorageService.SetItem("Culture", SelectedCulture, persistent: true);
            cultureInfoManager.SetCurrentCulture(SelectedCulture!);
            pubSubService.Publish(PubSubMessages.CULTURE_CHANGED, SelectedCulture);
        }
        else
        {
            await cookie.Set(new()
            {
                Name = ".AspNetCore.Culture",
                Value = Uri.EscapeDataString($"c={SelectedCulture}|uic={SelectedCulture}"),
                MaxAge = 30 * 24 * 3600,
                Secure = AppEnvironment.IsDev() is false
            });
        }

        NavigationManager.NavigateTo(NavigationManager.GetUriWithoutQueryParameter("culture"), forceLoad: true, replace: true);
    }

    private async Task ToggleTheme()
    {
        await bitDeviceCoordinator.ApplyTheme(await bitThemeManager.ToggleDarkLightAsync() == "dark");
    }

    private async Task GoToProfile()
    {
        //await CloseMenu();
        NavigationManager.NavigateTo(Urls.ProfilePage);
    }
}
