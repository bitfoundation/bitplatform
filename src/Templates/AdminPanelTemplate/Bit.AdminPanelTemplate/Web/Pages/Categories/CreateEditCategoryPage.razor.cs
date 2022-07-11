using AdminPanelTemplate.Shared.Dtos.Categories;

namespace AdminPanelTemplate.App.Pages.Categories;

public partial class CreateEditCategoryPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

    [AutoInject] private IStateService stateService = default!;



    [Parameter]
    public int? Id { get; set; }

    public CategoryDto? Category { get; set; } = new();
    public bool IsLoading { get; private set; }

    protected override async Task OnInitAsync()
    {
        await LoadCategory();
        await base.OnInitAsync();
    }

    private async Task LoadCategory()
    {
        if (Id == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            Category = await httpClient.GetFromJsonAsync($"Category/{Id}", AppJsonContext.Default.CategoryDto);
            Category!.Color = "#FFFFFF";
            
        }
        finally
        {
            IsLoading = false;
        }
        
    }

    private async Task Save()
    {
        if (IsLoading)
        {
            return;
        }


        try
        {
            if (Category!.Id == 0)
            {
                await httpClient.PostAsJsonAsync("Category", Category, AppJsonContext.Default.CategoryDto);
            }
            else
            {
                await httpClient.PutAsJsonAsync("Category", Category, AppJsonContext.Default.CategoryDto);
            }

            navigationManager.NavigateTo("categories");
        }
        finally
        {

        }

    }

}
