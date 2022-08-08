namespace TodoTemplate.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] public IOptionsSnapshot<AppSettings> AppSettings { get; set; } = default!;

    [AutoInject] public IWebHostEnvironment WebHostEnvironment { get; set; } = default!;

    [AutoInject] public IMapper Mapper { get; set; } = default!;

    [AutoInject] public AppDbContext DbContext { get; set; } = default!;
}
