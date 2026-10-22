using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// The languages the highlighter knows, keyed by identifier and alias.
/// </summary>
/// <remarks>
/// Register a <see cref="BitAiLanguageDefinition"/> to add a language, or replace a built-in one to
/// change how it is coloured, without touching the highlighter itself.
/// </remarks>
public static class BitAiLanguageRegistry
{
    private static readonly ConcurrentDictionary<string, BitAiLanguageDefinition> Languages =
        new(StringComparer.OrdinalIgnoreCase);

    static BitAiLanguageRegistry()
    {
        foreach (var language in BitAiBuiltInLanguages.All) Register(language);
    }

    /// <summary>Adds or replaces a language, indexing it by identifier and every alias.</summary>
    /// <param name="language">The language definition.</param>
    public static void Register(BitAiLanguageDefinition language)
    {
        ArgumentNullException.ThrowIfNull(language);

        // Replacing a language replaces it under the aliases it was registered with too; otherwise
        // ```cs would keep the old colours after csharp was replaced.
        if (Languages.TryGetValue(language.Id, out var previous) && !ReferenceEquals(previous, language))
        {
            foreach (var (name, existing) in Languages)
            {
                if (ReferenceEquals(existing, previous)) Languages.TryUpdate(name, language, previous);
            }
        }

        Languages[language.Id] = language;
        foreach (var alias in language.Aliases) Languages[alias] = language;
    }

    /// <summary>Finds a language by identifier or alias.</summary>
    /// <param name="name">The name from the fence info string.</param>
    /// <returns>The language, or <see langword="null"/> when it is not registered.</returns>
    public static BitAiLanguageDefinition? Find(string? name) =>
        name is not null && Languages.TryGetValue(name, out var language) ? language : null;

    /// <summary>Whether a language is registered under this name.</summary>
    /// <param name="name">The name from the fence info string.</param>
    /// <returns><see langword="true"/> when highlighting is available.</returns>
    public static bool IsSupported(string? name) => Find(name) is not null;

    /// <summary>The distinct registered languages.</summary>
    /// <remarks>Each identifier yields the definition currently registered under it.</remarks>
    public static IReadOnlyCollection<BitAiLanguageDefinition> All =>
        Languages
            .Where(entry => string.Equals(entry.Key, entry.Value.Id, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Value)
            .OrderBy(l => l.Id, StringComparer.Ordinal)
            .ToArray();
}
