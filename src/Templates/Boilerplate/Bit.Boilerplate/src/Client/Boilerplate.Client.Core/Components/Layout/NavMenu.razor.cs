//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class NavMenu : IDisposable
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
            new BitNavItem
            {
                Text = Localizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = "/",
            },
            //#if (sample == "Admin")
            new BitNavItem
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
            new BitNavItem
            {
                Text = Localizer[nameof(AppStrings.TodoTitle)],
                IconName = BitIconName.ToDoLogoOutline,
                Url = "/todo",
            },
            //#endif
            new BitNavItem
            {
                Text = Localizer[nameof(AppStrings.EditProfileTitle)],
                IconName = BitIconName.EditContact,
                Url = "/edit-profile",
            },
            new BitNavItem
            {
                Text = Localizer[nameof(AppStrings.TermsTitle)],
                IconName = BitIconName.EntityExtraction,
                Url = "/terms",
            }
        ];

        unsubscribe = PubSubService.Subscribe(PubSubMessages.PROFILE_UPDATED, async payload =>
        {
            if (payload is null) return;

            user = (UserDto)payload;

            SetProfileImageUrl();

            StateHasChanged();
        });

        user = await PrerenderStateService.GetValue($"{nameof(NavMenu)}-{nameof(user)}", async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto)) ?? new();

        var access_token = await PrerenderStateService.GetValue($"{nameof(NavMenu)}-access_token", AuthTokenProvider.GetAccessTokenAsync);
        profileImageUrlBase = $"{Configuration.GetApiServerAddress()}Attachment/GetProfileImage?access_token={access_token}&file=";

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

    private async Task CloseMenu()
    {
        IsMenuOpen = false;
        await IsMenuOpenChanged.InvokeAsync(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed || disposing is false) return;

        unsubscribe?.Invoke();

        disposed = true;
    }
}
