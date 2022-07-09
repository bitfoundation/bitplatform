﻿using AdminPanelTemplate.Shared.Dtos.Categories;
using AdminPanelTemplate.Shared.Dtos.Products;

namespace AdminPanelTemplate.App.Pages.Products;

public partial class CreateEditProductModal
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    [Parameter]
    public ProductDto Product { get; set; }

    [Parameter]
    public EventCallback OnProductSave { get; set; }

    private bool IsOpen { get; set; }
    public bool IsLoading { get; private set; }
    public List<BitDropDownItem>? AllCategoryList { get; set; } = new();

    public string SelectedCategoyId { get => Product!.CategoryId!.ToString(); set { Product.CategoryId = int.Parse(value); } }

    protected override async void OnInitialized()
    {
        AllCategoryList =await GetCategoryDropdownItemsAsync();
        base.OnInitialized();
    }

    public void ShowModal(ProductDto product)
    {
        InvokeAsync(() =>
        {
            IsOpen = true;

            Product = product;

            StateHasChanged();
        });
    }

    private async Task<List<BitDropDownItem>> GetCategoryDropdownItemsAsync()
    {
        IsLoading = true;
        try
        {
            var categoryList = await stateService.GetValue($"{nameof(ProductsPage)}-{nameof(AllCategoryList)}", async () => await httpClient.GetFromJsonAsync("Category", AppJsonContext.Default.ListCategoryDto));

            return categoryList!.Select(c => new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = c.Name!,
                Value = c.Id!.ToString()
            }).ToList();
          
        }
        finally
        {
            IsLoading = false;
        }


    }


    private async Task Save()
    {
        if (IsLoading)
        {
            return;
        }

        
        try
        {
            if (Product.Id == 0)
            {
                await httpClient.PostAsJsonAsync("Product", Product, AppJsonContext.Default.ProductDto);
            }
            else
            {
                 await httpClient.PutAsJsonAsync("Product", Product, AppJsonContext.Default.ProductDto);
            }

            IsOpen = false;
            await OnProductSave.InvokeAsync();
        }
        finally
        {
            
        }


        
    }



    private void OnCloseClick()
    {
        IsOpen = false;
    }





}
