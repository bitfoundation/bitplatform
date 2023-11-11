﻿//-:cnd:noEmit
using AdminPanel.Shared.Dtos.Identity;
using Microsoft.AspNetCore.Components.Routing;

namespace AdminPanel.Client.Core.Shared;

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

    [AutoInject] private BitThemeManager _bitThemeManager { get; set; } = default!;
    [AutoInject] private IBitDeviceCoordinator _bitDeviceCoordinator { get; set; } = default!;


    [Parameter] public EventCallback OnToggleMenu { get; set; }

    protected override async Task OnInitAsync()
    {
        SetCurrentUrl();
        SetBreadcrumbItems();
        SetCurrentBreadcrumbItems();

        NavigationManager.LocationChanged += OnLocationChanged;

        AuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        _unsubscribe = PubSubService.Subscribe(PubSubMessages.PROFILE_UPDATED, payload =>
        {
            if (payload is null) return;

            _user = (UserDto)payload;

            SetProfileImageUrl();

            StateHasChanged();
        });

        _user = await PrerenderStateService.GetValue($"{nameof(Header)}-User", async () => await HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto)) ?? new();

        _isUserAuthenticated = await PrerenderStateService.GetValue($"{nameof(Header)}-IsUserAuthenticated", AuthenticationStateProvider.IsUserAuthenticatedAsync);

        var access_token = await PrerenderStateService.GetValue($"{nameof(Header)}-access_token", AuthTokenProvider.GetAccessTokenAsync);
        _profileImageUrlBase = $"{Configuration.GetApiServerAddress()}Attachment/GetProfileImage?access_token={access_token}&file=";

        SetProfileImageUrl();
    }

    private void SetProfileImageUrl()
    {
        _profileImageUrl = _user.ProfileImageName is not null ? _profileImageUrlBase + _user.ProfileImageName : null;
    }

    private void SetBreadcrumbItems()
    {
        _homeBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.Home)],
                Href = "/",
                IsSelected = true
            }
        };

        _profileBreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = Localizer[nameof(AppStrings.EditProfile)],
                Href = "/edit-profile",
                IsSelected = true
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
                IsSelected = true
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
                IsSelected = true
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
                IsSelected = true
            }
        };
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
        ToggleHeaderDropdown();
        await JsRuntime.SetBodyOverflow(true);
        _isSignOutModalOpen = true;
    }

    private void ToggleHeaderDropdown()
    {
        _isHeaderDrpDownOpen = !_isHeaderDrpDownOpen;
    }

    private void OpenEditProfilePage()
    {
        ToggleHeaderDropdown();
        NavigationManager.NavigateTo("/edit-profile");
    }

    private async Task ToggleTheme()
    {
        await _bitDeviceCoordinator.ApplyTheme(await _bitThemeManager.ToggleDarkLightAsync() == "dark");
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
