//-:cnd:noEmit
using System.Text.Json;
using AdminPanelTemplate.App.Shared;
using AdminPanelTemplate.Shared.Dtos.Categories;

namespace AdminPanelTemplate.App.Pages.Categories;
public partial class CategoriesPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

    [AutoInject] private IStateService stateService = default!;

    public bool IsLoading { get; set; }

    BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };
    BitDataGrid<CategoryDto>? dataGrid;
    BitDataGridItemsProvider<CategoryDto>? categoriesProvider;

    int NumResults;
    string _categoryNameFilter = string.Empty;
    string CategoryNameFilter
    {
        get => _categoryNameFilter;
        set
        {
            _categoryNameFilter = value;
            _ =RefreshData();
        }
    }

    protected override async Task OnInitAsync()
    {
        await PrepareGridDataProvider();
        await base.OnInitAsync();
    }

    private Task PrepareGridDataProvider()
    {
        categoriesProvider = async req =>
        {
            try
            {
                IsLoading = true;
                var input = new PagedInputDto()
                {
                    Skip = req.StartIndex,
                    MaxResultCount = req.Count.HasValue ? req.Count.Value : 10,
                    Filter = _categoryNameFilter,
                    SortBy = req.SortByColumn?.Title,
                    SortAscending = req.SortByAscending
                };

                var response = await httpClient.PostAsJsonAsync("Category/GetPagedCategories", input, AppJsonContext.Default.PagedInputDto);

                var data = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.PagedResultDtoCategoryDto);

                NumResults = data!.Total;

                return BitDataGridItemsProviderResult.From(data!.Items, data!.Total);
            }
            catch
            {
                return BitDataGridItemsProviderResult.From(new List<CategoryDto> { }, 0);
            }   
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }

        };
        return Task.CompletedTask;
    }

    private async Task RefreshData()
    {
       await dataGrid!.RefreshDataAsync();
    }


    private void CreateCategory()
    {
        navigationManager.NavigateTo("create-edit-category");
    }


    private Task EditCategory(CategoryDto Category)
    {
        navigationManager.NavigateTo($"create-edit-category/{Category!.Id}");
        return Task.CompletedTask;
    }

    private Task DeleteCategory(CategoryDto Category)
    {
        ConfirmMessageBox.Show("Are you sure delete?", Category.Name, "Delete", async (confirmed) =>
        {
            if (confirmed)
            {
                await httpClient.DeleteAsync($"Category/{Category.Id}");
                await RefreshData();
            }
        });
        return Task.CompletedTask;
    }
}




