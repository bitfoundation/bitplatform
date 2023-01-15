//-:cnd:noEmit
using AdminPanel.Shared.Dtos.Account;
using Microsoft.AspNetCore.Components.Routing;

namespace AdminPanel.Client.Shared;

public partial class Header : IDisposable
{
    private static List<BitBreadcrumbItem> _homeBreadcrumbItems = default!;
    private static List<BitBreadcrumbItem> _profileBreadcrumbItems = default!;
    private static List<BitBreadcrumbItem> _productsBreadcrumbItems = default!;
    private static List<BitBreadcrumbItem> _categoriesBreadcrumbItems = default!;
    private static List<BitBreadcrumbItem> _addCategoryBreadcrumbItems = default!;

    private bool _disposed;
    private UserDto _user = new();
    private string? _profileImageUrl;
    private string? _profileImageUrlBase;
    private bool _isUserAuthenticated;
    private bool _isHeaderDrpDownOpen;
    private bool _isSignOutModalOpen;
    private string _currentUrl = string.Empty;
    private List<BitBreadcrumbItem> _currentBreadcrumbItems = default!;

    private Action _unsubscribe = default!;

    [Parameter] public EventCallback OnToggleMenu { get; set; }

    protected override async Task OnInitAsync()
    {
        SetCurrentUrl();
        SetBreadcrumbItems();
        SetCurrentBreadcrumbItems();

        NavigationManager.LocationChanged += OnLocationChanged;

        AuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        _unsubscribe = PubSubService.Sub(PubSubMessages.PROFILE_UPDATED, payload =>
        {
            if (payload is null) return;

            _user = (UserDto)payload;

            _profileImageUrl = _profileImageUrlBase + _user.ProfileImageName;

            StateHasChanged();
        });

        _user = await StateService.GetValue($"{nameof(Header)}-User", async () => await HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto)) ?? new();

        _isUserAuthenticated = await StateService.GetValue($"{nameof(Header)}-IsUserAuthenticated", AuthenticationStateProvider.IsUserAuthenticatedAsync);

        var access_token = await StateService.GetValue($"{nameof(Header)}-access_token", () => AuthTokenProvider.GetAcccessToken());
        _profileImageUrlBase = $"{GetBaseUrl()}Attachment/GetProfileImage?access_token={access_token}&file=";
        _profileImageUrl = _profileImageUrlBase + _user.ProfileImageName;
    }

    private void SetBreadcrumbItems()
    {
        _homeBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.Home)],
                Href = "/",
            }
        };

        _profileBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.EditProfile)],
                Href = "/edit-profile",
            }
        };

        _productsBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.ProductCatologue)],
            },
            new()
            {
                Text = Localizer[nameof(AppStrings.Products)],
                Href = "/products",
            }
        };

        _categoriesBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.ProductCatologue)],
            },
            new()
            {
                Text = Localizer[nameof(AppStrings.Categories)],
                Href = "/categories",
            }
        };

        _addCategoryBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.ProductCatologue)],
            },
            new()
            {
                Text = Localizer[nameof(AppStrings.Categories)],
                Href = "/categories",
            },
            new()
            {
                Text = Localizer[nameof(AppStrings.AddEditCategory)],
            }
        };
    }

    private string GetBaseUrl()
    {
#if BlazorWebAssembly
        return "/api/";
#else
        return Configuration.GetValue<string>("ApiServerAddress") ?? string.Empty;
#endif
    }

    private async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            _isUserAuthenticated = await AuthenticationStateProvider.IsUserAuthenticatedAsync();
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

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetCurrentUrl();
        SetCurrentBreadcrumbItems();
        StateHasChanged();
    }

    private void SetCurrentUrl()
    {
        _currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);
    }

    private void SetCurrentBreadcrumbItems()
    {
        if (_currentUrl.Contains("/add-edit-category"))
        {
            _currentBreadcrumbItems = _addCategoryBreadcrumbItems;
        }
        else
        {
            _currentBreadcrumbItems = _currentUrl switch
            {
                "/" => _homeBreadcrumbItems,
                "/products" => _productsBreadcrumbItems,
                "/categories" => _categoriesBreadcrumbItems,
                "/edit-profile" => _profileBreadcrumbItems,
                _ => new(),
            };
        }
    }

    private async Task ToggleMenu()
    {
        await OnToggleMenu.InvokeAsync();
    }

    private async Task OpenSignOutModal()
    {
        ToggleHeaderDropDown();
        await JsRuntime.SetToggleBodyOverflow(true);
        _isSignOutModalOpen = true;
    }

    private void ToggleHeaderDropDown()
    {
        _isHeaderDrpDownOpen = !_isHeaderDrpDownOpen;
    }

    private void OpenEditProfilePage()
    {
        ToggleHeaderDropDown();
        NavigationManager.NavigateTo("/edit-profile");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        AuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;

        _unsubscribe?.Invoke();

        _disposed = true;
    }
}
