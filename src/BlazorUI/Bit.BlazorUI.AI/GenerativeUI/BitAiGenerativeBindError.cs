namespace Bit.BlazorUI;

/// <summary>
/// A property of a generated component that did not bind, and why: one entry of
/// <see cref="BitAiGenerativeBindResult.Failures"/>.
/// </summary>
/// <remarks>
/// The binder describes a failure rather than wording it, so whoever shows it words it in the language of the page:
/// <see cref="BitAiGenerativeUiView"/> writes it through <see cref="BitAiStrings.UnreadableProperty"/> and
/// <see cref="BitAiStrings.MissingProperty"/>. <see cref="ToString"/> is the English description a log reads, the same
/// text <see cref="BitAiGenerativeBindResult.Errors"/> lists.
/// </remarks>
/// <param name="Kind">Why the property did not bind.</param>
/// <param name="PropertyName">The property, named as the model wrote it, or as the component declares it when missing.</param>
public sealed record BitAiGenerativeBindError(BitAiGenerativeBindErrorKind Kind, string PropertyName)
{
    /// <summary>The name of the component the property belongs to.</summary>
    public string? ComponentName { get; init; }

    /// <summary>
    /// The type the value was to be read as, for an <see cref="BitAiGenerativeBindErrorKind.Unreadable"/> value.
    /// </summary>
    public Type? ExpectedType { get; init; }

    /// <summary>
    /// What the serializer said about an <see cref="BitAiGenerativeBindErrorKind.Unreadable"/> value: an explanation
    /// written for a developer, in the language of the runtime rather than the page's.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>The English description of the failure, the way a log reads it.</summary>
    /// <returns>The description.</returns>
    public override string ToString() => Kind switch
    {
        BitAiGenerativeBindErrorKind.Missing => $"'{PropertyName}' is required by '{ComponentName}'.",
        _ when Detail is { Length: > 0 } => $"'{PropertyName}' could not be read as {ExpectedType?.Name}: {Detail}",
        _ => $"'{PropertyName}' could not be read as {ExpectedType?.Name}.",
    };
}
