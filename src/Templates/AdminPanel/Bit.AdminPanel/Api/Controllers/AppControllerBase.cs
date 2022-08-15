using Microsoft.Extensions.Localization;

namespace AdminPanel.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected IOptionsSnapshot<AppSettings> AppSettings = default!;

    [AutoInject] protected IMapper Mapper = default!;

    [AutoInject] protected AppDbContext DbContext = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
}
