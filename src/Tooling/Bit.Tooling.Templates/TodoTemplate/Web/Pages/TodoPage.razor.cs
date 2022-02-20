using TodoTemplate.Shared.Dtos.TodoItem;
namespace TodoTemplate.App.Pages;

public partial class TodoPage
{
    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    [Inject]
    public IStateService StateService { get; set; } = default!;

    public bool IsLoading { get; set; }
    public bool IsAddLoading { get; set; }
    public string? SelectedSortTodoItem { get; set; }
    public string? SearchTextTodoItem { get; set; }
    public string NewTodoItemTitle { get; set; } = string.Empty;
    public List<TodoItemDto>? TodoItemList { get; set; } = new();
    public List<TodoItemDto>? FilteredAllTodoItemList { get; set; } = new();
    public List<TodoItemDto>? FilteredCompletedTodoItemList { get; set; } = new();
    public List<TodoItemDto>? FilteredActiveTodoItemList { get; set; } = new();

    public List<BitDropDownItem> SortItemList = new()
    {
        new BitDropDownItem
        {
            Text = "Alphabetical",
        },
        new BitDropDownItem
        {
            Text = "Date",
        }
    };

    protected async override Task OnInitAsync()
    {
        await LoadTodoItems();

        await base.OnInitAsync();
    }

    private async Task LoadTodoItems()
    {
        IsLoading = true;
        try
        {
            TodoItemList = await StateService.GetValue(nameof(TodoItemList), async () => await HttpClient.GetFromJsonAsync("TodoItem", ToDoTemplateJsonContext.Default.ListTodoItemDto));
            GenarateTodoItemList();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void GenarateTodoItemList()
    {

        FilteredActiveTodoItemList = TodoItemList?.Where(td => td.IsDone is false).ToList();
        FilteredCompletedTodoItemList = TodoItemList?.Where(td => td.IsDone is true).ToList();
        FilteredAllTodoItemList = TodoItemList;

        if (SearchTextTodoItem != null)
        {
            HandlerTodoItemSearch(SearchTextTodoItem);
        }
        if (SelectedSortTodoItem != null)
        {
            HandlerTodoItemSort();
        }
    }

    private async Task ToggleTodoItem(TodoItemDto todoItem)
    {
        todoItem.IsDone = !todoItem.IsDone;
        await EditTodoItem(todoItem);
        GenarateTodoItemList();
    }

    private void HandlerTodoItemSearch(string searchStr)
    {
        FilteredActiveTodoItemList = FilteredActiveTodoItemList?.Where(td => td.IsDone is false).Where(td => td.Title.Contains(searchStr)).ToList();
        FilteredCompletedTodoItemList = FilteredCompletedTodoItemList?.Where(td => td.IsDone is true).Where(td => td.Title.Contains(searchStr)).ToList();
        FilteredAllTodoItemList = FilteredAllTodoItemList?.Where(td => td.Title.Contains(searchStr)).ToList();
    }

    private void TodoItemSearch(string searchStr)
    {
        SearchTextTodoItem = searchStr;
        GenarateTodoItemList();
    }

    private void HandlerTodoItemSort()
    {
        if (SelectedSortTodoItem == "Alphabetical")
        {
            FilteredActiveTodoItemList = FilteredActiveTodoItemList?.Where(td => td.IsDone is false).OrderBy(td => td.Title).ToList();
            FilteredCompletedTodoItemList = FilteredCompletedTodoItemList?.Where(td => td.IsDone is true).OrderBy(td => td.Title).ToList();
            FilteredAllTodoItemList = FilteredAllTodoItemList?.OrderBy(td => td.Title).ToList();
        }
        else
        {
            FilteredActiveTodoItemList = FilteredActiveTodoItemList?.Where(td => td.IsDone is false).OrderBy(td => td.Date).ToList();
            FilteredCompletedTodoItemList = FilteredCompletedTodoItemList?.Where(td => td.IsDone is true).OrderBy(td => td.Date).ToList();
            FilteredAllTodoItemList = FilteredAllTodoItemList?.OrderBy(td => td.Date).ToList();
        }
    }

    private void TodoItemSort()
    {
        SelectedSortTodoItem = SortItemList.Where(s => s.IsSelected is true).Single().Text;
        GenarateTodoItemList();
    }

    private async Task AddTodoItem()
    {
        IsAddLoading = true;
        try
        {
            var newTodoItem = new TodoItemDto
            {
                Title = NewTodoItemTitle,
                Date = DateTime.Now,
            };

            await HttpClient.PostAsJsonAsync("TodoItem", newTodoItem, ToDoTemplateJsonContext.Default.TodoItemDto);

            await LoadTodoItems();

            NewTodoItemTitle = "";
        }
        finally
        {
            IsAddLoading = false;
        }
    }

    private async Task DeleteTodoItem(TodoItemDto todoItem)
    {
        await HttpClient.DeleteAsync($"TodoItem/{todoItem.Id}");
        TodoItemList?.Remove(todoItem);
        GenarateTodoItemList();
    }

    private async Task EditTodoItem(TodoItemDto todoItem)
    {
        if (string.IsNullOrWhiteSpace(todoItem.Title))
            return;

        todoItem.IsInEditMode = false;

        await HttpClient.PutAsJsonAsync("TodoItem", todoItem, ToDoTemplateJsonContext.Default.TodoItemDto);
        GenarateTodoItemList();
    }
}
