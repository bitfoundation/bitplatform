namespace Bit.Borm;

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
