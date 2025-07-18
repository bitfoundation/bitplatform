﻿using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;

namespace Boilerplate.Client.Core.Components.Pages.Home;

public partial class ProductsCarousel
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }
 
 
    [AutoInject] private IProductViewController productViewController = default!;


    private BitCarousel carouselRef = default!;
    private IEnumerable<ProductDto>? carouselProducts;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        carouselProducts = await productViewController
                                    .WithQuery(new ODataQuery 
                                    { 
                                        Top = 6, 
                                        OrderBy = nameof(ProductDto.Name) 
                                    })
                                    .Get(CurrentCancellationToken);
    }


    private string? GetProductImageUrl(ProductDto product) => product.GetPrimaryMediumImageUrl(AbsoluteServerAddress);
}
