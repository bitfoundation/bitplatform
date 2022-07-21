using AdminPanel.Shared.Dtos.Categories;

namespace AdminPanel.App.Pages.Categories;

public partial class AddOrEditCategoryPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

    [AutoInject] private IStateService stateService = default!;

    [Parameter]
    public int? Id { get; set; }

    public CategoryDto? Category { get; set; } = new();
    public bool IsLoading { get; private set; }
    public bool IsSaveLoading { get; private set; }
    public BitMessageBarType SaveMessageType { get; set; }
    public string? SaveMessage { get; set; }
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
        }        
        finally
        {
            IsLoading = false;
        }
        
    }

    private async Task Save()
    {
        if (IsSaveLoading)
        {
            return;
        }

        try
        {
            IsSaveLoading = true;

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
        catch (ResourceValidationException e)
        {
            SaveMessageType = BitMessageBarType.Error;
            SaveMessage = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Messages)
                .Select(e => ErrorStrings.ResourceManager.Translate(e, Category.Name!)));
        }
        catch (KnownException e)
        {
            SaveMessageType = BitMessageBarType.Error;
            SaveMessage = ErrorStrings.ResourceManager.Translate(e.Message, Category.Name);
        }
        finally
        {
            IsSaveLoading = false;
        }

    }

}
