using TodoTemplate.Shared.Dtos.TodoItem;
namespace TodoTemplate.App.Pages;

public partial class TodoPage
{
    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    private bool IsLoading;
    private TodoItemDto TodoItem = new();
    private List<TodoItemDto>? TodoList = new();

    protected override async Task OnInitializedAsync()
    {
        await GetTodoItems();
    }
    private async Task GetTodoItems()
    {
        TodoList = await HttpClient.GetFromJsonAsync<List<TodoItemDto>>("TodoItem");
    }
    private async Task AddTodoItem()
    {
        IsLoading = true;
        TodoItem.Date = DateTime.Now;
        await HttpClient.PostAsJsonAsync("TodoItem", TodoItem);
        TodoItem.Title = "";
        await GetTodoItems();
        IsLoading = false;
    }
}
