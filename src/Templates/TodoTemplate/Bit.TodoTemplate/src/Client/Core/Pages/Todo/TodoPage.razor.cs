using TodoTemplate.Client.Core.Shared;
using TodoTemplate.Shared.Dtos.Todo;

namespace TodoTemplate.Client.Core.Pages.Todo;

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
    private IEnumerable<TodoItemDto> _viewTodoItems = default!;
    private List<BitDropdownItem> _sortItems = new();

    protected override async Task OnInitAsync()
    {
        _selectedFilter = nameof(AppStrings.All);
        _selectedSort = nameof(AppStrings.Alphabetical);

        _sortItems = new()
        {
            new BitDropdownItem { Text = Localizer[nameof(AppStrings.Alphabetical)], Value = nameof(AppStrings.Alphabetical) },
            new BitDropdownItem { Text = Localizer[nameof(AppStrings.Date)], Value = nameof(AppStrings.Date) }
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
            : _selectedFilter == nameof(AppStrings.Completed) ? todo.IsDone
            : true
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

        (await (await HttpClient.PutAsJsonAsync("TodoItem/Update", todoItem, AppJsonContext.Default.TodoItemDto))
            .Content.ReadFromJsonAsync(AppJsonContext.Default.TodoItemDto))!.Patch(todoItem);

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

    private void SortTodoItems(BitDropdownItem sort)
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
            var addedTodoItem = await (await HttpClient.PostAsJsonAsync("TodoItem/Create", new() { Title = _newTodoTitle }, AppJsonContext.Default.TodoItemDto))
                .Content.ReadFromJsonAsync(AppJsonContext.Default.TodoItemDto);

            _allTodoItems.Add(addedTodoItem!);

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

        try
        {
            var confirmed = await _confirmMessageBox.Show(Localizer.GetString(nameof(AppStrings.AreYouSureWannaDelete), todoItem.Title ?? string.Empty),
                                                     Localizer[nameof(AppStrings.DeleteTodoItem)]);

            if (confirmed)
            {
                _isLoading = true;
                
                await HttpClient.DeleteAsync($"TodoItem/Delete/{todoItem.Id}");

                _allTodoItems.Remove(todoItem);

                FilterViewTodoItems();
            }
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

            (await (await HttpClient.PutAsJsonAsync("TodoItem/Update", todoItem, AppJsonContext.Default.TodoItemDto))
                            .Content.ReadFromJsonAsync(AppJsonContext.Default.TodoItemDto))!.Patch(todoItem);

            FilterViewTodoItems();
        }
        finally
        {
            _isLoading = false;
        }
    }
}
