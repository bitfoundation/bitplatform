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
            if (entityType.GetDeclaredQueryFilters().Count > 0)
            {
                throw new NotSupportedException(
                    $"BlazeDb does not apply query filters, so the HasQueryFilter on '{entityType.DisplayName()}' would be ignored. " +
                    "Filter in the query itself instead.");
            }
        }
    }
}
