﻿using System.Runtime.InteropServices;

namespace TodoTemplate.App.Components;

public partial class Header : IAsyncDisposable
{
    [Parameter] public EventCallback OnToggleMenu { get; set; }

    public bool IsUserAuthenticated { get; set; }

    protected override async Task OnInitAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        IsUserAuthenticated = await StateService.GetValue($"{nameof(Header)}-{nameof(IsUserAuthenticated)}", AuthenticationStateProvider.IsUserAuthenticated);

        await base.OnInitAsync();
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

    private async Task ToggleMenu()
    {
        await OnToggleMenu.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

#if MultilingualEnabled
        if (firstRender)
        {
#if Maui
            SelectedCulture = App.GetPreferredCulture();
#else
            SelectedCulture = await JSRuntime.InvokeAsync<string>("window.App.getPreferredCulture", CultureInfoManager.GetCultureData());
#endif
            await InvokeAsync(StateHasChanged);
        }
#endif
    }

    string? SelectedCulture;

    async Task OnCultureChanged()
    {
        // Culture should set globally in Browser
        bool setCultureGlobally = RuntimeInformation.ProcessArchitecture == Architecture.Wasm;
#if Maui
        // Culture should set globally in Android, iOS, Windows
        setCultureGlobally = true;
#endif

        var culture = $"c={SelectedCulture}|uic={SelectedCulture}";

#if Maui
        Preferences.Set(".AspNetCore.Culture", culture);
#else
        await JSRuntime.InvokeVoidAsync("window.App.setCookie", ".AspNetCore.Culture", culture, 30 * 24 * 3600);
#endif

        NavigationManager.Reload();
    }

    List<BitDropDownItem> GetCultures()
    {
        return CultureInfoManager.SupportedCultures
            .Select(sc => new BitDropDownItem { Value = sc.code, Text = sc.name })
            .ToList();
    }
}
