using Boilerplate.Shared.Controllers.Todo;
using Boilerplate.Shared.Dtos.Todo;

namespace Boilerplate.Client.Core.Components.Pages.Todo;

[Authorize]
public partial class TodoPage
{
    [AutoInject] Keyboard keyboard = default!;
    [AutoInject] ITodoItemController todoItemController = default!;

    private bool isLoading;
    private string? searchText;
    private string? selectedSort;
    private string? selectedFilter;
    private string? underEditTodoItemTitle;
    private string newTodoTitle = string.Empty;
    private ConfirmMessageBox confirmMessageBox = default!;
    private IList<TodoItemDto> allTodoItems = [];
    private IList<TodoItemDto> viewTodoItems = default!;
    private BitSearchBox searchBox = default!;

    protected override async Task OnInitAsync()
    {
        _ = keyboard.Add(ButilKeyCodes.KeyF, () => _ = searchBox.FocusAsync(), ButilModifiers.Ctrl);

        selectedFilter = nameof(AppStrings.All);
        selectedSort = nameof(AppStrings.Alphabetical);

        await LoadTodoItems();

        await base.OnInitAsync();
    }

    private async Task LoadTodoItems()
    {
        isLoading = true;

        try
        {
            allTodoItems = await todoItemController.Get(CurrentCancellationToken);

            FilterViewTodoItems();
        }
        finally
        {
            isLoading = false;
        }
    }

    private void FilterViewTodoItems()
    {
        viewTodoItems = allTodoItems
            .Where(t => TodoItemIsVisible(t))
            .OrderByIf(selectedSort == nameof(AppStrings.Alphabetical), t => t.Title!)
            .OrderByIf(selectedSort == nameof(AppStrings.Date), t => t.Date!)
            .ToList();
    }

    private bool TodoItemIsVisible(TodoItemDto todoItem)
    {
        var condition1 = string.IsNullOrWhiteSpace(searchText) || todoItem.Title!.Contains(searchText!, StringComparison.OrdinalIgnoreCase);

        var condition2 = selectedFilter == nameof(AppStrings.Active) ? todoItem.IsDone is false
            : selectedFilter == nameof(AppStrings.Completed) ? todoItem.IsDone
            : true;

        return condition1 && condition2;
    }

    private async Task ToggleIsDone(TodoItemDto todoItem)
    {
        todoItem.IsDone = !todoItem.IsDone;

        await UpdateTodoItem(todoItem);
    }

    private void SearchTodoItems(string text)
    {
        searchText = text;

        FilterViewTodoItems();
    }

    private void SortTodoItems(string? sort)
    {
        selectedSort = sort;

        FilterViewTodoItems();
    }

    private void FilterTodoItems(string filter)
    {
        selectedFilter = filter;

        FilterViewTodoItems();
    }

    private void ToggleEditMode(TodoItemDto todoItem)
    {
        underEditTodoItemTitle = todoItem.Title;
        todoItem.IsInEditMode = !todoItem.IsInEditMode;
    }

    private async Task AddTodoItem()
    {
        var addedTodoItem = await todoItemController.Create(new() { Title = newTodoTitle }, CurrentCancellationToken);

        allTodoItems.Add(addedTodoItem!);

        if (TodoItemIsVisible(addedTodoItem!))
        {
            viewTodoItems.Add(addedTodoItem!);
        }

        newTodoTitle = "";
    }

    private async Task DeleteTodoItem(TodoItemDto todoItem)
    {
        if (isLoading) return;

        try
        {
            var confirmed = await confirmMessageBox.Show(Localizer.GetString(nameof(AppStrings.AreYouSureWannaDelete), todoItem.Title!),
                                                     Localizer[nameof(AppStrings.DeleteTodoItem)]);

            if (confirmed)
            {
                isLoading = true;

                StateHasChanged();

                await todoItemController.Delete(todoItem.Id, CurrentCancellationToken);

                allTodoItems.Remove(todoItem);

                viewTodoItems.Remove(todoItem);
            }
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SaveTodoItem(TodoItemDto todoItem)
    {
        if (isLoading) return;

        isLoading = true;

        try
        {
            todoItem.Title = underEditTodoItemTitle;

            await UpdateTodoItem(todoItem);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task UpdateTodoItem(TodoItemDto todoItem)
    {
        (await todoItemController.Update(todoItem, CurrentCancellationToken)).Patch(todoItem);

        todoItem.IsInEditMode = false;

        if (TodoItemIsVisible(todoItem) is false)
        {
            viewTodoItems.Remove(todoItem);
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(true);

        if (disposing)
        {
            await keyboard.DisposeAsync();
        }
    }
}
