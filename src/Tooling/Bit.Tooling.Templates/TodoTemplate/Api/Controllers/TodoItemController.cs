namespace TodoTemplate.Api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TodoItemController : ControllerBase
    {
        private readonly TodoTemplateDbContext _dbContext;

        private readonly IMapper _mapper;

        public TodoItemController(TodoTemplateDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }
    }

}
