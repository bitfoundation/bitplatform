using global::FluentValidation;
using global::FluentValidation.Results;
using FvSeverity = global::FluentValidation.Severity;
using FvContext = global::FluentValidation.ValidationContext<object>;

namespace Bit.Borm;

/// <summary>
/// Bridges FluentValidation into Borm. FluentValidation reports failures with property paths
/// (e.g. <c>Address.City</c>, <c>Items[0].Product</c>) that already match Borm field paths, so
/// results map directly onto fields.
/// </summary>
public static class BormFluentValidationIntegration
{
    /// <summary>
    /// Registers a concrete validator instance as the form's external validator.
    /// </summary>
    public static BormState UseFluentValidation<TModel>(this BormState state, IValidator<TModel> validator)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(validator);

        // Combined rather than assigned: a form has one external-validator slot, and calling this
        // alongside UseDataAnnotations() used to mean whichever came second silently won.
        state.ExternalValidator = state.ExternalValidator.CombineWith((_, data, _) =>
        {
            if (data.Root is not TModel model)
                return new ValueTask<IReadOnlyList<BormValidationMessage>>(Array.Empty<BormValidationMessage>());
            return RunAsync(validator, new global::FluentValidation.ValidationContext<TModel>(model));
        });
        return state;
    }

    /// <summary>
    /// Resolves <c>IValidator&lt;TModel&gt;</c> from the service provider and registers it as the
    /// external validator. The model type comes from <see cref="BormDefinition.ModelType"/>, or
    /// — for a schema built without one — from the runtime type of the bound model.
    /// </summary>
    /// <param name="state">The form state to attach to.</param>
    /// <param name="throwIfMissing">
    /// When true, a missing <c>IValidator&lt;T&gt;</c> registration throws instead of silently
    /// validating nothing. Useful in development, where a forgotten registration otherwise looks like
    /// a form with no rules at all.
    /// </param>
    public static BormState UseFluentValidation(this BormState state, bool throwIfMissing = false)
    {
        ArgumentNullException.ThrowIfNull(state);

        state.ExternalValidator = state.ExternalValidator.CombineWith(async (form, data, services) =>
        {
            if (data.Root is null || services is null)
                return Array.Empty<BormValidationMessage>();

            var modelType = form.ModelType ?? data.Root.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(modelType);

            if (services.GetService(validatorType) is not IValidator validator)
            {
                return throwIfMissing
                    ? throw new InvalidOperationException(
                        $"No IValidator<{modelType.Name}> is registered. Register one, or pass the validator " +
                        "explicitly with UseFluentValidation(validator).")
                    : (IReadOnlyList<BormValidationMessage>)Array.Empty<BormValidationMessage>();
            }

            var result = await validator.ValidateAsync(new FvContext(data.Root));
            return Map(result);
        });
        return state;
    }

    private static async ValueTask<IReadOnlyList<BormValidationMessage>> RunAsync<TModel>(
        IValidator<TModel> validator, global::FluentValidation.ValidationContext<TModel> context)
    {
        var result = await validator.ValidateAsync(context);
        return Map(result);
    }

    private static IReadOnlyList<BormValidationMessage> Map(ValidationResult result)
    {
        if (result.IsValid) return Array.Empty<BormValidationMessage>();
        var messages = new List<BormValidationMessage>(result.Errors.Count);
        foreach (var failure in result.Errors)
            messages.Add(new BormValidationMessage(failure.PropertyName, failure.ErrorMessage, MapSeverity(failure.Severity)));
        return messages;
    }

    private static BormValidationSeverity MapSeverity(FvSeverity severity) => severity switch
    {
        FvSeverity.Error => BormValidationSeverity.Error,
        FvSeverity.Warning => BormValidationSeverity.Warning,
        FvSeverity.Info => BormValidationSeverity.Info,
        _ => BormValidationSeverity.Error
    };
}
