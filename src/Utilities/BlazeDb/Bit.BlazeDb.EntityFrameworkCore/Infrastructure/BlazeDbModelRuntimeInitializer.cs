using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bit.BlazeDb.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Refuses a model with a query filter when it is first initialized for use. Every model comes through
/// here - a compiled one, or one handed to <c>UseModel</c>, as well as one built from
/// <c>OnModelCreating</c> - while <see cref="BlazeDbModelValidator"/> only sees the models EF validates,
/// and EF does not validate a compiled model when it loads one.
/// </summary>
internal sealed class BlazeDbModelRuntimeInitializer : ModelRuntimeInitializer
{
    private const string QueryFiltersCheckedAnnotation = "BlazeDb:QueryFiltersChecked";

    public BlazeDbModelRuntimeInitializer(ModelRuntimeInitializerDependencies dependencies) : base(dependencies)
    {
    }

    public override IModel Initialize(
        IModel model,
        bool designTime = true,
        IDiagnosticsLogger<DbLoggerCategory.Model.Validation>? validationLogger = null)
    {
        model = base.Initialize(model, designTime, validationLogger);

        // EF initializes a compiled model again for every context that uses it, so the verdict is kept on
        // the model and checked once. A check that throws keeps nothing, so every context is refused.
        model.GetOrAddRuntimeAnnotationValue(
            QueryFiltersCheckedAnnotation,
            static model =>
            {
                foreach (var entityType in model!.GetEntityTypes())
                {
                    BlazeDbModelValidator.ThrowIfQueryFiltered(entityType);
                }
                return true;
            },
            model);

        return model;
    }
}
