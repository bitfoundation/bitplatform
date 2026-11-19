namespace Bit.Borm;

/// <summary>
/// Supplies the text of built-in validation messages. Register an implementation in DI to localise
/// Borm without replacing its rules; the provider is resolved from
/// <see cref="BormValidationContext.Services"/> at validation time, so the same schema can render
/// in different languages per request.
/// </summary>
public interface IBormMessageProvider
{
    /// <summary>
    /// Returns the message for <paramref name="key"/> (see <see cref="BormMessageKeys"/>),
    /// formatted with <paramref name="args"/>.
    /// </summary>
    string Get(string key, params object?[] args);
}
