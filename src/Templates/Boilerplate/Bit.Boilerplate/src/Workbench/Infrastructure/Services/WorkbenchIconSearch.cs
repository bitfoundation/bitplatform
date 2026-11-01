namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Searches bit BlazorUI's icons the way its demo's icon page does: the exact name first, then names that start with the
/// term, names with a word that starts with it, names a common word points at (trash finds Delete), and last any name
/// that contains it; ties go to the shorter name.
/// </summary>
public static class WorkbenchIconSearch
{
    private static readonly Dictionary<string, string[]> aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["trash"] = ["delete", "recyclebin"],
        ["bin"] = ["recyclebin", "delete"],
        ["garbage"] = ["delete", "recyclebin"],
        ["gear"] = ["settings"],
        ["cog"] = ["settings"],
        ["pencil"] = ["edit"],
        ["house"] = ["home"],
        ["magnifier"] = ["search", "zoom"],
        ["user"] = ["contact", "people", "account", "person"],
        ["avatar"] = ["contact", "people", "persona"],
        ["profile"] = ["persona", "contact", "account"],
        ["basket"] = ["shoppingcart", "cart"],
        ["logout"] = ["signout"],
        ["login"] = ["signin"],
        ["exit"] = ["signout", "leave"],
        ["close"] = ["cancel", "chromeclose", "clear"],
        ["cross"] = ["cancel", "chromeclose"],
        ["tick"] = ["accept", "checkmark", "completed"],
        ["success"] = ["completed", "accept", "checkmark"],
        ["danger"] = ["warning", "error", "blocked"],
        ["question"] = ["help", "unknown"],
        ["bell"] = ["ringer", "alert"],
        ["notification"] = ["ringer", "alert"],
        ["image"] = ["picture", "photo", "image"],
        ["hamburger"] = ["globalnavbutton", "collapsemenu"],
        ["menu"] = ["globalnavbutton", "collapsemenu", "more"],
        ["kebab"] = ["more"],
        ["ellipsis"] = ["more"],
        ["dots"] = ["more"],
        ["dropdown"] = ["chevrondown"],
        ["favourite"] = ["favorite"],
        ["spinner"] = ["progressring", "sync"],
        ["loading"] = ["progressring", "sync"],
        ["reload"] = ["refresh", "sync"],
        ["eye"] = ["view", "redeye", "hide"],
        ["envelope"] = ["mail"],
        ["plus"] = ["add", "calculatoraddition"],
        ["minus"] = ["remove", "calculatorsubtract"],
        ["dark"] = ["clearnight"],
        ["moon"] = ["clearnight"],
        ["light"] = ["sunny", "brightness"],
        ["language"] = ["localelanguage", "translate"],
        ["terminal"] = ["commandprompt", "developertools"],
        ["wrench"] = ["repair", "toolbox"],
        ["tool"] = ["repair", "toolbox", "developertools"],
        ["idea"] = ["lightbulb"],
        ["bulb"] = ["lightbulb"],
        ["radio"] = ["radiobtn", "radiobullet"],
        ["toggle"] = ["switch"],
        ["battery"] = ["power"]
    };

    private static readonly Lazy<(string Name, string Lower, string[] Words)[]> icons = new(() =>
    [
        .. typeof(BitIconName).GetFields(BindingFlags.Public | BindingFlags.Static)
                              .Select(field => field.GetRawConstantValue() as string)
                              .OfType<string>()
                              .Order(StringComparer.OrdinalIgnoreCase)
                              .Select(name => (name, name.ToLowerInvariant(), WordsOf(name)))
    ]);

    private static readonly Lazy<Dictionary<string, string>> fieldsByName = new(() => typeof(BitIconName).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.GetRawConstantValue() is string)
        .ToDictionary(field => (string)field.GetRawConstantValue()!, field => field.Name, StringComparer.Ordinal));

    public static int Count => icons.Value.Length;

    /// <summary>The BitIconName field that holds an icon name, like _12PointStar for 12PointStar; null for a name it doesn't have.</summary>
    public static string? FieldOf(string name) => fieldsByName.Value.GetValueOrDefault(name);

    /// <summary>The icon name a BitIconName field holds; null for a field it doesn't have.</summary>
    public static string? NameOf(string field) => typeof(BitIconName).GetField(field, BindingFlags.Public | BindingFlags.Static)?.GetRawConstantValue() as string;

    /// <summary>The icons a term finds, best first; every icon in name order for an empty term.</summary>
    public static IEnumerable<string> Search(string? term)
    {
        // Spaces are dropped, so "add friend" finds AddFriend.
        var needle = term?.Replace(" ", string.Empty).ToLowerInvariant();

        if (string.IsNullOrEmpty(needle))
            return icons.Value.Select(icon => icon.Name);

        var alias = aliases.GetValueOrDefault(needle);

        return icons.Value.Select(icon => (icon.Name, Rank: Rank(icon.Lower, icon.Words, needle, alias)))
                          .Where(match => match.Rank >= 0)
                          .OrderBy(match => match.Rank)
                          .ThenBy(match => match.Name.Length)
                          .ThenBy(match => match.Name, StringComparer.OrdinalIgnoreCase)
                          .Select(match => match.Name);
    }

    private static int Rank(string lower, string[] words, string needle, string[]? alias)
    {
        if (lower == needle)
            return 0;

        if (lower.StartsWith(needle, StringComparison.Ordinal))
            return 1;

        if (words.Any(word => word.StartsWith(needle, StringComparison.Ordinal)))
            return 2;

        if (alias?.Any(fragment => lower.Contains(fragment, StringComparison.Ordinal)) is true)
            return 3;

        return lower.Contains(needle, StringComparison.Ordinal) ? 4 : -1;
    }

    /// <summary>A PascalCase name's words in lower case: AddFriend as add and friend, AADLogo as aad and logo.</summary>
    private static string[] WordsOf(string name)
    {
        List<string> words = [];
        var start = 0;

        for (var index = 1; index <= name.Length; index++)
        {
            var isBoundary = index == name.Length
                || (char.IsUpper(name[index]) && char.IsUpper(name[index - 1]) is false)
                || char.IsDigit(name[index]) != char.IsDigit(name[index - 1])
                || (char.IsUpper(name[index]) && char.IsUpper(name[index - 1]) && index + 1 < name.Length && char.IsLower(name[index + 1]));

            if (isBoundary is false)
                continue;

            words.Add(name[start..index].ToLowerInvariant());
            start = index;
        }

        return [.. words];
    }
}
