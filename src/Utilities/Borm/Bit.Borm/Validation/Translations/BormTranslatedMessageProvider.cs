using System.Globalization;

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

/// <summary>
/// The library's own words - every validation message, button, announcement and label it renders - in German,
/// Spanish, French, Italian, Dutch, Portuguese, Russian, Turkish, Persian, Arabic, Chinese (simplified) and
/// Japanese, and in English for any other language or any word a table lacks. Register it with
/// <c>services.AddBormTranslations()</c>.
/// </summary>
/// <remarks>
/// The language is the form's <see cref="BormState.Language"/> when it has one, else the current UI culture - by name,
/// so it needs no culture data and works under <c>InvariantGlobalization</c> - and then its parent: <c>de-CH</c> reads
/// the German table. Simplified Chinese answers to <c>zh</c>, <c>zh-Hans</c>, <c>zh-CN</c> and <c>zh-SG</c>; a
/// traditional-Chinese culture is not given it. A count is phrased so the sentence stands for any number, rather than
/// with one language's plural rules imposed on another's. The words a schema supplies - an item noun, a label - are
/// the schema's to translate (<see cref="IBormSchemaLocalizer"/>, <c>x-translations</c>).
/// </remarks>
public sealed class BormTranslatedMessageProvider : IBormLocalizedMessageProvider
{
    /// <summary>A shared instance; it holds nothing but the tables.</summary>
    public static BormTranslatedMessageProvider Instance { get; } = new();

    /// <summary>The languages it has a table for, by culture name.</summary>
    public static IReadOnlyCollection<string> Languages => BormTranslations.Tables.Keys;

    /// <inheritdoc />
    public string Get(string key, params object?[] args) => Get(CultureInfo.CurrentUICulture.Name, key, args);

    /// <inheritdoc />
    public string Get(string language, string key, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(key);
        args ??= [];
        language ??= string.Empty;

        // zh-TW and zh-Hant-HK are parented by zh, whose table is simplified Chinese: English reads better than a
        // script the reader did not ask for.
        if (IsTraditionalChinese(language)) return BormDefaultMessageProvider.Instance.Get(key, args);

        for (var name = language; name.Length > 0; name = Parent(name))
        {
            if (!BormTranslations.Tables.TryGetValue(name, out var table)) continue;

            // A sentence about exactly one thing has a wording of its own where the table gives one.
            if (args is [1, ..] && table.TryGetValue(key + ".one", out var one)) return Format(one, args);
            if (table.TryGetValue(key, out var text)) return Format(text, args);
            // The language has a table and it lacks this word: English, not the language's parent's.
            break;
        }
        return BormDefaultMessageProvider.Instance.Get(key, args);
    }

    private static bool IsTraditionalChinese(string language)
        => language.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase)
           || language.Equals("zh-TW", StringComparison.OrdinalIgnoreCase)
           || language.Equals("zh-HK", StringComparison.OrdinalIgnoreCase)
           || language.Equals("zh-MO", StringComparison.OrdinalIgnoreCase);

    private static string Parent(string name)
    {
        var dash = name.LastIndexOf('-');
        return dash > 0 ? name[..dash] : string.Empty;
    }

    // The most arguments any message is given.
    private const int MaxArgs = 4;

    /// <summary>
    /// Every table entry is a composite format string, its literal braces doubled, formatted whether or not it was
    /// given anything - so a brace means the same in each. An argument a caller left out reads as nothing, as it
    /// does in English, rather than throwing.
    /// </summary>
    private static string Format(string template, object?[] args)
    {
        if (args.Length < MaxArgs) Array.Resize(ref args, MaxArgs);
        return string.Format(CultureInfo.CurrentCulture, template, args);
    }
}

/// <summary>The tables <see cref="BormTranslatedMessageProvider"/> reads, one per language, each in a file of its own.</summary>
internal static partial class BormTranslations
{
    private static Dictionary<string, IReadOnlyDictionary<string, string>>? _tables;

    internal static Dictionary<string, IReadOnlyDictionary<string, string>> Tables => _tables ??= Build();

    private static Dictionary<string, IReadOnlyDictionary<string, string>> Build()
    {
        var chinese = Chinese();
        return new(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = German(),
            ["es"] = Spanish(),
            ["fr"] = French(),
            ["it"] = Italian(),
            ["nl"] = Dutch(),
            ["pt"] = Portuguese(),
            ["ru"] = Russian(),
            ["tr"] = Turkish(),
            ["fa"] = Persian(),
            ["ar"] = Arabic(),
            ["ja"] = Japanese(),
            ["zh"] = chinese,
            ["zh-Hans"] = chinese,
            ["zh-CN"] = chinese,
            ["zh-SG"] = chinese
        };
    }

    // The keys the designer's properties and kinds are named under, for the tables to spell.
    private static string Property(string name) => BormMessageKeys.DesignerProperty(name);

    private static string Kind(string name) => BormMessageKeys.DesignerKind(name);
}
