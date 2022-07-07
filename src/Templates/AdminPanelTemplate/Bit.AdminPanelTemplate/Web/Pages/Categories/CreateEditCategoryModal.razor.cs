using AdminPanelTemplate.Shared.Dtos.Categories;

namespace AdminPanelTemplate.App.Pages.Categories;

public partial class CreateEditCategoryModal
{
    [Parameter]
    public CategoryDto Category { get; set; }


    [Parameter]
    public EventCallback OnCategorySave { get; set; }

    private bool IsOpen { get; set; }


    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    public void ShowModal(CategoryDto category)
    {
        InvokeAsync(() =>
        {
            IsOpen = true;

            Category = category;

            StateHasChanged();
        });
    }

    private async Task Save()
    {
        IsOpen = false;
        await OnCategorySave.InvokeAsync();
    }

    

    private void OnCloseClick()
    {
        IsOpen = false;
    }


    
}
