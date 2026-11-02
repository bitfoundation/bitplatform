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
    /// <para>
    /// It also maps the get-only properties the serializer stores, which EF leaves out by convention: a
    /// type with no parameterless constructor is built through the constructor whose parameters all name
    /// properties, and the get-only properties that constructor takes are stored like any other. EF
    /// binds the same constructor once they are mapped.
    /// </para>
    /// </summary>
    private sealed class BlazeDbAttributeConvention : IEntityTypeAddedConvention
    {
        public void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder, IConventionContext<IConventionEntityTypeBuilder> context)
        {
            var clrType = entityTypeBuilder.Metadata.ClrType;
            var constructorBound = ConstructorBoundProperties(clrType);
            foreach (var property in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.IsDefined(typeof(BlazeDbIgnoreAttribute), inherit: true))
                {
                    entityTypeBuilder.Ignore(property.Name, fromDataAnnotation: true);
                    continue;
                }
                if (!property.CanWrite && constructorBound.Contains(property.Name))
                {
                    entityTypeBuilder.Property(property, fromDataAnnotation: true);
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
    /// The properties the generated reader passes to a constructor - mirroring the generator's choice:
    /// none when there is a parameterless constructor, otherwise those of the constructor with the most
    /// parameters that each name a property of the same type (a record's copy constructor excepted).
    /// </summary>
    internal static HashSet<string> ConstructorBoundProperties(Type clrType)
    {
        var bound = new HashSet<string>(StringComparer.Ordinal);
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        if (clrType.IsValueType || clrType.GetConstructor(Flags, Type.EmptyTypes) is not null)
        {
            return bound;
        }

        var properties = clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => !p.IsDefined(typeof(BlazeDbIgnoreAttribute), inherit: true))
            .ToArray();
        PropertyInfo? Match(ParameterInfo parameter) =>
            properties.FirstOrDefault(p => string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase) && p.PropertyType == parameter.ParameterType);

        var constructor = clrType.GetConstructors(Flags)
            .Where(c => !(c.GetParameters() is [var only] && only.ParameterType == clrType))
            .Where(c => c.GetParameters().All(parameter => Match(parameter) is not null))
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
        foreach (var parameter in constructor?.GetParameters() ?? [])
        {
            bound.Add(Match(parameter)!.Name);
        }
        return bound;
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
