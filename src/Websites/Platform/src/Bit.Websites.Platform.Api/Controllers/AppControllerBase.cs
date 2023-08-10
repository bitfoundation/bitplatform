namespace Bit.Websites.Platform.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected AppSettings AppSettings = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
}
