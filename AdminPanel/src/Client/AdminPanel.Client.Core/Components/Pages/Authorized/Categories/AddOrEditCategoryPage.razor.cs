using AdminPanel.Shared.Controllers.Categories;
using AdminPanel.Shared.Dtos.Categories;

namespace AdminPanel.Client.Core.Components.Pages.Authorized.Categories;

[Authorize]
public partial class AddOrEditCategoryPage
{
    protected override string? Title => Localizer[nameof(AppStrings.Category)];
    protected override string? Subtitle => string.Empty;

    [AutoInject] ICategoryController categoryController = default!;

    [Parameter] public Guid? Id { get; set; }

    private bool isLoading;
    private bool isSaving;
    private bool isColorPickerOpen;
    private CategoryDto category = new();

    protected override async Task OnInitAsync()
    {
        await LoadCategory();
    }

    private async Task LoadCategory()
    {
        if (Id is null) return;

        isLoading = true;

        try
        {
            category = await categoryController.Get(Id.Value, CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void SetCategoryColor(string color)
    {
        category.Color = color;
    }

    private void ToggleColorPicker()
    {
        isColorPickerOpen = !isColorPickerOpen;
    }

    private async Task Save()
    {
        if (isSaving) return;

        isSaving = true;

        try
        {
            if (category.Id == default)
            {
                await categoryController.Create(category, CurrentCancellationToken);
            }
            else
            {
                await categoryController.Update(category, CurrentCancellationToken);
            }

            NavigationManager.NavigateTo(Urls.CategoriesPage);
        }
        catch (ResourceValidationException e)
        {
            SnackBarService.Error(string.Join(Environment.NewLine, e.Payload.Details.SelectMany(d => d.Errors).Select(e => e.Message)));
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isSaving = false;
        }
    }
}
