namespace Bit.Borm;

/// <summary>How the library resolves its own words, in one place.</summary>
internal static class BormMessageText
{
    /// <summary>The registered provider, else the English defaults.</summary>
    internal static IBormMessageProvider Provider(IServiceProvider? services)
        => services?.GetService(typeof(IBormMessageProvider)) as IBormMessageProvider ?? BormDefaultMessageProvider.Instance;

    /// <summary>
    /// The text for <paramref name="key"/> in <paramref name="language"/> when there is one and the provider can answer in
    /// a language of its choosing, else in the provider's own (the current UI culture's, for most).
    /// </summary>
    internal static string Get(IBormMessageProvider provider, string? language, string key, object?[] args)
        => language is not null && provider is IBormLocalizedMessageProvider localized
            ? localized.Get(language, key, args)
            : provider.Get(key, args);
}
