namespace AdminPanel.Server.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected ServerApiSettings AppSettings = default!;

    [AutoInject] protected IConfiguration Configuration = default!;

    [AutoInject] protected AppDbContext DbContext = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
}
