﻿namespace Boilerplate.Client.Maui.Services;

public partial class MauiExternalNavigationService : IExternalNavigationService
{
    public async Task NavigateToAsync(string url)
    {
        await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
    }
}
