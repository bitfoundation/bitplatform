//+:cnd:noEmit
using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class NavMenu : IDisposable
{
    [AutoInject] IUserController userController = default!;

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
            //#if (offlineDb == true)
            new BitNavItem
            {
                Text = Localizer[nameof(AppStrings.OfflineEditProfileTitle)],
                IconName = BitIconName.EditContact,
                Url = "/offline-edit-profile",
            },
            //#endif
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

            await InvokeAsync(StateHasChanged);
        });

        // Automatically generated controllers employ the PrerenderStateService discreetly when the AppRenderMode.PrerenderEnabled is set to true.
        // PrerenderStateService efficiently stores information in a dictionary. Subsequently, upon the initialization of the client,
        // this stored data is employed to establish the initial state of the application, eliminating the need for redundant server requests.
        // The dictionary's keys correspond to the URLs of the respective requests.
        // As the GetCurrentUser method is invoked in both NavMenu.razor.cs and EditProfile.razor.cs, it becomes imperative to
        // incorporate a query string into the URL to differentiate between the two instances.
        // The nature of the query string can be arbitrary, but assigning it a meaningful value is advisable for clarity and coherence.
        userController.AddQueryString("origin", "NavMenu");
        user = await userController.GetCurrentUser(CurrentCancellationToken);

        var access_token = await PrerenderStateService.GetValue(AuthTokenProvider.GetAccessTokenAsync);
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

    public override void Dispose()
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
