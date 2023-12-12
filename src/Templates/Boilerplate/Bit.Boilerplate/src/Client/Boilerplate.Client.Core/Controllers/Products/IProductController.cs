﻿using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Client.Core.Controllers.Product;

[Route("api/[controller]/[action]/")]
public interface IProductController : IAppController
{
    [HttpGet("{id}")]
    Task<ProductDto> Get(int id, CancellationToken cancellationToken = default);

    [HttpPost]
    Task<ProductDto> Create(ProductDto body, CancellationToken cancellationToken = default);

    [HttpPut]
    Task<ProductDto> Update(ProductDto body, CancellationToken cancellationToken = default);

    [HttpDelete("{id}")]
    Task Delete(int id, CancellationToken cancellationToken = default);

    [HttpGet]
    Task<PagedResult<ProductDto>> GetProducts(CancellationToken cancellationToken = default) => default!;
}
