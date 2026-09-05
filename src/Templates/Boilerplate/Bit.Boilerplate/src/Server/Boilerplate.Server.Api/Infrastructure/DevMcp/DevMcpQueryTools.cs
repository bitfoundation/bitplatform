using System.ComponentModel;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using ModelContextProtocol.Server;
using System.Linq.Dynamic.Core;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

[Authorize(Policy = AppFeatures.System.DevMcp)]
public sealed class DevMcpQueryTools(AppDbContext db, DevMcpAuditContext audit)
{
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
    [Description("Read-only Dynamic LINQ over the EF Core model, not SQL. Name an entity (CLR type, e.g. User, Product), a required projection (column names of that entity; selecting everything is refused), an optional filter expression, optional orderBy, skip and take. The provider generates SQL and query filters stay on: tenant-filtered and any other global filters are applied, so a result set is not 'all rows in the table'. IgnoreQueryFilters is not offered. Password hashes, security stamps, tokens, WebAuthn credentials, data-protection keys and other credential-shaped properties are rejected as a projection, filter operand or ordering key. take is capped at 100. Command timeout is 15 seconds. Results larger than 256KB are refused. Hangfire's jobs schema cannot be queried here.")]
    public async Task<string> QueryEntity(
        [Required, Description("CLR type name, e.g. User or Product")] string entity,
        [Description("Columns to return. Required. Scalar properties of the entity only.")] string[]? select = null,
        [Description("Dynamic LINQ filter, e.g. Email == \"a@b.c\" && CreatedOn > DateTime(2024,1,1). Query filters still apply.")] string? filter = null,
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
            await using var _ = await DevMcpReadOnly.BeginAsync(db, cancellationToken);

            IQueryable query = SetOf(entityType);
            if (string.IsNullOrWhiteSpace(filter) is false)
                query = query.Where(ParsingConfig, filter);

            query = query.OrderBy(ParsingConfig, orderBy);

            var projection = "new(" + string.Join(", ", select!.Select(column => $"{column} as {column}")) + ")";
            query = query.Select(ProjectionConfig, projection);

            var rows = await query.Skip(skip).Take(take).ToDynamicListAsync(cancellationToken);
            audit.RowCount = rows.Count;

            var payload = DevMcpJson.Serialize(new
            {
                Entity = entityType.ClrType.Name,
                Table = entityType.GetTableName(),
                Schema = entityType.GetSchema(),
                QueryFiltersApplied = true,
                IgnoreQueryFilters = false,
                skip,
                take,
                Count = rows.Count,
                Rows = rows
            });

            if (Encoding.UTF8.GetByteCount(payload) > DevMcpLimits.MaxPayloadBytes)
                return DevMcpJson.Serialize(new { Error = $"Result exceeded {DevMcpLimits.MaxPayloadBytes} bytes. Narrow the projection or take." });

            return payload;
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

    private IQueryable<TEntity> SetOfCore<TEntity>() where TEntity : class => db.Set<TEntity>().AsNoTracking();
}
