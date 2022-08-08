namespace TodoTemplate.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected IOptionsSnapshot<AppSettings> appSettings = default!;

    [AutoInject] protected IWebHostEnvironment webHostEnvironment = default!;

    [AutoInject] protected IMapper mapper = default!;

    [AutoInject] protected AppDbContext dbContext = default!;
}
