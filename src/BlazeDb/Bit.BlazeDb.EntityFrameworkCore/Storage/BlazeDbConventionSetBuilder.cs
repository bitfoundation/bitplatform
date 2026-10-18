using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Bit.BlazeDb.EntityFrameworkCore.Storage;

/// <summary>Model conventions, minus every relational one.</summary>
internal sealed class BlazeDbConventionSetBuilder : ProviderConventionSetBuilder
{
    public BlazeDbConventionSetBuilder(ProviderConventionSetBuilderDependencies dependencies) : base(dependencies)
    {
    }

    public override ConventionSet CreateConventionSet()
    {
        var conventionSet = base.CreateConventionSet();
        conventionSet.Add(new BlazeDbAttributeConvention());
        conventionSet.Add(new NoValueGenerationConvention());
        return conventionSet;
    }

    /// <summary>
    /// Reads the engine's own attributes into the model, so a table type needs no second set of
    /// EF annotations: <c>[Bit.BlazeDb.BlazeDbKey]</c> names the primary key - which EF's conventions would
    /// otherwise only find when it happens to be called <c>Id</c> - and <c>[Bit.BlazeDb.BlazeDbIgnore]</c>
    /// keeps a property out of the entity type, matching what the serializer does with it.
    /// </summary>
    private sealed class BlazeDbAttributeConvention : IEntityTypeAddedConvention
    {
        public void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder, IConventionContext<IConventionEntityTypeBuilder> context)
        {
            var clrType = entityTypeBuilder.Metadata.ClrType;
            foreach (var property in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.IsDefined(typeof(BlazeDbIgnoreAttribute), inherit: true))
                {
                    entityTypeBuilder.Ignore(property.Name, fromDataAnnotation: true);
                    continue;
                }
                if (property.IsDefined(typeof(BlazeDbKeyAttribute), inherit: true))
                {
                    var propertyBuilder = entityTypeBuilder.Property(property, fromDataAnnotation: true);
                    if (propertyBuilder is not null)
                    {
                        entityTypeBuilder.PrimaryKey([propertyBuilder.Metadata], fromDataAnnotation: true);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Turns off value generation across the model. The engine assigns nothing: a row's key is
    /// whatever the application put in it, which is also what makes a row's identity stable enough
    /// to hand the live object straight back from a query.
    /// </summary>
    private sealed class NoValueGenerationConvention : IModelFinalizingConvention
    {
        public void ProcessModelFinalizing(
            IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    property.Builder.ValueGenerated(ValueGenerated.Never);
                }
            }
        }
    }
}
