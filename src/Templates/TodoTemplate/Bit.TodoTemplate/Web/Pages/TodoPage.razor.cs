using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.App.Pages;

public partial class TodoPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    public bool IsLoading { get; set; }
    public string SelectedPivotName { get; set; } = TodoStrings.ResourceManager.Translate(TodoStrings.All);
    public string? EditModeTodoItemText { get; set; }
    public bool IsAddLoading { get; set; }
    public string? SelectedSortTodoItemName { get; set; }
    public string? SearchTextTodoItem { get; set; }
    public string NewTodoItemTitle { get; set; } = string.Empty;
    public List<TodoItemDto>? AllTodoItemList { get; set; } = new();
    public List<TodoItemDto>? ViewTodoItemList { get; set; } = new();

    public List<BitDropDownItem> SortItemList = new()
    {
        new BitDropDownItem
        {
            Text = TodoStrings.ResourceManager.Translate(TodoStrings.Alphabetical),
        },
        new BitDropDownItem
        {
            Text = TodoStrings.ResourceManager.Translate(TodoStrings.Date),
        }
    };

    protected override async Task OnInitAsync()
    {
        await LoadTodoItems();

        await base.OnInitAsync();
    }

    private async Task LoadTodoItems()
    {
        IsLoading = true;
        try
        {
            AllTodoItemList = await stateService.GetValue($"{nameof(TodoPage)}-{nameof(AllTodoItemList)}", async () => await httpClient.GetFromJsonAsync("TodoItem", AppJsonContext.Default.ListTodoItemDto));
            GenarateViewTodoItemList();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void GenarateViewTodoItemList()
    {
        FilterTodoItemList();

        if (SearchTextTodoItem != null)
        {
            HandlerTodoItemSearch(SearchTextTodoItem);
        }
        if (SelectedSortTodoItemName != null)
        {
            HandlerTodoItemSort();
        }
    }

    private void NavigatePivotItem(BitPivotItem bitPivotItem)
    {
        SelectedPivotName = bitPivotItem.HeaderText;
        GenarateViewTodoItemList();
    }

    private void FilterTodoItemList()
    {
        if (SelectedPivotName == TodoStrings.ResourceManager.Translate(TodoStrings.All))
        {
            ViewTodoItemList = AllTodoItemList?.ToList();
        }
        if (SelectedPivotName == TodoStrings.ResourceManager.Translate(TodoStrings.Active))
        {
            ViewTodoItemList = AllTodoItemList?.Where(c => c.IsDone == false).ToList();
        }
        if (SelectedPivotName == TodoStrings.ResourceManager.Translate(TodoStrings.Completed))
        {
            ViewTodoItemList = AllTodoItemList?.Where(c => c.IsDone == true).ToList();
        }
    }

    private async Task ToggleTodoItem(TodoItemDto todoItem)
    {
        todoItem.IsDone = !todoItem.IsDone;
        await EditTodoItem(todoItem);
        GenarateViewTodoItemList();
    }

    private void HandlerTodoItemSearch(string searchStr)
    {
        FilterTodoItemList();
        ViewTodoItemList = ViewTodoItemList?.Where(td => td.Title!.Contains(searchStr, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void TodoItemSearch(string searchStr)
    {
        SearchTextTodoItem = searchStr;
        GenarateViewTodoItemList();
    }

    private void HandlerTodoItemSort()
    {
        if (SelectedSortTodoItemName == TodoStrings.ResourceManager.Translate(TodoStrings.Alphabetical))
        {
            ViewTodoItemList = ViewTodoItemList?.OrderBy(td => td.Title).ToList();
        }
        else
        {
            ViewTodoItemList = ViewTodoItemList?.OrderBy(td => td.Date).ToList();
        }
    }

    private void CancelEditMode(TodoItemDto todoItem)
    {
        todoItem.Title = EditModeTodoItemText;
        todoItem.IsInEditMode = false;
    }

    private void ToggleToEditMode(TodoItemDto todoItem)
    {
        EditModeTodoItemText = todoItem.Title;
        todoItem.IsInEditMode = true;
    }

    private void TodoItemSort()
    {
        SelectedSortTodoItemName = SortItemList.Where(s => s.IsSelected is true).Single().Text;
        GenarateViewTodoItemList();
    }

    private async Task AddTodoItem()
    {
        if (IsLoading)
        {
            return;
        }

        IsAddLoading = true;

        try
        {
            var newTodoItem = new TodoItemDto
            {
                Title = NewTodoItemTitle,
                Date = DateTimeOffset.Now,
            };

            await httpClient.PostAsJsonAsync("TodoItem", newTodoItem, AppJsonContext.Default.TodoItemDto);

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
        await httpClient.DeleteAsync($"TodoItem/{todoItem.Id}");
        AllTodoItemList?.Remove(todoItem);
        GenarateViewTodoItemList();
    }

    private async Task EditTodoItem(TodoItemDto todoItem)
    {
        if (string.IsNullOrWhiteSpace(todoItem.Title))
            return;

        todoItem.IsInEditMode = false;

        await httpClient.PutAsJsonAsync("TodoItem", todoItem, AppJsonContext.Default.TodoItemDto);
        GenarateViewTodoItemList();
    }
}
