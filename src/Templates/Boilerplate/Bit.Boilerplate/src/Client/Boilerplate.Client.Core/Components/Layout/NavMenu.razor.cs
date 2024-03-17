//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class NavMenu
{
    private bool disposed;
    private bool isSignOutModalOpen;
    private string? profileImageUrl;
    private string? profileImageUrlBase;
    private UserDto user = new();
    private List<BitNavItem> navItems = [];
    private Action unsubscribe = default!;

    [AutoInject] private NavigationManager navManager = default!;

    [Parameter] public bool IsMenuOpen { get; set; }

    [Parameter] public EventCallback<bool> IsMenuOpenChanged { get; set; }

    protected override async Task OnInitAsync()
    {
        navItems =
        [
            new()
            {
                Text = Localizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = "/",
            },
            //#if (sample == "Admin")
            new()
            {
                Text = Localizer[nameof(AppStrings.ProductCategory)],
                IconName = BitIconName.Product,
                IsExpanded = true,
                ChildItems =
                [
                    new() {
                        Text = Localizer[nameof(AppStrings.Dashboard)],
                        Url = "/dashboard",
                    },
                    new() {
                        Text = Localizer[nameof(AppStrings.Products)],
                        Url = "/products",
                    },
                    new() {
                        Text = Localizer[nameof(AppStrings.Categories)],
                        Url = "/categories",
                    },
                ]
            },
            //#elif (sample == "Todo")
            new()
            {
                Text = Localizer[nameof(AppStrings.TodoTitle)],
                IconName = BitIconName.ToDoLogoOutline,
                Url = "/todo",
            },
            //#endif
            new()
            {
                Text = Localizer[nameof(AppStrings.EditProfileTitle)],
                IconName = BitIconName.EditContact,
                Url = "/edit-profile",
            },
            //#if (offlineDb == true)
            new()
            {
                Text = Localizer[nameof(AppStrings.OfflineEditProfileTitle)],
                IconName = BitIconName.EditContact,
                Url = "/offline-edit-profile",
            },
            //#endif
            new()
            {
                Text = Localizer[nameof(AppStrings.TermsTitle)],
                IconName = BitIconName.EntityExtraction,
                Url = "/terms",
            }
        ];

        if (AppRenderMode.IsBlazorHybrid)
        {
            // Presently, the About page is absent from the Client/Core project, rendering it inaccessible on the web platform.
            // In order to exhibit a sample page that grants direct access to native functionalities without dependence on dependency injection (DI) or publish-subscribe patterns,
            // about page is integrated within Blazor hybrid projects like Client/Maui.

            navItems.Add(new()
            {
                Text = Localizer[nameof(AppStrings.AboutTitle)],
                IconName = BitIconName.Help,
                Url = "/about",
            });
        }

        unsubscribe = PubSubService.Subscribe(PubSubMessages.PROFILE_UPDATED, async payload =>
        {
            if (payload is null) return;

            user = (UserDto)payload;

            SetProfileImageUrl();

            await InvokeAsync(StateHasChanged);
        });

        user = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync("api/User/GetCurrentUser", AppJsonContext.Default.UserDto, CurrentCancellationToken)))!;

        var access_token = await PrerenderStateService.GetValue(() => AuthTokenProvider.GetAccessTokenAsync());
        profileImageUrlBase = $"{Configuration.GetApiServerAddress()}api/Attachment/GetProfileImage?access_token={access_token}&file=";

        SetProfileImageUrl();
    }

    private void SetProfileImageUrl()
    {
        profileImageUrl = user.ProfileImageName is not null ? profileImageUrlBase + user.ProfileImageName : null;
    }

    private async Task DoSignOut()
    {
        isSignOutModalOpen = true;

        await CloseMenu();
    }

    private async Task GoToEditProfile()
    {
        await CloseMenu();
        navManager.NavigateTo("edit-profile");
    }

    private async Task HandleNavItemClick(BitNavItem item)
    {
        if (string.IsNullOrEmpty(item.Url)) return;

        await CloseMenu();
    }

    private async Task CloseMenu()
    {
        IsMenuOpen = false;
        await IsMenuOpenChanged.InvokeAsync(false);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        if (disposed || disposing is false) return;

        unsubscribe?.Invoke();

        disposed = true;
    }
}
