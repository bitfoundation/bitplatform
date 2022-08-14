using AdminPanel.Shared.Dtos.Account;
using Microsoft.AspNetCore.Components.Routing;

namespace AdminPanel.App.Components;

public partial class Header : IAsyncDisposable
{
    [Parameter] public EventCallback OnToggleMenu { get; set; }

    public UserDto? User { get; set; } = new();

    public string? ProfileImageUrl { get; set; }

    public bool IsUserAuthenticated { get; set; }

    public bool IsHeaderDrpDownOpen { get; set; }

    public bool IsSignOutModalOpen { get; set; }

    public string CurrentUrl { get; set; } = string.Empty;

    public List<BitBreadcrumbItem> BreadcrumbItems { get; set; } = new List<BitBreadcrumbItem>();

    private List<BitBreadcrumbItem> ProductsBreadcrumbItems = new List<BitBreadcrumbItem>
    {
        new()
        {
            Text = "Product catologue",
            Key = "Product catologue"
        },
        new()
        {
            Text = "Products",
            Key = "Products",
            href = "/products",
            IsCurrentItem = true
        }
    };

    private List<BitBreadcrumbItem> CategoriesBreadcrumbItems = new List<BitBreadcrumbItem>
    {
        new()
        {
            Text = "Product catologue",
            Key = "Product catologue"
        },
        new()
        {
            Text = "Categories",
            Key = "Categories",
            href = "/categories",
            IsCurrentItem = true
        }
    };

    private List<BitBreadcrumbItem> AddCategoryBreadcrumbItems = new List<BitBreadcrumbItem>
    {
        new()
        {
            Text = "Product catologue",
            Key = "Product catologue"
        },
        new()
        {
            Text = "Categories",
            Key = "Categories",
            href = "/categories",
        },
        new()
        {
            Text = "Add/Edit Category",
            Key = "Add/Edit Category",
            IsCurrentItem = true
        }
    };

    private List<BitBreadcrumbItem> HomeBreadcrumbItems = new List<BitBreadcrumbItem>
    {
        new()
        {
            Text = "Home",
            Key = "Home",
            href = "/",
            IsCurrentItem = true
        }
    };

    private List<BitBreadcrumbItem> ProfileBreadcrumbItems = new List<BitBreadcrumbItem>
    {
        new()
        {
            Text = "Edit profile",
            Key = "Edit profile",
            href = "/edit-profile",
            IsCurrentItem = true
        }
    };

    protected override async Task OnInitAsync()
    {
        try
        {
            SetCurrentUrl();
            SetBreadcrumbItem();
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            NavigationManager.LocationChanged += OnLocationChanged;
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

            base.OnInitialized();
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }

        User = await StateService.GetValue($"{nameof(NavMenu)}-{nameof(User)}", async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto));

        var access_token = await StateService.GetValue($"{nameof(NavMenu)}-access_token", async () =>
            await AuthTokenProvider.GetAcccessToken());

        ProfileImageUrl = $"{GetBaseUrl()}Attachment/GetProfileImage?access_token={access_token}&file={User!.ProfileImageName}";

        AuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        IsUserAuthenticated = await StateService.GetValue($"{nameof(Header)}-{nameof(IsUserAuthenticated)}", AuthenticationStateProvider.IsUserAuthenticated);

        await base.OnInitAsync();
    }

    string GetBaseUrl()
    {
#if BlazorWebAssembly
        return "/api/";
#else
        return Configuration.GetValue<string>("ApiServerAddress");
#endif
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            IsUserAuthenticated = await AuthenticationStateProvider.IsUserAuthenticated();
        }
        catch (Exception ex)
        {
            ExceptionHandler.Handle(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }
    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        SetCurrentUrl();
        SetBreadcrumbItem();
        Task.Delay(300);
        StateHasChanged();
    }

    private void SetCurrentUrl()
    {
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);
    }

    private void SetBreadcrumbItem()
    {
        if (CurrentUrl.Contains("/add-edit-category"))
        {
            BreadcrumbItems = AddCategoryBreadcrumbItems;
        }
        else
        {
            switch (CurrentUrl)
            {
                case "/":
                    BreadcrumbItems = HomeBreadcrumbItems;
                    break;
                case "/products":
                    BreadcrumbItems = ProductsBreadcrumbItems;
                    break;
                case "/categories":
                    BreadcrumbItems = CategoriesBreadcrumbItems;
                    break;
                case "/edit-profile":
                    BreadcrumbItems = ProfileBreadcrumbItems;
                    break;
                default:
                    BreadcrumbItems = new List<BitBreadcrumbItem>();
                    break;
            }
        }
    }

    private async Task ToggleMenu()
    {
        await OnToggleMenu.InvokeAsync();
    }

    private async Task OpenSignOutModal()
    {
        ToggleHeaderDrpDown();
        await JavaScriptRuntime.SetToggleBodyOverflow(true);
        IsSignOutModalOpen = true;
    }

    private void ToggleHeaderDrpDown()
    {
        IsHeaderDrpDownOpen = !IsHeaderDrpDownOpen;
    }

    private void OpenEditProfilePage()
    {
        ToggleHeaderDrpDown();
        NavigationManager.NavigateTo("/edit-profile");
    }

    public async ValueTask DisposeAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}
