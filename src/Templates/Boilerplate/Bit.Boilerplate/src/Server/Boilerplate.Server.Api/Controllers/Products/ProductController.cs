//+:cnd:noEmit
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
//#endif
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Server.Api.Models.Products;
using Boilerplate.Shared.Controllers.Products;
using Ganss.Xss;

namespace Boilerplate.Server.Api.Controllers.Products;

[ApiController, Route("api/[controller]/[action]")]
[Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
[Authorize(Policy = AppFeatures.AdminPanel.ManageProductCatalog)]
public partial class ProductController : AppControllerBase, IProductController
{
    [AutoInject] private HtmlSanitizer htmlSanitizer = default!;

    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif
    //#if (database == "PostgreSQL" || database == "SqlServer")
    [AutoInject] private ProductEmbeddingService productEmbeddingService = default!;
    //#endif
    [AutoInject] private ResponseCacheService responseCacheService = default!;

    [HttpGet, EnableQuery]
    public IQueryable<ProductDto> Get()
    {
        return DbContext.Products
            .Project();
    }

    [HttpGet]
    public async Task<PagedResponse<ProductDto>> GetProducts(ODataQueryOptions<ProductDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<ProductDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

        return new PagedResponse<ProductDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }

    [HttpGet("{searchQuery}")]
    public async Task<PagedResponse<ProductDto>> SearchProducts(string searchQuery, ODataQueryOptions<ProductDto> odataQuery, CancellationToken cancellationToken)
    {
        //#if (database == "PostgreSQL" || database == "SqlServer")
        var query = (IQueryable<ProductDto>)odataQuery.ApplyTo((await (productEmbeddingService.SearchProducts(searchQuery, cancellationToken))).Project(),
            ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip | AllowedQueryOptions.OrderBy /* Ordering can disrupt the results of the embedding service. */);
        var totalCount = await query.LongCountAsync(cancellationToken);

        query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

        return new PagedResponse<ProductDto>(await query.ToArrayAsync(cancellationToken), totalCount);
        //#else
        throw new NotImplementedException(); // Embedding based search is only implemented for PostgreSQL and SQL Server only.
        //#endif
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
        dto.DescriptionHTML = htmlSanitizer.Sanitize(dto.DescriptionHTML ?? string.Empty);

        var entityToAdd = dto.Map();

        await DbContext.Products.AddAsync(entityToAdd, cancellationToken);

        await Validate(entityToAdd, cancellationToken);

        //#if (database == "PostgreSQL" || database == "SqlServer")
        //#if (IsInsideProjectTemplate == true)
        if (DbContext.Database.ProviderName!.EndsWith("PostgreSQL", StringComparison.InvariantCulture) ||
            DbContext.Database.ProviderName!.EndsWith("SqlServer", StringComparison.InvariantCulture))
        {
            //#endif
            await productEmbeddingService.Embed(entityToAdd, cancellationToken);
            //#if (IsInsideProjectTemplate == true)
        }
        //#endif
        //#endif

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalR == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif

        return entityToAdd.Map();
    }

    [HttpPut]
    public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
    {
        dto.DescriptionHTML = htmlSanitizer.Sanitize(dto.DescriptionHTML ?? string.Empty);

        var entityToUpdate = await DbContext.Products.FindAsync([dto.Id], cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        dto.Patch(entityToUpdate);

        await Validate(entityToUpdate, cancellationToken);

        //#if (database == "PostgreSQL" || database == "SqlServer")
        //#if (IsInsideProjectTemplate == true)
        if (DbContext.Database.ProviderName!.EndsWith("PostgreSQL", StringComparison.InvariantCulture) ||
            DbContext.Database.ProviderName!.EndsWith("SqlServer", StringComparison.InvariantCulture))
        {
            //#endif
            await productEmbeddingService.Embed(entityToUpdate, cancellationToken);
            //#if (IsInsideProjectTemplate == true)
        }
        //#endif
        //#endif

        await DbContext.SaveChangesAsync(cancellationToken);

        await responseCacheService.PurgeProductCache(entityToUpdate.ShortId);

        //#if (signalR == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif

        return entityToUpdate.Map();
    }

    [HttpDelete("{id}/{version}")]
    public async Task Delete(Guid id, string version, CancellationToken cancellationToken)
    {
        var entityToDelete = await DbContext.Products.FindAsync([id], cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        entityToDelete.Version = Convert.FromHexString(version);

        DbContext.Remove(entityToDelete);

        await DbContext.SaveChangesAsync(cancellationToken);

        await responseCacheService.PurgeProductCache(entityToDelete.ShortId);

        //#if (signalR == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif
    }

    //#if (signalR == true)
    private async Task PublishDashboardDataChanged(CancellationToken cancellationToken)
    {
        // Check out AppHub's comments for more info.
        // In order to exclude current user session, gets its signalR connection id from database and use GroupExcept instead.
        await appHubContext.Clients.Group("AuthenticatedClients").Publish(SharedAppMessages.DASHBOARD_DATA_CHANGED, null, cancellationToken);
    }
    //#endif

    private async Task Validate(Product product, CancellationToken cancellationToken)
    {
        var entry = DbContext.Entry(product);
        // Remote validation example: Any errors thrown here will be displayed in the client's edit form component.
        if ((entry.State is EntityState.Added || entry.Property(c => c.Name).IsModified)
            && await DbContext.Products.AnyAsync(p => p.Name == product.Name, cancellationToken))
            throw new ResourceValidationException((nameof(ProductDto.Name), [Localizer[nameof(AppStrings.DuplicateProductName)]]));
    }
}

