using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bit.BlazeDb.EntityFrameworkCore.Infrastructure;

internal sealed class BlazeDbModelValidator : ModelValidator
{
    public BlazeDbModelValidator(ModelValidatorDependencies dependencies) : base(dependencies)
    {
    }

    public override void Validate(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        base.Validate(model, logger);

        foreach (var entityType in model.GetEntityTypes())
        {
            ThrowIfQueryFiltered(entityType);
        }
    }

    /// <summary>
    /// Query filters are applied by EF's query pipeline, which this provider replaces, so a filter would
    /// be ignored and every row it exists to hide - another tenant's, a soft-deleted one - returned.
    /// <para>
    /// Also checked where an entity type is bound to its table: a compiled model is not validated when
    /// it is loaded, so this validator never sees it.
    /// </para>
    /// </summary>
    internal static void ThrowIfQueryFiltered(IEntityType entityType)
    {
        if (entityType.GetDeclaredQueryFilters().Count > 0)
        {
            throw new NotSupportedException(
                $"BlazeDb does not apply query filters, so the HasQueryFilter on '{entityType.DisplayName()}' would be ignored. " +
                "Filter in the query itself instead.");
        }
    }
}
