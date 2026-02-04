using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Boilerplate.Server.Api.Infrastructure.Data.Configurations;

public partial class PostgreSQLPrimaryKeySequentialGuidDefaultValueConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var prop in entityType.GetKeys().SelectMany(k => k.Properties).Where(p => p.ClrType == typeof(Guid)))
            {
                prop.SetDefaultValueSql("uuidv7()");
            }
        }
    }
}
