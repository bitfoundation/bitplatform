//+:cnd:noEmit
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
//#endif
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Server.Api.Models.Products;
using Boilerplate.Shared.Controllers.Products;

namespace Boilerplate.Server.Api.Controllers.Products;

[ApiController, Route("api/[controller]/[action]")]
[Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
public partial class ProductController : AppControllerBase, IProductController
{
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif
    [AutoInject] private ResponseCacheService responseCacheService = default!;

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

        query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

        return new PagedResult<ProductDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ProductDto> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        return dto;
    }

    [HttpPost]
    public async Task<ProductDto> Create(ProductDto dto, CancellationToken cancellationToken)
    {
        var entityToAdd = dto.Map();

        await DbContext.Products.AddAsync(entityToAdd, cancellationToken);

        await Validate(entityToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalR == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif

        return entityToAdd.Map();
    }

    [HttpPut]
    public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
    {
        var entityToUpdate = await DbContext.Products.FindAsync([dto.Id], cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        dto.Patch(entityToUpdate);

        await Validate(entityToUpdate, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        await responseCacheService.PurgeCache("/", $"product/{dto.Id}", $"api/ProductView/Get/{dto.Id}" /*You can also use Url.Action to build urls.*/);

        //#if (signalR == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif

        return entityToUpdate.Map();
    }

    [HttpDelete("{id}/{concurrencyStamp}")]
    public async Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken)
    {
        DbContext.Products.Remove(new() { Id = id, ConcurrencyStamp = Convert.FromHexString(concurrencyStamp) });

        await DbContext.SaveChangesAsync(cancellationToken);

        await responseCacheService.PurgeCache("/", $"product/{id}", $"api/ProductView/Get/{id}" /*You can also use Url.Action to build urls.*/);

        //#if (signalR == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif
    }

    //#if (signalR == true)
    private async Task PublishDashboardDataChanged(CancellationToken cancellationToken)
    {
        // Checkout AppHub's comments for more info.
        // In order to exclude current user session, gets its signalR connection id from database and use GroupExcept instead.
        await appHubContext.Clients.Group("AuthenticatedClients").SendAsync(SignalREvents.PUBLISH_MESSAGE, SharedPubSubMessages.DASHBOARD_DATA_CHANGED, null, cancellationToken);
    }
    //#endif

    private async Task Validate(Product product, CancellationToken cancellationToken)
    {
        // Remote validation example: Any errors thrown here will be displayed in the client's edit form component.
        if (DbContext.Entry(product).Property(c => c.Name).IsModified
            && await DbContext.Products.AnyAsync(p => p.Name == product.Name, cancellationToken: cancellationToken))
            throw new ResourceValidationException((nameof(ProductDto.Name), [Localizer[nameof(AppStrings.DuplicateProductName)]]));
    }
}

