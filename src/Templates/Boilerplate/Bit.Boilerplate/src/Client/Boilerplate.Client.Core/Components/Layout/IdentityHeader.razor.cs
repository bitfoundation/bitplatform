﻿namespace Boilerplate.Client.Core.Components.Layout;

public partial class IdentityHeader : AppComponentBase, IDisposable
{
    private string? backLinkPayload;
    private BitDropdownItem<string>[] cultures = default!;
    private Action unsubscribeUpdateBackLink = default!;


    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private CultureService cultureService = default!;


    [CascadingParameter] private BitDir? currentDir { get; set; }
    [CascadingParameter(Name = Parameters.CurrentTheme)] private AppThemeType? currentTheme { get; set; }
    [CascadingParameter(Name = Parameters.IsCrossLayoutPage)] private bool? isCrossLayoutPage { get; set; }


    protected override async Task OnInitAsync()
    {
        unsubscribeUpdateBackLink = PubSubService.Subscribe(PubSubMessages.UPDATE_IDENTITY_HEADER_BACK_LINK, async payload =>
        {
            backLinkPayload = (string?)payload;

            await InvokeAsync(StateHasChanged);
        });

        if (CultureInfoManager.MultilingualEnabled)
        {
            cultures = CultureInfoManager.SupportedCultures
                        .Select(sc => new BitDropdownItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })
                        .ToArray();
        }

        await base.OnInitAsync();
    }


    private async Task HandleBackLinkClick()
    {
        PubSubService.Publish(PubSubMessages.IDENTITY_HEADER_BACK_LINK_CLICKED, backLinkPayload);
    }

    private async Task ToggleTheme()
    {
        await themeService.ToggleTheme();
    }

    private async Task OnCultureChanged(string? cultureName)
    {
        await cultureService.ChangeCulture(cultureName);
    }


    private int clickCount = 0;
    private async Task HandleTitleClick()
    {
        if (++clickCount == 7)
        {
            clickCount = 0;
            PubSubService.Publish(PubSubMessages.SHOW_DIAGNOSTIC_MODAL);
        }
    }


    public void Dispose()
    {
        unsubscribeUpdateBackLink?.Invoke();
    }
}
