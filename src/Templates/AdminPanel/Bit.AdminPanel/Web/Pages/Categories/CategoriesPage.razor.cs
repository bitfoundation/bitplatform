﻿//-:cnd:noEmit
using AdminPanel.Shared.Dtos.Categories;

namespace AdminPanel.App.Pages.Categories;

[Authorize]
public partial class CategoriesPage
{
    public bool IsLoading { get; set; }

    BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };
    BitDataGrid<CategoryDto>? dataGrid;
    BitDataGridItemsProvider<CategoryDto>? categoriesProvider;

    string _categoryNameFilter = string.Empty;
    string CategoryNameFilter
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
        await PrepareGridDataProvider();
        await base.OnInitAsync();
    }

    private async Task PrepareGridDataProvider()
    {
        categoriesProvider = async req =>
        {
            try
            {
                IsLoading = true;

                // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

                var query = new Dictionary<string, object>()
                {
                    { "$top", req.Count.HasValue ? req.Count.Value : 10 },
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

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultCategoryDto);

                return BitDataGridItemsProviderResult.From(data!.Items, (int)data!.TotalCount);
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
    }

    private async Task RefreshData()
    {
        await dataGrid!.RefreshDataAsync();
    }

    private void CreateCategory()
    {
        NavigationManager.NavigateTo("add-edit-category");
    }

    private void EditCategory(CategoryDto Category)
    {
        NavigationManager.NavigateTo($"add-edit-category/{Category!.Id}");
    }

    private async Task DeleteCategory(CategoryDto Category)
    {
        var confirmed = await ConfirmMessageBox.Show($"Are you sure you want to delete category \"{Category.Name}\"?", "Delete category");

        if (confirmed)
        {
            await HttpClient.DeleteAsync($"Category/Delete/{Category.Id}");
            await RefreshData();
        }
    }
}




