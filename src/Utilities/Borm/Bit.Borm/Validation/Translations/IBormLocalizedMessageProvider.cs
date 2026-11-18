namespace Bit.Borm;

/// <summary>
/// A message provider that answers in a form's own <see cref="BormState.Language"/> as well as in the current UI
/// culture. <see cref="BormTranslatedMessageProvider"/> is one; a provider of your own can be too.
/// </summary>
public interface IBormLocalizedMessageProvider : IBormMessageProvider
{
    /// <summary>The message for <paramref name="key"/> in <paramref name="language"/> - a culture name, <c>de</c> or <c>pt-BR</c>.</summary>
    string Get(string language, string key, params object?[] args);
}
