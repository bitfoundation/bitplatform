using Boilerplate.Shared.Controllers.Categories;
using Boilerplate.Shared.Dtos.Categories;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Categories;

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
    private AppDataAnnotationsValidator validatorRef = default!;

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
            validatorRef.DisplayErrors(e);
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
