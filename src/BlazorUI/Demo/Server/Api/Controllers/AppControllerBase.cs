namespace Bit.BlazorUI.Demo.Server.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected AppSettings AppSettings = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
}
