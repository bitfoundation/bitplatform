using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.Client.Shared.Pages;

[Authorize]
public partial class TodoPage
{
    public bool IsLoading { get; set; }
    public string SelectedPivotName { get; set; }
    public string? EditModeTodoItemText { get; set; }
    public bool IsAddLoading { get; set; }
    public string? SelectedSortTodoItemName { get; set; }
    public string? SearchTextTodoItem { get; set; }
    public string NewTodoItemTitle { get; set; } = string.Empty;
    public List<TodoItemDto>? AllTodoItemList { get; set; } = new();
    public List<TodoItemDto>? ViewTodoItemList { get; set; } = new();

    public List<BitDropDownItem> SortItemList = new();

    protected override async Task OnInitAsync()
    {
        SelectedPivotName = Localizer[nameof(AppStrings.All)];

        SortItemList = new()
        {
            new BitDropDownItem
            {
                Text = Localizer[nameof(AppStrings.Alphabetical)],
            },
            new BitDropDownItem
            {
                Text = Localizer[nameof(AppStrings.Date)],
            }
        };

        await LoadTodoItems();

        await base.OnInitAsync();
    }

    private async Task LoadTodoItems()
    {
        IsLoading = true;
        try
        {
            AllTodoItemList = await StateService.GetValue($"{nameof(TodoPage)}-{nameof(AllTodoItemList)}", async () => await HttpClient.GetFromJsonAsync("TodoItem/Get", AppJsonContext.Default.ListTodoItemDto));
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
        if (SelectedPivotName == Localizer[nameof(AppStrings.All)])
        {
            ViewTodoItemList = AllTodoItemList?.ToList();
        }
        if (SelectedPivotName == Localizer[nameof(AppStrings.Active)])
        {
            ViewTodoItemList = AllTodoItemList?.Where(c => c.IsDone == false).ToList();
        }
        if (SelectedPivotName == Localizer[nameof(AppStrings.Completed)])
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
        if (SelectedSortTodoItemName == Localizer[nameof(AppStrings.Alphabetical)])
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

            await HttpClient.PostAsJsonAsync("TodoItem/Create", newTodoItem, AppJsonContext.Default.TodoItemDto);

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
        await HttpClient.DeleteAsync($"TodoItem/Delete/{todoItem.Id}");
        AllTodoItemList?.Remove(todoItem);
        GenarateViewTodoItemList();
    }

    private async Task EditTodoItem(TodoItemDto todoItem)
    {
        if (string.IsNullOrWhiteSpace(todoItem.Title))
            return;

        todoItem.IsInEditMode = false;

        await HttpClient.PutAsJsonAsync("TodoItem/Update", todoItem, AppJsonContext.Default.TodoItemDto);
        GenarateViewTodoItemList();
    }
}
