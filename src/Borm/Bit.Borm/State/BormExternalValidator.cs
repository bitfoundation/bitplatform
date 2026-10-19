namespace Bit.Borm;

/// <summary>
/// Optional external validator (e.g. FluentValidation) invoked alongside the schema's built-in rules.
/// </summary>
public delegate ValueTask<IReadOnlyList<BormValidationMessage>> BormExternalValidator(
    BormDefinition form, IBormDataReader data, IServiceProvider? services);

/// <summary>Helpers for combining external validators.</summary>
public static class BormExternalValidatorExtensions
{
    /// <summary>
    /// Runs <paramref name="first"/> and <paramref name="second"/> and returns everything both found.
    /// </summary>
    /// <remarks>
    /// A form has one external-validator slot, so an integration that assigns it replaces whatever was
    /// there. Wiring up FluentValidation <em>and</em> the model's own <c>IValidatableObject</c> is an
    /// entirely reasonable thing to want — they cover different rules — and doing it meant the second
    /// call silently threw the first away, leaving a form that appeared to have half its validation
    /// and nothing at all to say why. Combining is what the caller who wrote both lines meant.
    /// </remarks>
    public static BormExternalValidator CombineWith(
        this BormExternalValidator? first, BormExternalValidator second)
    {
        ArgumentNullException.ThrowIfNull(second);
        if (first is null) return second;

        return async (form, data, services) =>
        {
            var a = await first(form, data, services);
            var b = await second(form, data, services);
            if (a.Count == 0) return b;
            if (b.Count == 0) return a;

            var merged = new List<BormValidationMessage>(a.Count + b.Count);
            merged.AddRange(a);
            // The two validators may report the same problem on the same field in the same words —
            // [Required] read by both layers, say — and the user should read it once.
            var seen = new HashSet<(string, string)>(a.Count);
            foreach (var m in a) seen.Add((m.FieldPath, m.Message));
            foreach (var m in b)
                if (seen.Add((m.FieldPath, m.Message))) merged.Add(m);
            return merged;
        };
    }
}
