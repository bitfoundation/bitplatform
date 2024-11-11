//+:cnd:noEmit
using Boilerplate.Server.Api.Signalr;
//#if (signalr == true)
using Microsoft.AspNetCore.SignalR;
//#endif
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Product;

namespace Boilerplate.Server.Api.Controllers;

[ApiController, Route("api/[controller]/[action]")]
public partial class ProductController : AppControllerBase, IProductController
{
    //#if (signalr == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif

    [HttpGet, EnableQuery]
    public IQueryable<ProductDto> Get()
    {
        return DbContext.Products
            .Project();
    }

    [HttpGet]
    public async Task<PagedResult<ProductDto>> GetProducts(ODataQueryOptions<ProductDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<ProductDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip!.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top!.Value);

        return new PagedResult<ProductDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ProductDto> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (dto is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        return dto;
    }

    [HttpPost]
    public async Task<ProductDto> Create(ProductDto dto, CancellationToken cancellationToken)
    {
        var entityToAdd = dto.Map();

        await DbContext.Products.AddAsync(entityToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalr == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif

        return entityToAdd.Map();
    }

    [HttpPut]
    public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
    {
        var entityToUpdate = dto.Map();

        DbContext.Update(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalr == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif

        return entityToUpdate.Map();
    }

    [HttpDelete("{id}/{concurrencyStamp}")]
    public async Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken)
    {
        DbContext.Products.Remove(new() { Id = id, ConcurrencyStamp = Convert.FromBase64String(Uri.UnescapeDataString(concurrencyStamp)) });

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalr == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif
    }

    //#if (signalr == true)
    private async Task PublishDashboardDataChanged(CancellationToken cancellationToken)
    {
        await appHubContext.Clients.AllExcept(User.GetUserId().ToString()).SendAsync(SharedPubSubMessages.DASHBOARD_DATA_CHANGED, cancellationToken);
    }
    //#endif
}

