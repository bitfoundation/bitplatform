using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
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

    /// <summary>Unguarded on purpose: nothing reaches a tool here without the endpoint's global-admin + 2FA authorization.</summary>
    private Guid CallerId => httpContextAccessor.HttpContext!.User.GetUserId();

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
                var (query, computedValueError) = ApplyFilter(SetOf(entityType), entityType, filter);
                if (computedValueError is not null)
                    return DevMcpJson.Serialize(new { Error = computedValueError });

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

    [McpServerTool(Name = nameof(AggregateEntity))]
    [Description("Read-only counts, sums, averages and the smallest and largest values of an entity's rows, which QueryEntity cannot give without paging through them. Name an entity (CLR type, e.g. User, Product), optional columns to group by, optional aggregates and an optional filter. matchingRows is how many rows the filter matched, so a group's count over it is that group's share. Each row of the result has the group's columns, its Count and one value per aggregate, named like SumOfAmount or CountDistinctOfUserId; a column that is null is left out of its row. Enums come back as numbers. Groups are ordered by Count, largest first, and at most 100 are returned; truncated is true when there were more. Global query filters are IGNORED, tenant filters included, as in QueryEntity, so the numbers span every tenant; add a TenantId term to narrow them to one. Credential-shaped properties are rejected as a group, an aggregate's column or a filter operand. Command timeout is 15 seconds.")]
    public async Task<string> AggregateEntity(
        [Required, Description("CLR type name, e.g. User or Product")] string entity,
        [Description("Columns to group by, e.g. PlatformType. Scalar properties of the entity only. Empty: one result for all the matching rows.")] string[]? groupBy = null,
        [Description("What to work out besides Count: Sum(Column), Average(Column), Min(Column), Max(Column) or CountDistinct(Column), e.g. CountDistinct(UserId).")] string[]? aggregates = null,
        [Description("Dynamic LINQ filter, e.g. CreatedOn > DateTime(2024,1,1). Evaluated with global query filters ignored, so it is matched against every tenant's rows; add a TenantId term to narrow it to one.")] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        var entityType = FindEntity(entity);
        if (entityType is null)
            return DevMcpJson.Serialize(new { Error = $"Unknown entity '{entity}'. Call GetDatabaseSchema for the list." });

        var keys = (groupBy ?? []).Where(column => string.IsNullOrWhiteSpace(column) is false).Select(column => column.Trim()).ToArray();

        var columns = new List<string>(keys);
        var projection = keys.Select(column => $"Key.{column} as {column}").Append("Count() as Count").ToList();

        foreach (var aggregate in aggregates ?? [])
        {
            var match = AggregateShape().Match(aggregate ?? "");
            if (match.Success is false)
                return DevMcpJson.Serialize(new { Error = $"'{aggregate}' is not an aggregate. Use Sum(Column), Average(Column), Min(Column), Max(Column) or CountDistinct(Column)." });

            var (function, column) = (match.Groups[1].Value, match.Groups[2].Value);

            columns.Add(column);
            projection.Add($"{(function is "CountDistinct" ? $"Select({column}).Distinct().Count()" : $"{function}({column})")} as {function}Of{column}");
        }

        var error = DevMcpQueryGuards.ValidateAggregate(entityType, columns, filter);
        if (error is not null)
            return DevMcpJson.Serialize(new { Error = error });

        try
        {
            return await DevMcpReadOnly.ReadAsync(db, async token =>
            {
                var (query, computedValueError) = ApplyFilter(SetOf(entityType), entityType, filter);
                if (computedValueError is not null)
                    return DevMcpJson.Serialize(new { Error = computedValueError });

                var matchingRows = await query.Select(ParsingConfig, "1").Cast<int>().CountAsync(token);

                List<dynamic> rows = [];

                // Nothing to group or work out: matchingRows is the whole answer.
                if (columns.Count > 0)
                {
                    var key = keys.Length is 0 ? "1" : "new(" + string.Join(", ", keys.Select(column => $"{column} as {column}")) + ")";

                    rows = await query.GroupBy(ProjectionConfig, key)
                        .OrderBy(ParsingConfig, "Count() desc")
                        .Select(ProjectionConfig, "new(" + string.Join(", ", projection) + ")")
                        .Take(DevMcpLimits.MaxTake + 1)
                        .ToDynamicListAsync(token);
                }

                var truncated = rows.Count > DevMcpLimits.MaxTake;
                if (truncated)
                    rows.RemoveAt(DevMcpLimits.MaxTake);

                logger.LogInformation("Dev MCP aggregated {MatchingRows} {Entity} rows into {GroupCount} groups for {UserId}. GroupBy: {GroupBy}. Aggregates: {Aggregates}. Filter: {Filter}.",
                    matchingRows, entityType.ClrType.Name, rows.Count, CallerId, keys, aggregates, filter);

                var payload = DevMcpJson.Serialize(new
                {
                    Entity = entityType.ClrType.Name,
                    Table = entityType.GetTableName(),
                    Schema = entityType.GetSchema(),
                    MatchingRows = matchingRows,
                    Truncated = truncated,
                    Rows = rows
                });

                if (Encoding.UTF8.GetByteCount(payload) > DevMcpLimits.MaxPayloadBytes)
                    return DevMcpJson.Serialize(new { Error = $"Result exceeded {DevMcpLimits.MaxPayloadBytes} bytes. Narrow the filter or the groups." });

                return payload;
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            return DevMcpJson.Serialize(new { Error = exception.Message });
        }
    }

    private static (IQueryable Query, string? Error) ApplyFilter(IQueryable query, IEntityType entityType, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return (query, null);

        var predicate = DynamicExpressionParser.ParseLambda(ParsingConfig, false, entityType.ClrType, typeof(bool), filter);

        var computedValueError = DevMcpQueryGuards.RejectComputedValues(predicate);

        return computedValueError is null ? (query.Where(predicate), null) : (query, computedValueError);
    }

    [GeneratedRegex(@"^\s*(CountDistinct|Sum|Average|Min|Max)\(\s*([_A-Za-z][_A-Za-z0-9]*)\s*\)\s*$")]
    private static partial Regex AggregateShape();

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
