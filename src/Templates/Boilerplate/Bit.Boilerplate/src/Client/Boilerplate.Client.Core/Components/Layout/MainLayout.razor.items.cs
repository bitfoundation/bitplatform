using Microsoft.AspNetCore.Authorization;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout
{
    [AutoInject] protected IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] protected IAuthorizationService authorizationService = default!;

    private async Task<List<BitNavItem>> GetNavPanelItems()
    {
        List<BitNavItem> items =
        [
        ];

        return items;
    }
}
