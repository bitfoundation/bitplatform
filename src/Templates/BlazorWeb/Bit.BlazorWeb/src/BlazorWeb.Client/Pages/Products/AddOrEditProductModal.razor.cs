﻿using BlazorWeb.Shared.Dtos.Products;

namespace BlazorWeb.Client.Pages.Products;

public partial class AddOrEditProductModal
{

    private bool _isOpen;
    private bool _isLoading;
    private bool _isSaving;
    private ProductDto _product = new();
    private List<BitDropdownItem<string>> _allCategoryList = [];
    private string _selectedCategoyId = string.Empty;

    [Parameter] public EventCallback OnSave { get; set; }

    protected override async Task OnInitAsync()
    {
        await LoadAllCategoriesAsync();
    }

    public async Task ShowModal(ProductDto product)
    {
        await InvokeAsync(() =>
        {
            _ = JSRuntime.SetBodyOverflow(true);

            _isOpen = true;
            _product = product;
            _selectedCategoyId = (_product.CategoryId ?? 0).ToString();

            StateHasChanged();
        });
    }

    private async Task LoadAllCategoriesAsync()
    {
        _isLoading = true;

        try
        {
            var categoryList = await PrerenderStateService.GetValue($"{nameof(ProductsPage)}-AllCategoryList",
                                        async () => await HttpClient.GetFromJsonAsync("Category/Get",
                                            AppJsonContext.Default.ListCategoryDto)) ?? [];

            _allCategoryList = categoryList.Select(c => new BitDropdownItem<string>()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = c.Name ?? string.Empty,
                Value = c.Id.ToString()
            }).ToList();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task Save()
    {
        if (_isLoading) return;

        _isSaving = true;

        try
        {
            if (_product.Id == 0)
            {
                await HttpClient.PostAsJsonAsync("Product/Create", _product, AppJsonContext.Default.ProductDto);
            }
            else
            {
                await HttpClient.PutAsJsonAsync("Product/Update", _product, AppJsonContext.Default.ProductDto);
            }

            _isOpen = false;

            await JSRuntime.SetBodyOverflow(false);
        }
        finally
        {
            _isSaving = false;

            await OnSave.InvokeAsync();
        }
    }

    private async Task OnCloseClick()
    {
        _isOpen = false;

        await JSRuntime.SetBodyOverflow(false);
    }
}
