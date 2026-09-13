using System.ComponentModel;
using Microsoft.EntityFrameworkCore.Metadata;
using ModelContextProtocol.Server;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

[Authorize(Policy = AppFeatures.System.DevMcp)]
public partial class DevMcpSchemaTools
{
    [AutoInject] private AppDbContext db = default!;

    [McpServerTool(Name = nameof(GetDatabaseSchema))]
    [Description("Describes the EF Core model this process is running, not information_schema. Includes entity CLR names, table and schema, properties (CLR and column types, nullability, keys), indexes, foreign keys, navigations and query filters. The queryFilters listed for an entity are what QueryEntity bypasses, since it reads with IgnoreQueryFilters - they tell you which column (TenantId, usually) the rows would otherwise have been narrowed by. hangfireStorage marks the entities backing Hangfire's own tables: they are queryable, but GetHangfireStats and ListHangfireJobs read them through Hangfire's monitoring API and stay correct on isolated storage. Optional entityName limits the result to one CLR type.")]
    // Synchronous because it reads the EF model rather than information_schema, so it opens no connection at all.
    public string GetDatabaseSchema(
        [Description("Optional CLR type name, e.g. User or Product")] string? entityName = null)
    {
        var entities = db.Model.GetEntityTypes()
            .Where(entity => entity.ClrType is not null && entity.IsOwned() is false)
            .Where(entity => string.IsNullOrWhiteSpace(entityName) || string.Equals(entity.ClrType.Name, entityName, StringComparison.OrdinalIgnoreCase))
            .Select(Describe)
            .ToArray();

        if (string.IsNullOrWhiteSpace(entityName) is false && entities.Length == 0)
            return DevMcpJson.Serialize(new { Error = $"Unknown entity '{entityName}'." });

        return DevMcpJson.Serialize(new
        {
            EntityCount = entities.Length,
            Entities = entities
        });
    }

    [McpServerTool(Name = nameof(GetAppliedMigrations))]
    [Description("Lists EF Core migrations applied to this database, with the latest one first. Pending migrations (in the assembly but not applied) are listed separately. This is what 'did that migration actually run in production' is answered with.")]
    public async Task<string> GetAppliedMigrations(CancellationToken cancellationToken)
    {
        return await DevMcpReadOnly.ReadMetadataAsync(db, async token =>
        {
            var canConnect = await db.Database.CanConnectAsync(token);

            string[] applied = [], pending = [];
            string? historyUnavailable = null;

            try
            {
                applied = [.. (await db.Database.GetAppliedMigrationsAsync(token)).Reverse()];
                pending = [.. await db.Database.GetPendingMigrationsAsync(token)];
            }
            catch (Exception exception)
            {
                // An EnsureCreated deployment has no __EFMigrationsHistory at all, and providers disagree about
                // whether reading it then throws or answers empty.
                historyUnavailable = exception.Message;
            }

            return DevMcpJson.Serialize(new
            {
                CanConnect = canConnect,
                Latest = applied.FirstOrDefault(),
                AppliedCount = applied.Length,
                Applied = applied,
                Pending = pending,
                HistoryUnavailable = historyUnavailable,
                Note = "EnsureCreated deployments have an empty applied list even though the schema exists."
            });
        }, cancellationToken);
    }

    private static object Describe(IEntityType entity)
    {
        var primaryKey = entity.FindPrimaryKey();
        return new
        {
            Entity = entity.ClrType.Name,
            ClrType = entity.ClrType.FullName,
            Table = entity.GetTableName(),
            Schema = entity.GetSchema(),
            HangfireStorage = DevMcpForbiddenColumns.IsHangfireStorage(entity),
            QueryFilters = entity.GetDeclaredQueryFilters().Select(filter => filter.Expression?.ToString()).Where(expression => expression is not null),
            Properties = entity.GetProperties().Select(property => new
            {
                property.Name,
                ClrType = property.ClrType.Name,
                Column = property.GetColumnName(),
                ColumnType = property.GetColumnType(),
                property.IsNullable,
                IsKey = property.IsKey(),
                IsPrimaryKey = primaryKey?.Properties.Contains(property) is true,
                IsConcurrencyToken = property.IsConcurrencyToken,
                IsShadowProperty = property.IsShadowProperty()
            }),
            Indexes = entity.GetIndexes().Select(index => new
            {
                Properties = index.Properties.Select(property => property.Name),
                index.IsUnique,
                Name = index.GetDatabaseName()
            }),
            ForeignKeys = entity.GetForeignKeys().Select(fk => new
            {
                Properties = fk.Properties.Select(property => property.Name),
                PrincipalEntity = fk.PrincipalEntityType.ClrType.Name,
                PrincipalProperties = fk.PrincipalKey.Properties.Select(property => property.Name),
                DeleteBehavior = fk.DeleteBehavior.ToString()
            }),
            Navigations = entity.GetNavigations().Select(navigation => new
            {
                navigation.Name,
                Target = navigation.TargetEntityType.ClrType.Name,
                navigation.IsCollection
            })
        };
    }
}
