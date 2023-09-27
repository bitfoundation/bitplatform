﻿namespace Bit.Websites.Platform.Web.Shared;

public partial class NotFoundComponent
{
    [AutoInject] public NavigationManager NavigationManager { get; set; }

    private void BackToHome()
    {
        NavigationManager.NavigateTo("/");
    }
}
