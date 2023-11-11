using WebTemplate.Shared.Dtos.Todo;

namespace WebTemplate.Web.Pages.Todo;

[Authorize]
public partial class TodoPage
{
    private bool _isAdding;
    private bool _isLoading;
    private string? _searchText;
    private string? _selectedSort;
    private string? _selectedFilter;
    private string? _underEditTodoItemTitle;
    private string _newTodoTitle = string.Empty;
    private ConfirmMessageBox _confirmMessageBox = default!;
    private IList<TodoItemDto> _allTodoItems = default!;
    private IList<TodoItemDto> _viewTodoItems = default!;
    private List<BitDropdownItem<string>> _sortItems = [];

    protected override async Task OnInitAsync()
    {
        _selectedFilter = nameof(AppStrings.All);
        _selectedSort = nameof(AppStrings.Alphabetical);

        _sortItems =
        [
            new BitDropdownItem<string> { Text = Localizer[nameof(AppStrings.Alphabetical)], Value = nameof(AppStrings.Alphabetical) },
            new BitDropdownItem<string> { Text = Localizer[nameof(AppStrings.Date)], Value = nameof(AppStrings.Date) }
        ];

        await LoadTodoItems();

        await base.OnInitAsync();
    }

    private async Task LoadTodoItems()
    {
        _isLoading = true;

        try
        {
            await Task.Delay(10_000);

            _allTodoItems = await PrerenderStateService.GetValue($"{nameof(TodoPage)}-allTodoItems",
                                async () => await HttpClient.GetFromJsonAsync("TodoItem/Get", AppJsonContext.Default.ListTodoItemDto)) ?? [];

            FilterViewTodoItems();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void FilterViewTodoItems()
    {
        _viewTodoItems = _allTodoItems
            .Where(t => TodoItemIsVisible(t))
            .OrderByIf(_selectedSort == nameof(AppStrings.Alphabetical), t => t.Title!)
            .OrderByIf(_selectedSort == nameof(AppStrings.Date), t => t.Date!)
            .ToList();
    }

    private bool TodoItemIsVisible(TodoItemDto todoItem)
    {
        var condition1 = string.IsNullOrWhiteSpace(_searchText) || todoItem.Title!.Contains(_searchText!, StringComparison.OrdinalIgnoreCase);

        var condition2 = _selectedFilter == nameof(AppStrings.Active) ? todoItem.IsDone is false
            : _selectedFilter == nameof(AppStrings.Completed) ? todoItem.IsDone
            : true;

        return condition1 && condition2;
    }

    private async Task ToggleIsDone(TodoItemDto todoItem)
    {
        todoItem.IsDone = !todoItem.IsDone;

        await UpdateTodoItem(todoItem);
    }

    private void SearchTodoItems(string searchText)
    {
        _searchText = searchText;

        FilterViewTodoItems();
    }

    private void SortTodoItems(BitDropdownItem<string> sort)
    {
        _selectedSort = sort.Value;

        FilterViewTodoItems();
    }

    private void FilterTodoItems(string filter)
    {
        _selectedFilter = filter;

        FilterViewTodoItems();
    }

    private void ToggleEditMode(TodoItemDto todoItem)
    {
        _underEditTodoItemTitle = todoItem.Title;
        todoItem.IsInEditMode = !todoItem.IsInEditMode;
    }

    private async Task AddTodoItem()
    {
        if (_isAdding) return;

        _isAdding = true;

        try
        {
            var addedTodoItem = await (await HttpClient.PostAsJsonAsync("TodoItem/Create", new() { Title = _newTodoTitle }, AppJsonContext.Default.TodoItemDto))
                .Content.ReadFromJsonAsync(AppJsonContext.Default.TodoItemDto);

            _allTodoItems.Add(addedTodoItem!);

            if (TodoItemIsVisible(addedTodoItem!))
            {
                _viewTodoItems.Add(addedTodoItem!);
            }

            _newTodoTitle = "";
        }
        finally
        {
            _isAdding = false;
        }
    }

    private async Task DeleteTodoItem(TodoItemDto todoItem)
    {
        if (_isLoading) return;

        try
        {
            var confirmed = await _confirmMessageBox.Show(Localizer.GetString(nameof(AppStrings.AreYouSureWannaDelete), todoItem.Title!),
                                                     Localizer[nameof(AppStrings.DeleteTodoItem)]);

            if (confirmed)
            {
                _isLoading = true;

                StateHasChanged();

                await HttpClient.DeleteAsync($"TodoItem/Delete/{todoItem.Id}");

                _allTodoItems.Remove(todoItem);

                _viewTodoItems.Remove(todoItem);
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SaveTodoItem(TodoItemDto todoItem)
    {
        if (_isLoading) return;

        _isLoading = true;

        try
        {
            todoItem.Title = _underEditTodoItemTitle;

            await UpdateTodoItem(todoItem);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task UpdateTodoItem(TodoItemDto todoItem)
    {
        (await (await HttpClient.PutAsJsonAsync("TodoItem/Update", todoItem, AppJsonContext.Default.TodoItemDto))
            .Content.ReadFromJsonAsync(AppJsonContext.Default.TodoItemDto))!.Patch(todoItem);

        todoItem.IsInEditMode = false;

        if (TodoItemIsVisible(todoItem) is false)
        {
            _viewTodoItems.Remove(todoItem);
        }
    }
}
