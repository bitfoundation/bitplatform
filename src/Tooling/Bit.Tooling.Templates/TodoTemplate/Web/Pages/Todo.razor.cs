using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.App.Pages;

public partial class Todo
{
    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    public bool IsBussy { get; set; } = false;
    TodoItemDto todoItemDto = new();
    List<TodoModel> todolist = new();
    protected override void OnInitialized()
    {
        for (int i = 0; i < 3; i++)
        {
            todolist.Add(new TodoModel
            {
                Date = "January 3, 2022, Monday",
                Title = "Project name"
            });
        }
    }
    public async Task Add_Todoitem()
    {
        IsBussy = true;
        todoItemDto.Date = DateTime.Now;
        await HttpClient.PostAsJsonAsync("TodoItem", todoItemDto);
        todoItemDto.Title = "";
        IsBussy = false;
    }
}
public class TodoModel
{
    public string? Title { get; set; }
    public string? Date { get; set; }
}
