using AdminPanel.Shared.Dtos.Categories;

namespace AdminPanel.App.Pages.Categories;

[Authorize]
public partial class AddOrEditCategoryPage
{
    [Parameter]
    public int? Id { get; set; }

    public CategoryDto? Category { get; set; } = new();
    public bool IsLoading { get; private set; }
    public bool IsSaveLoading { get; private set; }
    public bool IsColorPickerOpen { get; set; }
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
            Category = await HttpClient.GetFromJsonAsync($"Category/Get/{Id}", AppJsonContext.Default.CategoryDto);
        }        
        finally
        {
            IsLoading = false;
        }
    }

    private void SetCategoryColor(string color)
    {
        Category!.Color = color;
    }

    private void ToggleColorPicker()
    {
        IsColorPickerOpen = !IsColorPickerOpen;
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
                await HttpClient.PostAsJsonAsync("Category/Create", Category, AppJsonContext.Default.CategoryDto);
            }
            else
            {
                await HttpClient.PutAsJsonAsync("Category/Update", Category, AppJsonContext.Default.CategoryDto);
            }

            NavigationManager.NavigateTo("categories");
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
