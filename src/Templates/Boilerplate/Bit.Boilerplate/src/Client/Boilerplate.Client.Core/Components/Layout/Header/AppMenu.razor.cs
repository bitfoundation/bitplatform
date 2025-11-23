using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Layout.Header;

public partial class AppMenu
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }

    [CascadingParameter] public UserDto? CurrentUser { get; set; }

    [CascadingParameter] public AppThemeType? CurrentTheme { get; set; }


    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private CultureService cultureService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private SignInModalService signInModalService = default!;


    private bool isOpen;
    private bool showCultures;
    private bool isSignOutConfirmOpen;
    private BitChoiceGroupItem<string>[] cultures = default!;


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
        NavigationManager.NavigateTo(PageUrls.Settings);
    }


    protected override async ValueTask DisposeAsyncCore()
    {
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;

        await base.DisposeAsyncCore();
    }

    private async Task ModalSignIn()
    {
        isOpen = false;
        await signInModalService.SignIn();
    }
}
