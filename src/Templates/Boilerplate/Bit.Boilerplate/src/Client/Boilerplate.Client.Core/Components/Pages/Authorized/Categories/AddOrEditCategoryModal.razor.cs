using Boilerplate.Shared.Dtos.Categories;
using Boilerplate.Shared.Controllers.Categories;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Categories;

public partial class AddOrEditCategoryModal
{
    protected override string? Title => Localizer[nameof(AppStrings.Category)];
    protected override string? Subtitle => string.Empty;

    [AutoInject] ICategoryController categoryController = default!;

    [Parameter] public EventCallback OnSave { get; set; }

    private bool isOpen;
    private bool isSaving;
    private bool isColorPickerOpen;
    private CategoryDto category = new();
    private AppDataAnnotationsValidator validatorRef = default!;

    public async Task ShowModal(CategoryDto categoryToShow)
    {
        await InvokeAsync(async () =>
        {
            isOpen = true;
            categoryToShow.Patch(category);
            StateHasChanged();
        });
    }

    private void SetCategoryColor(string color)
    {
        category.Color = color;
    }

    private async Task Save()
    {
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
            validatorRef.DisplayErrors(e);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            await OnSave.InvokeAsync();
        }
    }
}
