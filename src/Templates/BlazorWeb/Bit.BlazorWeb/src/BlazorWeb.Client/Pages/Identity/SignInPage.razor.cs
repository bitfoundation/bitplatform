﻿using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Client.Pages.Identity;

public partial class SignInPage
{
    private bool _isLoading;
    private string? _signInMessage;
    private BitMessageBarType _signInMessageType;
    private SignInRequestDto _signInModel = new();

    [Parameter]
    [SupplyParameterFromQuery]
    public string? RedirectUrl { get; set; }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (await AuthenticationStateProvider.IsUserAuthenticatedAsync())
        {
            NavigationManager.NavigateTo("/");
        }
    }

    private async Task DoSignIn()
    {
        if (_isLoading) return;

        _isLoading = true;
        _signInMessage = null;

        try
        {
            await AuthenticationService.SignIn(_signInModel);

            NavigationManager.NavigateTo(RedirectUrl ?? "/");
        }
        catch (KnownException e)
        {
            _signInMessageType = BitMessageBarType.Error;

            _signInMessage = e.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}

