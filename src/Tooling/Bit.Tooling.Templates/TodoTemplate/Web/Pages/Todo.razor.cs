using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.App.Pages;

public partial class Todo
{
    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    public bool IsLoading { get; set; } = false;
    TodoItemDto TodoItem = new();
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
        IsLoading = true;
        TodoItem.Date = DateTime.Now;
        await HttpClient.PostAsJsonAsync("TodoItem", TodoItem);
        TodoItem.Title = "";
        await GetTodoItems();
        IsLoading = false;
    }
}
