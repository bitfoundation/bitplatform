using Boilerplate.Shared.Dtos.Categories;
using Boilerplate.Shared.Controllers.Categories;
using Microsoft.AspNetCore.Components.Forms;

namespace Boilerplate.Client.Core.Components.Pages.Categories;

public partial class AddOrEditCategoryModal
{
    [AutoInject] ICategoryController categoryController = default!;

    [Parameter] public EventCallback OnSave { get; set; }

    private bool isOpen;
    private bool isSaving;
    private bool isColorPickerOpen;
    private CategoryDto category = new();
    private EditForm editForm = default!;
    private AppDataAnnotationsValidator validatorRef = default!;

    private bool isChanged => editForm?.EditContext?.IsModified() is true;

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

            await OnSave.InvokeAsync();
            isOpen = false;
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
