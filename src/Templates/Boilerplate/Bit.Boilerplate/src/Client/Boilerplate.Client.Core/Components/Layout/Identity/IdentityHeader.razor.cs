namespace Boilerplate.Client.Core.Components.Layout.Identity;

public partial class IdentityHeader : AppComponentBase, IDisposable
{
    private string? backLinkPayload;
    private BitDropdownItem<string>[] cultures = default!;
    private Action unsubscribeUpdateBackLink = default!;


    [AutoInject] private IThemeService themeService = default!;
    [AutoInject] private ICultureService cultureService = default!;


    [CascadingParameter] private BitDir? currentDir { get; set; }
    [CascadingParameter(Name = Parameters.CurrentUrl)] private string? currentUrl { get; set; }
    [CascadingParameter(Name = Parameters.CurrentTheme)] private AppThemeType? currentTheme { get; set; }
    [CascadingParameter(Name = Parameters.IsAnonymousPage)] private bool? isAnonymousPage { get; set; }


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


    public void Dispose()
    {
        unsubscribeUpdateBackLink();
    }
}
