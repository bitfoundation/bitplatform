namespace Bit.BlazorUI;

/// <summary>
/// Where <see cref="BitAiChatSessionFactory"/> finds the strings of a session whose options name none: registered by
/// the UI package as the strings of its registered UI options, which this package cannot name. Without it (a server, a
/// headless client) a session uses <see cref="BitAiStrings.Default"/>.
/// </summary>
/// <param name="resolve">Reads the strings off the services of the scope the session is created in.</param>
internal sealed class BitAiDefaultStrings(Func<IServiceProvider, BitAiStrings?> resolve)
{
    /// <summary>The strings, or <see langword="null"/> for none.</summary>
    /// <param name="services">The services of the scope the session is created in.</param>
    /// <returns>The strings.</returns>
    public BitAiStrings? Resolve(IServiceProvider services) => resolve(services);
}
