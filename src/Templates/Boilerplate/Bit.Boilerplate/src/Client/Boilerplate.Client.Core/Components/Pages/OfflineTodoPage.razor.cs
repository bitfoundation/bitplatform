using Boilerplate.Client.Core.Data;
using Boilerplate.Shared.Dtos.Todo;
using Microsoft.EntityFrameworkCore;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class OfflineTodoPage
{
    [AutoInject] SyncService syncService = default!;
    [AutoInject] IDbContextFactory<AppOfflineDbContext> offlineDbContextFactory = default!;

    // Refer to .docs/09- Dependency Injection & Service Registration.md 's Owned services section for more information about ScopedServices
    Keyboard keyboard => field ??= ScopedServices.GetRequiredService<Keyboard>();

    private bool isLoading;
    private string? searchText;
    private string? selectedSort;
    private bool isDescendingSort;
    private string? selectedFilter;
    private bool isDeleteDialogOpen;
    private TodoItemDto? deletingTodoItem;
    private string? underEditTodoItemTitle;
    private BitSearchBox searchBox = default!;
    private string newTodoTitle = string.Empty;
    private List<TodoItemDto> allTodoItems = [];
    private List<TodoItemDto> viewTodoItems = [];
    private BitTextField newTodoInput = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        selectedFilter = nameof(AppStrings.All);
        selectedSort = nameof(AppStrings.Alphabetical);

        await LoadTodoItems();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await keyboard.Add(ButilKeyCodes.KeyF, () => searchBox.FocusAsync(), ButilModifiers.Ctrl);

        await base.OnAfterFirstRenderAsync();
    }

    private async Task LoadTodoItems(bool showLoading = true)
    {
        if (showLoading)
        {
            isLoading = true;
        }

        try
        {
            await using var dbContext = await offlineDbContextFactory.CreateDbContextAsync(CurrentCancellationToken);

            await syncService.Pull(CurrentCancellationToken);

            allTodoItems = await dbContext.TodoItems.ToListAsync(CurrentCancellationToken);

            FilterViewTodoItems();
        }
        finally
        {
            if (showLoading)
            {
                isLoading = false;
            }
        }
    }

    private void FilterViewTodoItems()
    {
        var items = allTodoItems.Where(TodoItemIsVisible);
        if (isDescendingSort)
        {
            items = items.OrderByDescendingIf(selectedSort == nameof(AppStrings.Alphabetical), t => t.Title!)
                         .OrderByDescendingIf(selectedSort == nameof(AppStrings.Date), t => t.UpdatedAt!);
        }
        else
        {
            items = items.OrderByIf(selectedSort == nameof(AppStrings.Alphabetical), t => t.Title!)
                         .OrderByIf(selectedSort == nameof(AppStrings.Date), t => t.UpdatedAt!);
        }
        viewTodoItems = items.ToList();
    }

    private bool TodoItemIsVisible(TodoItemDto todoItem)
    {
        var condition1 = string.IsNullOrWhiteSpace(searchText) || todoItem.Title!.Contains(searchText!, StringComparison.OrdinalIgnoreCase);

        var condition2 = selectedFilter == nameof(AppStrings.Active) ? todoItem.IsDone is false
            : selectedFilter == nameof(AppStrings.Completed) ? todoItem.IsDone
            : true;

        return condition1 && condition2;
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

    private void EnterEditMode(TodoItemDto todoItem)
    {
        allTodoItems.ForEach(t => t.IsInEditMode = false);
        underEditTodoItemTitle = todoItem.Title;
        todoItem.IsInEditMode = true;
    }

    private void ExitEditMode(TodoItemDto todoItem)
    {
        todoItem.IsInEditMode = false;
    }

    private async Task AddTodoItem()
    {
        if (string.IsNullOrWhiteSpace(newTodoTitle)) return;

        await using var dbContext = await offlineDbContextFactory.CreateDbContextAsync(CurrentCancellationToken);

        var addedTodoItem = (await dbContext.TodoItems.AddAsync(new()
        {
            Id = Guid.NewGuid().ToString(),
            Title = newTodoTitle
        }, CurrentCancellationToken)).Entity;

        await dbContext.SaveChangesAsync(CurrentCancellationToken);

        _ = syncService.Push();

        allTodoItems.Add(addedTodoItem!);

        if (TodoItemIsVisible(addedTodoItem!))
        {
            viewTodoItems.Add(addedTodoItem!);
        }

        newTodoTitle = "";
        await newTodoInput.FocusAsync();
    }

    private async Task DeleteTodoItem()
    {
        if (isLoading || deletingTodoItem is null) return;

        isLoading = true;

        try
        {
            await using var dbContext = await offlineDbContextFactory.CreateDbContextAsync(CurrentCancellationToken);
            dbContext.TodoItems.Remove(deletingTodoItem);
            await dbContext.SaveChangesAsync(CurrentCancellationToken);

            _ = syncService.Push();

            allTodoItems.Remove(deletingTodoItem);
            viewTodoItems.Remove(deletingTodoItem);
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
        await using var dbContext = await offlineDbContextFactory.CreateDbContextAsync(CurrentCancellationToken);
        dbContext.TodoItems.Update(todoItem);
        await dbContext.SaveChangesAsync(CurrentCancellationToken);

        _ = syncService.Push();

        todoItem.IsInEditMode = false;

        if (TodoItemIsVisible(todoItem) is false)
        {
            viewTodoItems.Remove(todoItem);
        }
    }
}
