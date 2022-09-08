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

    private List<BitBreadcrumbItem> ProductsBreadcrumbItems { get; set; } = new();

    private List<BitBreadcrumbItem> CategoriesBreadcrumbItems { get; set; } = new();

    private List<BitBreadcrumbItem> AddCategoryBreadcrumbItems { get; set; } = new();

    private List<BitBreadcrumbItem> HomeBreadcrumbItems { get; set; } = new();

    private List<BitBreadcrumbItem> ProfileBreadcrumbItems { get; set; } = new();

    protected override async Task OnInitAsync()
    {
        SetBreadcrumbItems();
        SetCurrentUrl();
        SetBreadcrumbItem();

        NavigationManager.LocationChanged += OnLocationChanged;

        AuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        User = await StateService.GetValue($"{nameof(Header)}-{nameof(User)}", async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto));

        var access_token = await StateService.GetValue($"{nameof(Header)}-access_token", async () =>
            await AuthTokenProvider.GetAcccessToken());

        ProfileImageUrl = $"{GetBaseUrl()}Attachment/GetProfileImage?access_token={access_token}&file={User!.ProfileImageName}";

        AuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        IsUserAuthenticated = await StateService.GetValue($"{nameof(Header)}-{nameof(IsUserAuthenticated)}", AuthenticationStateProvider.IsUserAuthenticated);

        await base.OnInitAsync();
    }

    private void SetBreadcrumbItems()
    {
        ProductsBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.ProductCatologue)],
                Key = "Product catologue"
            },
            new()
            {
                Text = Localizer[nameof(AppStrings.Products)],
                Key = "Products",
                href = "/products",
                IsCurrentItem = true
            }
        };

        CategoriesBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.ProductCatologue)],
                Key = "Product catologue"
            },
            new()
            {
                            Text = Localizer[nameof(AppStrings.Categories)],
                Key = "Categories",
                href = "/categories",
                IsCurrentItem = true
            }
        };

        AddCategoryBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.ProductCatologue)],
                Key = "Product catologue"
            },
            new()
            {
                Text = Localizer[nameof(AppStrings.Categories)],
                Key = "Categories",
                href = "/categories",
            },
            new()
            {
                Text = Localizer[nameof(AppStrings.AddEditCategory)],
                Key = "Add/Edit Category",
                IsCurrentItem = true
            }
        };

        HomeBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
               Text = Localizer[nameof(AppStrings.Home)],
                Key = "Home",
                href = "/",
                IsCurrentItem = true
            }
        };

        ProfileBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.EditProfile)],
                Key = "Edit profile",
                href = "/edit-profile",
                IsCurrentItem = true
            }
        };
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
        await JsRuntime.SetToggleBodyOverflow(true);
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
