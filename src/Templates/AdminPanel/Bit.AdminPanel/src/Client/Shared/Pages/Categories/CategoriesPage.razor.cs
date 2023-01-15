//-:cnd:noEmit
using AdminPanel.Shared.Dtos.Categories;

namespace AdminPanel.Client.Shared.Pages;

[Authorize]
public partial class CategoriesPage
{
    private bool _isLoading;
    private string _categoryNameFilter = string.Empty;

    private BitDataGrid<CategoryDto>? _dataGrid;
    private BitDataGridItemsProvider<CategoryDto> _categoriesProvider = default!;
    private BitDataGridPaginationState _pagination = new() { ItemsPerPage = 10 };

    private string CategoryNameFilter
    {
        get => _categoryNameFilter;
        set
        {
            _categoryNameFilter = value;
            _ = RefreshData();
        }
    }

    protected override async Task OnInitAsync()
    {
        PrepareGridDataProvider();

        await base.OnInitAsync();
    }

    private void PrepareGridDataProvider()
    {
        _categoriesProvider = async req =>
        {
            _isLoading = true;

            try
            {
                // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview
                var query = new Dictionary<string, object?>()
                {
                    { "$top", req.Count ?? 10 },
                    { "$skip", req.StartIndex }
                };

                if (string.IsNullOrEmpty(_categoryNameFilter) is false)
                {
                    query.Add("$filter", $"contains(Name,'{_categoryNameFilter}')");
                }

                if (req.GetSortByProperties().Any())
                {
                    query.Add("$orderby", string.Join(", ", req.GetSortByProperties().Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}")));
                }

                var url = NavigationManager.GetUriWithQueryParameters("Category/GetCategories", query);

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultCategoryDto) ?? new();

                return BitDataGridItemsProviderResult.From(data.Items, (int)data.TotalCount);
            }
            catch
            {
                return BitDataGridItemsProviderResult.From(new List<CategoryDto> { }, 0);
            }
            finally
            {
                _isLoading = false;

                StateHasChanged();
            }
        };
    }

    private async Task RefreshData()
    {
        await _dataGrid!.RefreshDataAsync();
    }

    private void CreateCategory()
    {
        NavigationManager.NavigateTo("add-edit-category");
    }

    private void EditCategory(CategoryDto category)
    {
        NavigationManager.NavigateTo($"add-edit-category/{category.Id}");
    }

    private async Task DeleteCategory(CategoryDto category)
    {
        var confirmed = await ConfirmMessageBox.Show(Localizer.GetString(nameof(AppStrings.AreYouSureWannaDeleteCategory), category.Name ?? string.Empty), 
                                                     Localizer[nameof(AppStrings.DeleteCategory)]);

        if (confirmed)
        {
            await HttpClient.DeleteAsync($"Category/Delete/{category.Id}");

            await RefreshData();
        }
    }
}




