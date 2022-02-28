using TodoTemplate.Shared.Dtos.TodoItem;
namespace TodoTemplate.App.Pages;

public partial class TodoPage
{
    public List<TodoItemDto> AllTodoItemList { get; set; } = new();

    public List<TodoItemDto> ViewTodoItemList { get; set; } = new();

    public List<BitDropDownItem> SortItemList = new()
    {
        new BitDropDownItem
        {
            Text = "Alphabetical"
        },
        new BitDropDownItem
        {
            Text = "Date"
        }
    };
    public string? SelectedPivotName { get; set; }
    public string? SelectedSortName { get; set; }
    public string? SearchText { get; set; }

    public string? TitleToAdd { get; set; }
    public string? TitleToEdit { get; set; }

    public bool IsLoadingPage { get; set; }
    public bool IsEnabledAddButton { get; set; }
    public bool IsEnabledEditButton { get; set; }
    public bool IsLoadingAddButton { get; set; }
    public bool IsLoadingEditButton { get; set; }

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public IStateService StateService { get; set; } = default!;

    protected override async Task OnInitAsync()
    {
        IsLoadingPage = true;

        await LoadTodoItemsData();

        IsLoadingPage = false;

        await base.OnInitAsync();
    }

    private async Task LoadTodoItemsData()
    {
        AllTodoItemList = await StateService.GetValue(nameof(AllTodoItemList), async () =>
            await HttpClient.GetFromJsonAsync("TodoItem", ToDoTemplateJsonContext.Default.ListTodoItemDto))
            ?? new List<TodoItemDto>();

        ViewTodoItemList = AllTodoItemList;

        GenerateViewTodoItemList();
    }

    private void GenerateViewTodoItemList()
    {
        TodoItemListPivotHandler(SelectedPivotName ?? String.Empty);

        if (SearchText is not null)
        {
            TodoItemSearchHandler(SearchText);
        }
        if (SortItemList.Any(item => item.IsSelected))
        {
            TodoItemSortHandler();
        }
    }

    private void TodoItemListPivotHandler(string filterName)
    {
        switch (filterName)
        {
            case "All":
                ViewTodoItemList = AllTodoItemList;
                break;

            case "Active":
                ViewTodoItemList = AllTodoItemList.Where(c => c.IsDone == false).ToList();
                break;

            case "Completed":
                ViewTodoItemList = AllTodoItemList.Where(c => c.IsDone).ToList();
                break;

            default:
                ViewTodoItemList = AllTodoItemList;
                break;
        }
    }

    private void TodoItemSortHandler()
    {
        ViewTodoItemList = SelectedSortName == "Alphabetical"
            ? ViewTodoItemList.OrderBy(td => td.Title).ToList() 
            : ViewTodoItemList.OrderBy(td => td.Date).ToList();
    }

    private void SetSearchText(string? text)
    {
        SearchText = text;
        GenerateViewTodoItemList();
    }

    private void TodoItemSearchHandler(string searchStr)
    {
        ViewTodoItemList = ViewTodoItemList.Where(td => td.Title != null && td.Title.Contains(searchStr)).ToList();
    }

    private void SetSelectedPivotName(BitPivotItem bitPivotItem)
    {
        SelectedPivotName = bitPivotItem.HeaderText;
        GenerateViewTodoItemList();
    }

    private void SetSelectedSortName(BitDropDownItem bitDropDownItem)
    {
        SelectedSortName = bitDropDownItem.Text;
        GenerateViewTodoItemList();
    }

    private async void ToggleTodoItem(TodoItemDto todoItem)
    {
        todoItem.IsDone = !todoItem.IsDone;
        TitleToEdit = todoItem.Title;
        await EditTodoItem(todoItem);
    }

    private void CheckAddButtonEnable()
    {
        if (string.IsNullOrEmpty(TitleToAdd))
        {
            IsEnabledAddButton = false;
            return;
        }

        IsEnabledAddButton = true;
    }

    private async Task AddTodoItem()
    {
        IsLoadingAddButton = true;

        try
        {
            var todoItemToAdd = new TodoItemDto
            {
                Title = TitleToAdd,
                Date = DateTime.Now
            };

            await HttpClient.PostAsJsonAsync("TodoItem", todoItemToAdd, ToDoTemplateJsonContext.Default.TodoItemDto);

            await LoadTodoItemsData();

            TitleToAdd = String.Empty;
        }
        finally
        {
            IsLoadingAddButton = false;
        }
    }

    private void ChangeToEditMode(TodoItemDto todoItem)
    {
        TitleToEdit = todoItem.Title;
        IsEnabledEditButton = false;
        todoItem.IsInEditMode = true;
    }

    private void CancelEditMode(TodoItemDto todoItem)
    {
        TitleToEdit = String.Empty;
        IsEnabledEditButton = false;
        todoItem.IsInEditMode = false;
    }

    private void CheckEditButtonEnable(int todoItemId)
    {
        if (string.IsNullOrEmpty(TitleToEdit) 
            || AllTodoItemList.Find(dto => dto.Id == todoItemId)?.Title == TitleToEdit)
        {
            IsEnabledEditButton = false;
            return;
        }

        IsEnabledEditButton = true;
    }

    private async Task EditTodoItem(TodoItemDto todoItem)
    {
        IsLoadingEditButton = true;

        try
        {
            var todoItemToEdit = new TodoItemDto
            {
                Id = todoItem.Id,
                Title = TitleToEdit,
                Date = todoItem.Date,
                IsDone = todoItem.IsDone
            };

            await HttpClient.PutAsJsonAsync("TodoItem", todoItemToEdit, ToDoTemplateJsonContext.Default.TodoItemDto);

            todoItem.Title = TitleToEdit;
            todoItem.IsInEditMode = false;
            IsEnabledEditButton = false;

            GenerateViewTodoItemList();
        }
        finally
        {
            IsLoadingEditButton = false;
        }
    }

    private async Task DeleteTodoItem(TodoItemDto todoItem)
    {
        await HttpClient.DeleteAsync($"TodoItem/{todoItem.Id}");

        AllTodoItemList.Remove(todoItem);

        GenerateViewTodoItemList();
    }
}
