using System.Collections;

namespace Bit.Borm;

/// <summary>
/// Runs a <see cref="BormDefinition"/>'s validation rules against form data, walking nested
/// objects and array items and honouring conditional visibility (hidden fields are not validated).
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
    Justification = "The validator is stateless today, but it is a public, instantiable seam; making " +
                    "its methods static would break every caller for no functional gain.")]
public sealed class BormValidator
{
    // Stateless and reused: RequiredWhen would otherwise allocate one per field per validation pass.
    private static readonly BormRequiredRule ConditionalRequired = new();

    /// <summary>Validates the whole form (or a subset of top-level fields).</summary>
    /// <param name="form">The schema.</param>
    /// <param name="data">The data to validate.</param>
    /// <param name="services">Optional DI provider for rules that need it.</param>
    /// <param name="restrictToFields">When set, only these top-level field names are validated.</param>
    /// <param name="includeAsync">When false, async rules are skipped (e.g. for fast on-change validation).</param>
    /// <param name="cancellationToken">Abandons the run when a newer validation supersedes this one.</param>
    public async ValueTask<IReadOnlyList<BormValidationMessage>> ValidateAsync(
        BormDefinition form,
        IBormDataReader data,
        IServiceProvider? services = null,
        ISet<string>? restrictToFields = null,
        bool includeAsync = true,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<BormValidationMessage>();
        foreach (var field in form.Fields)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (restrictToFields is not null && !restrictToFields.Contains(field.Name))
                continue;
            await ValidateField(field, field.Name, data, services, includeAsync, messages, cancellationToken);
        }

        // Form-level rules run only for a full validation: a step or single field cannot judge the whole form.
        if (restrictToFields is null)
        {
            foreach (var rule in form.Validators)
            {
                if (rule.IsAsync && !includeAsync) continue;
                cancellationToken.ThrowIfCancellationRequested();
                var ctx = new BormValidationContext(string.Empty, data.Root, data, services)
                {
                    CancellationToken = cancellationToken
                };
                var result = await rule.ValidateAsync(ctx);
                if (!result.IsValid && result.Message is not null)
                    messages.Add(new BormValidationMessage(result.FieldPath ?? string.Empty, result.Message, result.Severity));
            }
        }

        return messages;
    }

    /// <summary>Validates a single field located at <paramref name="path"/>.</summary>
    public async ValueTask<IReadOnlyList<BormValidationMessage>> ValidateFieldAsync(
        BormFieldDefinition field,
        string path,
        IBormDataReader data,
        IServiceProvider? services = null,
        bool includeAsync = true,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<BormValidationMessage>();
        await ValidateField(field, path, data, services, includeAsync, messages, cancellationToken);
        return messages;
    }

    /// <summary>
    /// Runs only the synchronous rules declared on the field at <paramref name="path"/> itself, without
    /// descending into its children or rows — what a list's size and uniqueness are judged by after a
    /// row is added or removed. A hidden field reports nothing, as it does in a full pass.
    /// </summary>
    internal async ValueTask<IReadOnlyList<BormValidationMessage>> ValidateOwnRulesAsync(
        BormFieldDefinition field,
        string path,
        IBormDataReader data,
        IServiceProvider? services)
    {
        var messages = new List<BormValidationMessage>();
        await ValidateField(field, path, data, services, includeAsync: false, messages, CancellationToken.None, descend: false);
        return messages;
    }

    private static async ValueTask ValidateField(
        BormFieldDefinition field,
        string path,
        IBormDataReader data,
        IServiceProvider? services,
        bool includeAsync,
        List<BormValidationMessage> messages,
        CancellationToken cancellationToken,
        bool descend = true)
    {
        // Presentational content holds no value, so there is nothing here to be valid or invalid.
        if (field.IsPresentational) return;

        // Conditions and cross-field rules read paths relative to the object that owns the field, so a
        // rule written on a repeater's template ("required when this row's Kind is X") means this row.
        // An absolute path still resolves, because the scope falls back to the root.
        var scoped = BormScopedDataReader.ForOwnerOf(data, path);

        // Hidden fields are skipped entirely (including their rules and children): a rule the user
        // cannot see and cannot satisfy would block the form with no way forward.
        if (field.VisibleWhen is not null && !field.VisibleWhen.Evaluate(scoped))
            return;

        var value = data.GetValue(path);
        var ctx = new BormValidationContext(path, value, scoped, services, field)
        {
            CancellationToken = cancellationToken
        };

        // Conditional requiredness is evaluated before the declared rules so its message leads.
        if (field.RequiredWhen is not null && field.RequiredWhen.Evaluate(scoped) && !field.Required)
        {
            var result = await ConditionalRequired.ValidateAsync(ctx);
            if (!result.IsValid && result.Message is not null)
                messages.Add(new BormValidationMessage(path, result.Message, result.Severity));
        }

        foreach (var rule in field.Validators)
        {
            if (rule.IsAsync && !includeAsync)
                continue;

            cancellationToken.ThrowIfCancellationRequested();
            var result = await rule.ValidateAsync(ctx);
            if (!result.IsValid && result.Message is not null)
                messages.Add(new BormValidationMessage(result.FieldPath ?? path, result.Message, result.Severity));
        }

        // Recurse into composition.
        if (!descend) return;
        if (field.Type == BormFieldType.Object)
        {
            foreach (var child in field.Children)
                await ValidateField(child, BormPath.Combine(path, child.Name), data, services, includeAsync, messages, cancellationToken);
        }
        else if (field.Type == BormFieldType.Array && field.ItemTemplate is not null)
        {
            var count = value switch
            {
                null => 0,
                ICollection c => c.Count,
                IEnumerable e and not string => e.Cast<object?>().Count(),
                _ => 0
            };

            for (var i = 0; i < count; i++)
            {
                var itemPath = BormPath.Combine(path, i);
                var template = field.ItemTemplate;
                if (template.Type == BormFieldType.Object)
                {
                    foreach (var child in template.Children)
                        await ValidateField(child, BormPath.Combine(itemPath, child.Name), data, services, includeAsync, messages, cancellationToken);
                }
                else
                {
                    await ValidateField(template, itemPath, data, services, includeAsync, messages, cancellationToken);
                }
            }
        }
    }
}
