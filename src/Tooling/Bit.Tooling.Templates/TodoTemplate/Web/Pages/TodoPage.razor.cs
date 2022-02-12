﻿using TodoTemplate.Shared.Dtos.TodoItem;
namespace TodoTemplate.App.Pages;

public partial class TodoPage
{
    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    [Inject]
    public IStateService StateService { get; set; } = default!;

    public bool IsLoading { get; set; }
    public string NewTodoItemTitle { get; set; } = string.Empty;
    public List<TodoItemDto>? TodoItemList { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadTodoItems();
    }

    private async Task LoadTodoItems()
    {
        IsLoading = true;
        try
        {
            TodoItemList = await StateService.GetValue(nameof(TodoItemList), async () => await HttpClient.GetFromJsonAsync<List<TodoItemDto>>("TodoItem"));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddTodoItem()
    {
        IsLoading = true;
        try
        {
            var newTodoItem = new TodoItemDto
            {
                Title = NewTodoItemTitle,
                Date = DateTime.Now
            };

            await HttpClient.PostAsJsonAsync("TodoItem", newTodoItem);

            await LoadTodoItems();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteTodoItem(int id)
    {
        await HttpClient.DeleteAsync($"TodoItem/{id}");

        await LoadTodoItems();
    }

    private async Task EditTodoItem(TodoItemDto todoItem)
    {
        if (string.IsNullOrWhiteSpace(todoItem.Title))
            return;

        todoItem.IsInEditMode = false;

        await HttpClient.PutAsJsonAsync("TodoItem", todoItem);
    }
}
