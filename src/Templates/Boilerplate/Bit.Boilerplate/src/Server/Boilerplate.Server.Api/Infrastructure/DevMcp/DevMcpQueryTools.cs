using System.ComponentModel;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using ModelContextProtocol.Server;
using System.Linq.Dynamic.Core;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

[Authorize(Policy = AppFeatures.System.DevMcp)]
public partial class DevMcpQueryTools
{
    [AutoInject] private AppDbContext db = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private ILogger<DevMcpQueryTools> logger = default!;

    private Guid? CallerId => httpContextAccessor.HttpContext?.User is { } user && user.IsAuthenticated() ? user.GetUserId() : null;

    private static readonly ParsingConfig ParsingConfig = new()
    {
        UseParameterizedNamesInDynamicQuery = true,
        ResolveTypesBySimpleName = false,
        DisallowNewKeyword = true
    };

    // Select needs `new(...)`, which we build from validated column names. User-supplied filter/orderBy use ParsingConfig.
    private static readonly ParsingConfig ProjectionConfig = new()
    {
        UseParameterizedNamesInDynamicQuery = true,
        ResolveTypesBySimpleName = false
    };

    [McpServerTool(Name = nameof(QueryEntity))]
    [Description("Read-only Dynamic LINQ over the EF Core model, not SQL. Name an entity (CLR type, e.g. User, Product), a required projection (column names of that entity; selecting everything is refused), an optional filter expression, optional orderBy, skip and take. Global query filters are IGNORED, tenant filters included: the caller is a global admin who has no tenant selected - and signing in through an external provider leaves no way to switch into one - so a filtered read would quietly answer for a single tenant, or for none. Rows therefore span every tenant; project TenantId when that matters. Password hashes, security stamps, tokens, WebAuthn credentials, data-protection keys and other credential-shaped properties are rejected as a projection, filter operand or ordering key. take is capped at 100. Command timeout is 15 seconds. Results larger than 256KB are refused. Hangfire's own tables are queryable here, but GetHangfireStats and ListHangfireJobs read them through Hangfire's monitoring API and are correct on isolated storage too.")]
    public async Task<string> QueryEntity(
        [Required, Description("CLR type name, e.g. User or Product")] string entity,
        [Description("Columns to return. Required. Scalar properties of the entity only.")] string[]? select = null,
        [Description("Dynamic LINQ filter, e.g. Email == \"a@b.c\" && CreatedOn > DateTime(2024,1,1). Evaluated with global query filters ignored, so it is matched against every tenant's rows; add a TenantId term to narrow it to one.")] string? filter = null,
        [Description("Dynamic LINQ order, e.g. CreatedOn desc. Defaults to the primary key.")] string? orderBy = null,
        [Description("Rows to skip")] int skip = 0,
        [Description("Page size, 1-100")] int take = 25,
        CancellationToken cancellationToken = default)
    {
        var entityType = FindEntity(entity);
        if (entityType is null)
            return DevMcpJson.Serialize(new { Error = $"Unknown entity '{entity}'. Call GetDatabaseSchema for the list." });

        var error = DevMcpQueryGuards.Validate(entityType, select ?? [], filter, orderBy);
        if (error is not null)
            return DevMcpJson.Serialize(new { Error = error });

        take = Math.Clamp(take, 1, DevMcpLimits.MaxTake);
        skip = Math.Max(skip, 0);

        if (string.IsNullOrWhiteSpace(orderBy))
        {
            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey is null)
                return DevMcpJson.Serialize(new { Error = $"Entity '{entityType.ClrType.Name}' has no primary key to order by." });
            orderBy = string.Join(", ", primaryKey.Properties.Select(property => property.Name));
        }

        try
        {
            return await DevMcpReadOnly.ReadAsync(db, async token =>
            {
                IQueryable query = SetOf(entityType);
                if (string.IsNullOrWhiteSpace(filter) is false)
                    query = query.Where(ParsingConfig, filter);

                query = query.OrderBy(ParsingConfig, orderBy);

                var projection = "new(" + string.Join(", ", select!.Select(column => $"{column} as {column}")) + ")";
                query = query.Select(ProjectionConfig, projection);

                var rows = await query.Skip(skip).Take(take).ToDynamicListAsync(token);

                logger.LogInformation("Dev MCP read {RowCount} {Entity} rows for {UserId}. Select: {Select}. Filter: {Filter}. OrderBy: {OrderBy}. Skip: {Skip}. Take: {Take}.",
                    rows.Count, entityType.ClrType.Name, CallerId, select, filter, orderBy, skip, take);

                var payload = DevMcpJson.Serialize(new
                {
                    Entity = entityType.ClrType.Name,
                    Table = entityType.GetTableName(),
                    Schema = entityType.GetSchema(),
                    skip,
                    take,
                    Count = rows.Count,
                    Rows = rows
                });

                if (Encoding.UTF8.GetByteCount(payload) > DevMcpLimits.MaxPayloadBytes)
                    return DevMcpJson.Serialize(new { Error = $"Result exceeded {DevMcpLimits.MaxPayloadBytes} bytes. Narrow the projection or take." });

                return payload;
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            return DevMcpJson.Serialize(new { Error = exception.Message });
        }
    }

    private IEntityType? FindEntity(string name)
        => db.Model.GetEntityTypes()
            .FirstOrDefault(entity => entity.ClrType is not null
                && entity.IsOwned() is false
                && string.Equals(entity.ClrType.Name, name, StringComparison.OrdinalIgnoreCase));

    private IQueryable SetOf(IEntityType entityType)
        => (IQueryable)typeof(DevMcpQueryTools)
            .GetMethod(nameof(SetOfCore), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(entityType.ClrType)
            .Invoke(this, null)!;

    // IgnoreQueryFilters: a global admin has no tenant selected, so the tenant filter would answer for the fallback
    // tenant or for nothing at all - and an external sign-in cannot switch tenant to work around it.
    private IQueryable<TEntity> SetOfCore<TEntity>() where TEntity : class
        => db.Set<TEntity>().AsNoTracking().IgnoreQueryFilters();
}
