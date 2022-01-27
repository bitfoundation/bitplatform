namespace TodoTemplate.Api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private readonly TodoTemplateDbContext _dbContext;

        private readonly IMapper _mapper;

        public TodoController(TodoTemplateDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }
    }

}
