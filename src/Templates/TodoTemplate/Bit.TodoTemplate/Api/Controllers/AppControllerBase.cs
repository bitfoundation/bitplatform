namespace TodoTemplate.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected IOptionsSnapshot<AppSettings> _appSettings { get; set; } = default!;

    [AutoInject] protected IWebHostEnvironment _webHostEnvironment { get; set; } = default!;

    [AutoInject] protected IMapper _mapper { get; set; } = default!;

    [AutoInject] protected AppDbContext _dbContext { get; set; } = default!;
}
