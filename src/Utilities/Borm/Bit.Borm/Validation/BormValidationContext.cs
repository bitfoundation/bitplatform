namespace Bit.Borm;

/// <summary>Context passed to a validation rule.</summary>
public sealed class BormValidationContext
{
    public BormValidationContext(
        string fieldPath,
        object? value,
        IBormDataReader data,
        IServiceProvider? services = null,
        BormFieldDefinition? field = null)
    {
        FieldPath = fieldPath;
        Value = value;
        Data = data;
        Services = services;
        Field = field;
    }

    /// <summary>The path of the field being validated.</summary>
    public string FieldPath { get; }

    /// <summary>The current value of the field being validated.</summary>
    public object? Value { get; }

    /// <summary>Read access to the whole form, for cross-field rules.</summary>
    public IBormDataReader Data { get; }

    /// <summary>Optional service provider for rules that need DI (e.g. async uniqueness checks).</summary>
    public IServiceProvider? Services { get; }

    /// <summary>The definition of the field being validated, when the caller supplied it.</summary>
    public BormFieldDefinition? Field { get; }

    /// <summary>
    /// Signalled when this validation run has been superseded — the user typed again, pressed Next a
    /// second time, or the form was reset or disposed. An async rule should pass it on to the remote
    /// call it makes, so a lookup nobody is waiting for any more stops costing anything; its answer
    /// is discarded either way.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// The language the form reads its own text in - its <see cref="BormState.Language"/> - or null to follow the
    /// current UI culture. <see cref="Message"/> answers in it when the registered provider is an
    /// <see cref="IBormLocalizedMessageProvider"/>.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>The field's label, falling back to its path — for messages that need to name the field.</summary>
    public string DisplayName => Field?.Label ?? FieldPath;

    /// <summary>
    /// Resolves the text of a built-in message (see <see cref="BormMessageKeys"/>) through the
    /// <see cref="IBormMessageProvider"/> registered in <see cref="Services"/> - in <see cref="Language"/> when it
    /// is set and the provider can answer in it - falling back to the English defaults when none is registered.
    /// </summary>
    public string Message(string key, params object?[] args)
        => BormMessageText.Get(BormMessageText.Provider(Services), Language, key, args);
}
