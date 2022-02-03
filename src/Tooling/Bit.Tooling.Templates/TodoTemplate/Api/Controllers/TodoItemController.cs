using TodoTemplate.Api.Data.Models.TodoItem;
using TodoTemplate.Shared.Dtos.TodoItem;

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

        [HttpGet]
        public IQueryable<TodoItemDto> Get()
        {
            return _dbContext.todoItems.ProjectTo<TodoItemDto>(_mapper.ConfigurationProvider);
        }

        [HttpPost]
        public async Task Post(TodoItemDto dto)
        {
            var todoItem = _mapper.Map<TodoItem>(dto);
            await _dbContext.todoItems.AddAsync(todoItem);
            await _dbContext.SaveChangesAsync();
        }

        [HttpPut]
        public async Task Update(TodoItemDto dto, CancellationToken cancellationToken)
        {
            var itemToUpdate = await _dbContext.todoItems.FirstOrDefaultAsync(item => item.Id == dto.Id, cancellationToken);
            if (itemToUpdate is not null)
            {
                var updatedItem = _mapper.Map(dto, itemToUpdate);
                _dbContext.todoItems.Update(updatedItem);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }      
        }

        [HttpDelete("{id:int}")]
        public async Task Delete(int id)    
        {
            _dbContext.todoItems.Remove(new TodoItem { Id = id });
            await _dbContext.SaveChangesAsync();
        }
    }

}
