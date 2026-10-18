using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bit.BlazeDb.EntityFrameworkCore.Storage;

/// <summary>
/// The engine keeps every row as a live .NET object, so there is nothing to convert between a
/// store type and a CLR type. Every type the model can express is mapped as itself.
/// <para>
/// What does need care is the change tracker's view of the mutable values a row can hold - the
/// <c>List&lt;T&gt;</c> and <c>T[]</c> collections and <c>byte[]</c> blobs. EF's default comparer for
/// those compares references and snapshots the reference, so a collection changed in place would
/// still equal its "original" and the save would be skipped, leaving memory and the log apart.
/// They are compared element by element and snapshotted as copies instead.
/// </para>
/// </summary>
internal sealed class BlazeDbTypeMappingSource : TypeMappingSource
{
    public BlazeDbTypeMappingSource(TypeMappingSourceDependencies dependencies) : base(dependencies)
    {
    }

    protected override CoreTypeMapping? FindMapping(in TypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;
        return clrType is null ? null : new BlazeDbTypeMapping(clrType, BlazeDbSequenceComparer.TryCreate(clrType));
    }

    private sealed class BlazeDbTypeMapping : CoreTypeMapping
    {
        public BlazeDbTypeMapping(Type clrType, ValueComparer? comparer)
            : base(new CoreTypeMappingParameters(clrType, comparer: comparer, keyComparer: comparer))
        {
        }

        private BlazeDbTypeMapping(CoreTypeMappingParameters parameters) : base(parameters)
        {
        }

        public override CoreTypeMapping WithComposedConverter(
            ValueConverter? converter,
            ValueComparer? comparer = null,
            ValueComparer? keyComparer = null,
            CoreTypeMapping? elementMapping = null,
            JsonValueReaderWriter? jsonValueReaderWriter = null) =>
            new BlazeDbTypeMapping(Parameters.WithComposedConverter(
                converter, comparer, keyComparer, elementMapping, jsonValueReaderWriter));

        protected override CoreTypeMapping Clone(CoreTypeMappingParameters parameters) =>
            new BlazeDbTypeMapping(parameters);
    }
}
