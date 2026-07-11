//+:cnd:noEmit
//#if (multitenancy == true)
using Boilerplate.Server.Api.Features.Identity.Services;
//#endif

namespace Boilerplate.Server.Api.Infrastructure.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected ServerApiSettings AppSettings = default!;

    [AutoInject] protected AppDbContext DbContext = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;

    [AutoInject] protected TimeProvider TimeProvider = default!;

    //#if (multitenancy == true)
    [AutoInject] protected TenantProvider TenantProvider = default!;
    //#endif
}
