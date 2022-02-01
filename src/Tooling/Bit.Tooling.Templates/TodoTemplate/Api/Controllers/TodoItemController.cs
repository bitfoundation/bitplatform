﻿using TodoTemplate.Api.Data.Models.TodoItem;
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
        public async Task<List<TodoItemDto>> Get()
        {
            return await _dbContext.todoItems
                .Select(t => new TodoItemDto
                {
                    Date = t.Date,
                    Id = t.Id,
                    IsDone = t.IsDone,
                    Title = t.Title
                }).ToListAsync();
        }

        [HttpPost]
        public async Task Post(TodoItemDto dto)
        {
            var todoItem = _mapper.Map<TodoItem>(dto);
            await _dbContext.todoItems.AddAsync(todoItem);
            await _dbContext.SaveChangesAsync();
        }
    }

}
