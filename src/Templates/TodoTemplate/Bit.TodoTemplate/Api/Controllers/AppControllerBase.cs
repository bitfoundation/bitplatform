namespace TodoTemplate.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected IOptionsSnapshot<AppSettings> AppSettings = default!;

    [AutoInject] protected IWebHostEnvironment WebHostEnvironment = default!;

    [AutoInject] protected IMapper Mapper = default!;

    [AutoInject] protected AppDbContext DbContext = default!;
}
