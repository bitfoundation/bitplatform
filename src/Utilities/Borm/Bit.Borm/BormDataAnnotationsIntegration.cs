using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Borm;

/// <summary>
/// Runs the model's own <see cref="IValidatableObject"/> validation — the .NET-native way to say
/// "these two properties have to agree" — alongside the schema's rules.
/// </summary>
/// <remarks>
/// <para>
/// This is the counterpart of <c>UseFluentValidation()</c> (from the Bit.Borm.FluentValidation package)
/// for teams whose models already carry their rules as DataAnnotations. It deliberately does *not* run
/// the property attributes by default: <see cref="BormSchemaGenerator"/> has already turned
/// <c>[Required]</c>, <c>[StringLength]</c>, <c>[Range]</c> and the rest into field rules, so running
/// <see cref="Validator"/> over them again would put two differently-worded copies of the same
/// complaint under every field. What it cannot express — and what this adds — is the cross-property
/// layer.
/// </para>
/// <para>
/// Failures land on the field path each result names, and failures on a field the schema is currently
/// hiding are dropped, because a validator sees the whole model and knows nothing about the form's
/// conditions.
/// </para>
/// </remarks>
public static class BormDataAnnotationsIntegration
{
    /// <summary>
    /// Validates the bound model on every full validation pass.
    /// </summary>
    /// <param name="state">The form state to attach to.</param>
    /// <param name="includePropertyAttributes">
    /// Whether the property-level <see cref="ValidationAttribute"/>s are evaluated as well. Leave this
    /// false (the default) for a schema generated from the model or described with the typed builder,
    /// where those attributes are already field rules and running them twice only doubles the
    /// messages. Turn it on when the schema came from somewhere else — a JSON Schema document rendered
    /// over a typed model — so the model's own constraints are still enforced.
    /// </param>
    /// <remarks>
    /// <para>
    /// <see cref="IValidatableObject.Validate"/> is called directly rather than through
    /// <see cref="Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult}, bool)"/>,
    /// which skips it entirely whenever any property attribute has already failed. On a form that
    /// would mean the cross-property messages appear only once every other problem is fixed — the user
    /// corrects the last required field, presses submit again, and is told about something new. This
    /// library's stance everywhere else is to report everything it has found at once.
    /// </para>
    /// <para>
    /// Only the root model is validated, not nested objects or array items — the same scope
    /// <see cref="Validator"/> itself has. Nested constraints are already covered by the generated
    /// field rules; reach for FluentValidation when a nested object needs cross-property logic.
    /// </para>
    /// </remarks>
    public static BormState UseDataAnnotations(this BormState state, bool includePropertyAttributes = false)
    {
        ArgumentNullException.ThrowIfNull(state);

        // Combined rather than assigned: a form has one external-validator slot, and calling this
        // alongside UseFluentValidation() used to mean whichever came second silently won.
        state.ExternalValidator = state.ExternalValidator.CombineWith((_, data, services) =>
        {
            if (data.Root is not { } model)
                return new ValueTask<IReadOnlyList<BormValidationMessage>>(Array.Empty<BormValidationMessage>());

            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, services, items: null);

            if (includePropertyAttributes)
                ValidateAttributes(model, context, services, results);

            if (model is IValidatableObject validatable)
                results.AddRange(validatable.Validate(context).Where(r => r != ValidationResult.Success));

            return new ValueTask<IReadOnlyList<BormValidationMessage>>(Map(results));
        });
        return state;
    }

    /// <summary>
    /// The attribute half of <see cref="Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult}, bool)"/>:
    /// every property's <see cref="ValidationAttribute"/>s, then the type's own.
    /// </summary>
    /// <remarks>
    /// <c>TryValidateObject</c> itself cannot be used for this. It calls
    /// <see cref="IValidatableObject.Validate"/> too — when every attribute passed — and its result
    /// cannot say whether it did, since it is false both when an attribute failed and when
    /// <c>Validate</c> did. Calling it and then <c>Validate</c> ran the model's own validation twice on
    /// every valid model, doubling whatever it costs (a database lookup included); validating the
    /// attributes here leaves exactly one call, made by <see cref="UseDataAnnotations"/>.
    /// <para>
    /// The class-level attributes keep DataAnnotations' own short-circuit: they run only once every
    /// property attribute has passed. A class-level <c>[CustomValidation]</c> is written on that
    /// assumption — it reads members a <c>[Required]</c> has already vouched for — and run regardless,
    /// it dereferenced the very null the property attribute had just reported, throwing out of the
    /// validation pass.
    /// </para>
    /// </remarks>
    private static void ValidateAttributes(object model, ValidationContext context, IServiceProvider? services,
        List<ValidationResult> results)
    {
        var before = results.Count;

        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(model))
        {
            if (!property.Attributes.OfType<ValidationAttribute>().Any()) continue;

            var propertyContext = new ValidationContext(model, services, items: null) { MemberName = property.Name };
            Validator.TryValidateProperty(property.GetValue(model), propertyContext, results);
        }

        if (results.Count != before) return;

        Validator.TryValidateValue(model, context, results,
            TypeDescriptor.GetAttributes(model).OfType<ValidationAttribute>());
    }

    private static IReadOnlyList<BormValidationMessage> Map(List<ValidationResult> results)
    {
        if (results.Count == 0) return Array.Empty<BormValidationMessage>();

        var messages = new List<BormValidationMessage>(results.Count);
        var seen = new HashSet<(string, string)>();

        foreach (var result in results)
        {
            if (result?.ErrorMessage is not { Length: > 0 } message) continue;

            // A result naming no member is a statement about the model as a whole — which is what
            // IValidatableObject usually returns — and belongs on the form, not on a field.
            var members = result.MemberNames?.Where(m => !string.IsNullOrEmpty(m)).ToList() ?? [];
            if (members.Count == 0)
            {
                Add(string.Empty, message);
                continue;
            }

            // A result may name several members; the same complaint goes on each, so whichever one the
            // user is looking at explains itself.
            foreach (var member in members) Add(member, message);
        }

        return messages;

        void Add(string path, string message)
        {
            // Turning it on runs the property attributes over a model whose own IValidatableObject may
            // report the same thing; the user should read it once.
            if (seen.Add((path, message)))
                messages.Add(new BormValidationMessage(path, message));
        }
    }
}
