using AdminPanel.Shared.Dtos.Categories;

namespace AdminPanel.Client.Shared.Pages;

[Authorize]
public partial class AddOrEditCategoryPage
{
    [Parameter] public int? Id { get; set; }

    private bool _isLoading;
    private bool _isSaveLoading;
    private string? _saveMessage;
    private bool _isColorPickerOpen;
    private BitMessageBarType _saveMessageType;
    private CategoryDto _category = new();

    protected override async Task OnInitAsync()
    {
        await LoadCategory();
    }

    private async Task LoadCategory()
    {
        if (Id is null) return;

        _isLoading = true;

        try
        {
            _category = await HttpClient.GetFromJsonAsync($"Category/Get/{Id}", AppJsonContext.Default.CategoryDto) ?? new();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void SetCategoryColor(string color)
    {
        _category.Color = color;
    }

    private void ToggleColorPicker()
    {
        _isColorPickerOpen = !_isColorPickerOpen;
    }

    private async Task Save()
    {
        if (_isSaveLoading) return;

        _isSaveLoading = true;

        try
        {
            if (_category.Id == 0)
            {
                await HttpClient.PostAsJsonAsync("Category/Create", _category, AppJsonContext.Default.CategoryDto);
            }
            else
            {
                await HttpClient.PutAsJsonAsync("Category/Update", _category, AppJsonContext.Default.CategoryDto);
            }

            NavigationManager.NavigateTo("categories");
        }
        catch (ResourceValidationException e)
        {
            _saveMessageType = BitMessageBarType.Error;

            _saveMessage = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Errors).Select(e => e.Message));
        }
        catch (KnownException e)
        {
            _saveMessage = e.Message;
            _saveMessageType = BitMessageBarType.Error;
        }
        finally
        {
            _isSaveLoading = false;
        }
    }
}
