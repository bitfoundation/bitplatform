using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bit.BlazeDb.EntityFrameworkCore.Metadata;

/// <summary>
/// Resolves the <see cref="BlazeDbTableDescriptor{TKey,TRow}"/> for an entity type. Descriptors are
/// generated as a static <c>Table</c> member on each <c>[BlazeDbTable]</c> type, so the convention is to
/// look for that; <c>UseBlazeDbTable</c> overrides it.
/// </summary>
internal static class BlazeDbTableResolver
{
    public const string DescriptorAnnotation = "BlazeDb:Descriptor";

    public static BlazeDbTableDescriptor Resolve(IEntityType entityType)
    {
        if (entityType.FindAnnotation(DescriptorAnnotation)?.Value is BlazeDbTableDescriptor configured)
        {
            return configured;
        }

        var clrType = entityType.ClrType;
        var descriptor = FindConventionalDescriptor(clrType);
        if (descriptor is not null)
        {
            return descriptor;
        }

        throw new InvalidOperationException(
            $"No BlazeDb table descriptor was found for entity type '{clrType.Name}'. Annotate the " +
            "type with [Bit.BlazeDb.BlazeDbTable] so the source generator emits one, or call " +
            $"modelBuilder.Entity<{clrType.Name}>().UseBlazeDbTable(descriptor).");
    }

    private static BlazeDbTableDescriptor? FindConventionalDescriptor(Type clrType)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        foreach (var field in clrType.GetFields(flags))
        {
            if (Matches(field.FieldType, clrType))
            {
                return (BlazeDbTableDescriptor?)field.GetValue(null);
            }
        }
        foreach (var property in clrType.GetProperties(flags))
        {
            if (Matches(property.PropertyType, clrType))
            {
                return (BlazeDbTableDescriptor?)property.GetValue(null);
            }
        }
        return null;
    }

    private static bool Matches(Type memberType, Type clrType) =>
        memberType.IsGenericType &&
        memberType.GetGenericTypeDefinition() == typeof(BlazeDbTableDescriptor<,>) &&
        memberType.GetGenericArguments()[1] == clrType;

    /// <summary>Creates the strongly-typed binding for a descriptor whose type arguments are only known at runtime.</summary>
    public static IBlazeDbTableBinding CreateBinding(BlazeDbDatabase database, IEntityType entityType)
    {
        var descriptor = Resolve(entityType);
        var arguments = descriptor.GetType().GetGenericArguments();
        var bindingType = typeof(BlazeDbTableBinding<,>).MakeGenericType(arguments);
        var table = GetTableMethod
            .MakeGenericMethod(arguments)
            .Invoke(database, [descriptor])!;
        return (IBlazeDbTableBinding)Activator.CreateInstance(bindingType, table, FindKeyMember(entityType, descriptor, arguments[0]))!;
    }

    /// <summary>
    /// The property a <c>row.X == value</c> may be sent to the primary-key dictionary for. It has to be
    /// the property the engine's key selector reads: the descriptor names it when it was generated,
    /// and EF's own primary key is trusted only when it is that same property (or, for a hand-written
    /// descriptor that does not say, when it is a single property of the key's type). Anything else
    /// - an EF <c>HasKey</c> on a different property, a composite key - leaves such predicates as
    /// filters, which is slower but never answers a different question.
    /// </summary>
    private static string? FindKeyMember(IEntityType entityType, BlazeDbTableDescriptor descriptor, Type keyType)
    {
        var properties = entityType.FindPrimaryKey()?.Properties;
        if (properties is not [{ } property] || property.ClrType != keyType)
        {
            return null;
        }
        var declared = descriptor.KeyMember;
        return declared is null || declared == property.Name ? property.Name : null;
    }

    private static readonly MethodInfo GetTableMethod =
        typeof(BlazeDbDatabase).GetMethod(nameof(BlazeDbDatabase.GetTable))!;
}
