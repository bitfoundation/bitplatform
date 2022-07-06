//-:cnd:noEmit
using System.Text.Json;
using AdminPanelTemplate.Shared.Dtos.Categories;

namespace AdminPanelTemplate.App.Pages;
public partial class CategoriesPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    public bool IsLoading { get; set; }

    BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };
    BitDataGrid<CategoryDto>? dataGrid;
    BitDataGridItemsProvider<CategoryDto> categoriesProvider;

    int NumResults;
    string _categoryNameFilter = string.Empty;
    string CategoryNameFilter
    {
        get => _categoryNameFilter;
        set
        {
            _categoryNameFilter = value;
            _ = dataGrid.RefreshDataAsync();
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
                var input = new PagedInputDto()
                {
                    Skip = req.StartIndex,
                    MaxResultCount = (req.Count.HasValue) ? req.Count.Value : 10,
                    Filter = _categoryNameFilter,
                    SortBy = req.SortByColumn?.Title,
                    SortAscending = req.SortByAscending
                };

                var response = await httpClient.PostAsJsonAsync("Category/GetCategories", input, AppJsonContext.Default.PagedInputDto);

                var data = JsonSerializer.Deserialize(response.Content.ReadAsStream(), AppJsonContext.Default.PagedResultDtoCategoryDto);

                NumResults = data!.Total;

                return BitDataGridItemsProviderResult.From(data!.Items, data!.Total);
            }
            catch(Exception ex)
            {
                return BitDataGridItemsProviderResult.From<CategoryDto>(new List<CategoryDto> { }, 0);
            }   
            finally
            {
                StateHasChanged();
            }

        };

    }
}




