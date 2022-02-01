using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.App.Pages;

public partial class Todo
{
    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    public bool IsBussy { get; set; } = false;
    TodoItemDto todoItem = new();
    List<TodoItemDto>? TodoList = new();

    protected override async Task OnInitializedAsync()
    {
        await GetTodoItems();
    }
    public async Task GetTodoItems()
    {
        var response = await HttpClient.GetAsync("TodoItem");
        TodoList = await response.Content.ReadFromJsonAsync<List<TodoItemDto>>();
    }
    public async Task AddTodoItem()
    {
        IsBussy = true;
        todoItem.Date = DateTime.Now;
        await HttpClient.PostAsJsonAsync("TodoItem", todoItem);
        todoItem.Title = "";
        await GetTodoItems();
        IsBussy = false;
    }
}
