//+:cnd:noEmit
using Boilerplate.Server.Api.SignalR;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
//#endif
using Boilerplate.Shared.Dtos.Categories;
using Boilerplate.Shared.Controllers.Categories;

namespace Boilerplate.Server.Api.Controllers.Categories;

[ApiController, Route("api/[controller]/[action]"), Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
public partial class CategoryController : AppControllerBase, ICategoryController
{
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif

    [HttpGet, EnableQuery]
    public IQueryable<CategoryDto> Get()
    {
        return DbContext.Categories
            .Project();
    }

    [HttpGet]
    public async Task<PagedResult<CategoryDto>> GetCategories(ODataQueryOptions<CategoryDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<CategoryDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip!.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top!.Value);

        return new PagedResult<CategoryDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }

    [HttpGet("{id}")]
    public async Task<CategoryDto> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (dto is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.CategoryCouldNotBeFound)]);

        return dto;
    }

    [HttpPost]
    public async Task<CategoryDto> Create(CategoryDto dto, CancellationToken cancellationToken)
    {
        var entityToAdd = dto.Map();

        await DbContext.Categories.AddAsync(entityToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalR == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif

        return entityToAdd.Map();
    }

    [HttpPut]
    public async Task<CategoryDto> Update(CategoryDto dto, CancellationToken cancellationToken)
    {
        var entityToUpdate = dto.Map();

        DbContext.Update(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalR == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif

        return entityToUpdate.Map();
    }

    [HttpDelete("{id}/{concurrencyStamp}")]
    public async Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken)
    {
        if (await DbContext.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
        {
            throw new BadRequestException(Localizer[nameof(AppStrings.CategoryNotEmpty)]);
        }

        DbContext.Categories.Remove(new() { Id = id, ConcurrencyStamp = Convert.FromBase64String(Uri.UnescapeDataString(concurrencyStamp)) });

        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalR == true)
        await PublishDashboardDataChanged(cancellationToken);
        //#endif
    }

    //#if (signalR == true)
    private async Task PublishDashboardDataChanged(CancellationToken cancellationToken)
    {
        // Checkout AppHubConnectionHandler's comments for more info.
        await appHubContext.Clients.GroupExcept("AuthenticatedClients", excludedConnectionIds: [User.GetSessionId().ToString()]).SendAsync(SignalREvents.PUBLISH_MESSAGE, SharedPubSubMessages.DASHBOARD_DATA_CHANGED, cancellationToken);
    }
    //#endif
}

