using Bit.BlazeDb.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>Maps an entity type onto a specific BlazeDb table.</summary>
public static class BlazeDbModelBuilderExtensions
{
    /// <summary>
    /// Binds the entity type to <paramref name="descriptor"/>. Only needed when the descriptor is
    /// hand-written or lives elsewhere: for a type annotated with <c>[Bit.BlazeDb.BlazeDbTable]</c>, the
    /// source generator emits a static <c>Table</c> member the provider finds on its own.
    /// </summary>
    public static EntityTypeBuilder<TEntity> UseBlazeDbTable<TEntity, TKey>(
        this EntityTypeBuilder<TEntity> builder, Bit.BlazeDb.BlazeDbTableDescriptor<TKey, TEntity> descriptor)
        where TEntity : class
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(descriptor);

        builder.Metadata.SetAnnotation(BlazeDbTableResolver.DescriptorAnnotation, descriptor);
        return builder;
    }
}
