using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.Client.Shared.Pages;

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
    private IList<TodoItemDto> _allTodoItems = default!;
    private IEnumerable<TodoItemDto> _viewTodoItems = default!;
    private List<BitDropDownItem> _sortItems = new();

    protected override async Task OnInitAsync()
    {
        _selectedFilter = nameof(AppStrings.All);
        _selectedSort = nameof(AppStrings.Alphabetical);

        _sortItems = new()
        {
            new BitDropDownItem { Text = Localizer[nameof(AppStrings.Alphabetical)], Value = nameof(AppStrings.Alphabetical) },
            new BitDropDownItem { Text = Localizer[nameof(AppStrings.Date)], Value = nameof(AppStrings.Date) }
        };

        await LoadTodoItems();

        await base.OnInitAsync();
    }

    private async Task LoadTodoItems()
    {
        _isLoading = true;

        try
        {
            _allTodoItems = await StateService.GetValue($"{nameof(TodoPage)}-allTodoItems",
                                async () => await HttpClient.GetFromJsonAsync("TodoItem/Get", AppJsonContext.Default.ListTodoItemDto)) ?? new();

            FilterViewTodoItems();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void FilterViewTodoItems()
    {
        _viewTodoItems = _allTodoItems.Where(todo =>
            _selectedFilter == nameof(AppStrings.Active) ? todo.IsDone == false
            : _selectedFilter != nameof(AppStrings.Completed) || todo.IsDone
        );

        if (_searchText is not null)
        {
            _viewTodoItems = _viewTodoItems.Where(t => string.IsNullOrWhiteSpace(t.Title) is false
                                                                && t.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (_selectedSort is not null)
        {
            _viewTodoItems = _selectedSort == nameof(AppStrings.Alphabetical)
                                ? _viewTodoItems.OrderBy(t => t.Title)
                                : _viewTodoItems.OrderBy(t => t.Date);
        }
    }

    private async Task ToggleIsDone(TodoItemDto todoItem)
    {
        todoItem.IsDone = !todoItem.IsDone;

        await HttpClient.PutAsJsonAsync("TodoItem/Update", todoItem, AppJsonContext.Default.TodoItemDto);

        FilterViewTodoItems();
    }

    private void SearchTodoItems(string searchText)
    {
        _searchText = searchText;

        FilterViewTodoItems();
    }

    private void CancelEditMode(TodoItemDto todoItem)
    {
        todoItem.IsUnderEdit = false;
    }

    private void ToggleToEditMode(TodoItemDto todoItem)
    {
        todoItem.IsUnderEdit = true;
        _underEditTodoItemTitle = todoItem.Title;
    }

    private void SortTodoItems(BitDropDownItem sort)
    {
        _selectedSort = sort.Value;

        FilterViewTodoItems();
    }

    private async Task AddTodoItem()
    {
        if (_isAdding) return;

        _isAdding = true;

        try
        {
            var newTodoItem = new TodoItemDto { Title = _newTodoTitle, Date = DateTimeOffset.Now };

            var response = await HttpClient.PostAsJsonAsync("TodoItem/Create", newTodoItem, AppJsonContext.Default.TodoItemDto);

            var content = await response.Content.ReadAsStringAsync();

            newTodoItem.Id = int.Parse(content);

            _allTodoItems.Add(newTodoItem);

            FilterViewTodoItems();

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

        _isLoading = true;

        try
        {
            await HttpClient.DeleteAsync($"TodoItem/Delete/{todoItem.Id}");

            _allTodoItems.Remove(todoItem);

            FilterViewTodoItems();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task EditTodoItem(TodoItemDto todoItem)
    {
        if (_isLoading || string.IsNullOrWhiteSpace(_underEditTodoItemTitle)) return;

        _isLoading = true;

        try
        {
            todoItem.IsUnderEdit = false;
            todoItem.Title = _underEditTodoItemTitle;

            await HttpClient.PutAsJsonAsync("TodoItem/Update", todoItem, AppJsonContext.Default.TodoItemDto);

            FilterViewTodoItems();
        }
        finally
        {
            _isLoading = false;
        }
    }
}
