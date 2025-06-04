using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppMenu
{
    private bool isOpen;
    private bool showCultures;
    private bool isSignOutConfirmOpen;
    private BitChoiceGroupItem<string>[] cultures = default!;

    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private CultureService cultureService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private SignInModalService signInModalService = default!;


    [CascadingParameter(Name = Parameters.CurrentDir)]
    private BitDir? CurrentDir { get; set; }

    [CascadingParameter(Name = Parameters.CurrentTheme)]
    private AppThemeType? CurrentTheme { get; set; }

    [CascadingParameter(Name = Parameters.CurrentUser)]
    UserDto? CurrentUser { get; set; }

    private string? ProfileImageUrl => CurrentUser?.GetProfileImageUrl(AbsoluteServerAddress);


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        NavigationManager.LocationChanged += NavigationManager_LocationChanged;

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            cultures = CultureInfoManager.SupportedCultures
                              .Select(sc => new BitChoiceGroupItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })
                              .ToArray();
        }
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // The sign-in and sign-up buttons href are bound to NavigationManager.GetRelativePath().
        // To ensure the bound values update with each route change, it's necessary to call StateHasChanged on location changes.
        StateHasChanged();
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

        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
    }

    private async Task ModalSignIn()
    {
        isOpen = false;
        await signInModalService.SignIn();
    }
}
