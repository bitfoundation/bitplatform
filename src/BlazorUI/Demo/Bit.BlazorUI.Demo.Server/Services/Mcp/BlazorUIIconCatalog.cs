using System.Text;
using Bit.BlazorUI.Demo.Client.Core.Pages.Iconography;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// The Fabric (MDL2) glyph names <c>BitIconName</c> declares, and a search over them.
/// <para>
/// There are over two thousand, which is why this is a search rather than a listing: handing the
/// whole set to a client costs more than every other answer this server gives put together, and an
/// agent wants one of them.
/// </para>
/// <para>
/// The set itself, its Pascal-case words, the categories it is browsed by and the aliases that
/// point everyday words at the MDL2 ones are all <see cref="IconCatalog"/>'s - the same catalog
/// behind the iconography page's search box, so a word taught to the page is a word this tool
/// understands, and the site and the server can never disagree about what the library contains.
/// </para>
/// <para>
/// What is this tool's own is the ranking, because it answers a phrase written blind rather than a
/// box being typed into with a grid of glyphs underneath it. Every word is scored on its own and an
/// icon has to answer all of them, so "shopping cart" is ShoppingCart rather than everything
/// shopping followed by everything cart; and the ways a word can be right are ordered, so a name
/// MADE of the word beats one that opens with it, which beats the word an alias says it means,
/// which beats a fragment buried inside it. The last resort is that no answer is a dead end: a word
/// the set does not use is answered through its alias, a misspelt one through the word it is one
/// edit from, and a query nothing answers at all through the nearest names and the families.
/// </para>
/// </summary>
public static class BlazorUIIconCatalog
{
    /// <summary>An icon as the ranking reads it - the C# member name, and everything matched against.</summary>
    private sealed record Row(string Name, string Lowered, string[] Words, bool InCore, string[] Categories);

    /// <summary>One word of a query, with everything that word is also allowed to mean.</summary>
    private sealed record Term(string Text, string[] Forms, string[] Aliases, string[] Categories, int Fuzz);

    // The ways a name can answer a word, strongest first. The gaps matter more than the numbers: a
    // query of several words is scored by adding these up, and one tier has to be worth more than
    // any number of the tiers below it for a two-word name to beat a ten-word one.
    private const int ExactWord = 60;
    private const int WordForm = 50;
    private const int WordPrefix = 30;
    private const int AliasWord = 26;
    private const int AliasInName = 20;
    private const int Fragment = 12;
    private const int NearWord = 8;
    private const int Family = 5;

    private static readonly Lazy<Row[]> _icons = new(Build, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>How many glyphs the set has.</summary>
    public static int Count => _icons.Value.Length;

    public static void Warm() => _ = _icons.Value;

    /// <summary>The glyph names closest to a query, best first.</summary>
    public static string Search(string? query, int limit)
    {
        var typed = (query ?? string.Empty).Trim();

        var terms = typed
            .Split([' ', '-', '_', ',', '.', '/', '+', '&', ':', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => new string([.. word.Where(char.IsLetterOrDigit)]).ToLowerInvariant())
            .Where(word => word.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(6)
            .Select(AsTerm)
            .ToArray();

        if (terms.Length == 0)
        {
            return $"Searching for nothing matches nothing. `BitIconName` declares {Count:N0} glyphs; search for what the glyph shows - \"save\", \"chevron down\", \"add friend\", \"shopping cart\".";
        }

        // The words are also glued back together, because a reader copying a name out of a design
        // has no way of knowing that "add friend" is one word in the font and "chevron down" is too.
        var joined = string.Concat(terms.Select(term => term.Text));

        var hits = new List<(Row Icon, int Score, bool Whole, int Best)>();

        foreach (var icon in _icons.Value)
        {
            var total = 0;
            var matched = 0;
            var best = 0;

            foreach (var term in terms)
            {
                var score = Score(icon, term);

                if (score == 0) continue;

                total += score;
                best = Math.Max(best, score);
                matched++;
            }

            if (matched == 0) continue;

            if (icon.Lowered == joined) total += 400;
            else if (icon.Lowered.StartsWith(joined, StringComparison.Ordinal)) total += 150;
            else if (terms.Length > 1 && icon.Lowered.Contains(joined, StringComparison.Ordinal)) total += 60;

            // A name made of nothing but the words asked for beats one that merely contains them.
            total += Math.Max(0, 8 - icon.Words.Length);

            hits.Add((icon, total, matched == terms.Length, best));
        }

        // An icon that answers every word is what was asked for. One that answers only some of them
        // is a fallback worth showing when nothing answers all - and then only where the word it did
        // answer was a word of the name, or what an alias says that word means, rather than a
        // fragment that happens to fall inside it.
        var whole = hits.Where(hit => hit.Whole).ToList();
        var kept = whole.Count > 0 ? whole : [.. hits.Where(hit => hit.Best > Fragment)];

        if (kept.Count == 0) return Missed(typed);

        var shown = kept
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Icon.Name.Length)
            .ThenBy(hit => hit.Icon.Name, StringComparer.Ordinal)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(hit => hit.Icon)
            .ToArray();

        var builder = new StringBuilder();

        builder.AppendLine($"# BitIconName glyphs matching '{typed}'").AppendLine();

        if (whole.Count == 0)
        {
            builder.AppendLine($"No one glyph answers all of '{typed}'. These answer part of it, closest first.").AppendLine();
        }
        else if (shown.Length < kept.Count)
        {
            builder.AppendLine($"The closest {shown.Length} of {kept.Count:N0} - raise `limit` if none of them is it.").AppendLine();
        }

        builder.AppendLine(string.Join(", ", shown.Select(icon => icon.Name))).AppendLine();

        foreach (var note in Notes(terms, shown).Take(3))
        {
            builder.AppendLine(note).AppendLine();
        }

        builder.AppendLine("Used as `IconName=\"@BitIconName.<Name>\"` on any component that takes an icon, or as the whole of")
               .AppendLine("`<BitIcon IconName=\"@BitIconName.<Name>\" />`. They render only where the `Bit.BlazorUI.Icons`")
               .AppendLine("package is referenced and its stylesheet is on the page - without it the glyph is an empty box.");

        var core = shown.Where(icon => icon.InCore).Select(icon => icon.Name).ToArray();

        if (core.Length > 0)
        {
            builder.AppendLine()
                   .AppendLine($"Except {string.Join(", ", core.Select(name => $"`{name}`"))} - {(core.Length == 1 ? "it is" : "they are")} in the font subset the CORE package")
                   .AppendLine($"embeds for its own components, so {(core.Length == 1 ? "it renders" : "they render")} with no extra package at all.");
        }

        return builder.ToString();
    }

    /// <summary>
    /// How well one name answers one word, or 0 for one it does not answer. The tiers are the ways
    /// a name can be right, strongest first, and the first that holds is the answer: a name IS the
    /// word, is the word in another number, opens with it, is what an alias says the word means, or
    /// - last and weakest, because it is the one most often accidental - merely contains it. Below
    /// even that are the two that only ever decide the bottom of a list: a word one edit away, and
    /// the family the word names.
    /// </summary>
    private static int Score(Row icon, Term term)
    {
        foreach (var word in icon.Words)
        {
            if (word == term.Text) return ExactWord;
        }

        foreach (var form in term.Forms)
        {
            foreach (var word in icon.Words)
            {
                if (word == form) return WordForm;
            }
        }

        if (term.Text.Length >= 3)
        {
            foreach (var word in icon.Words)
            {
                if (word.StartsWith(term.Text, StringComparison.Ordinal)) return WordPrefix;
            }
        }

        // An alias table entry is written best first - "spinner" is a ProgressRing before it is a
        // Sync - so a later fragment is worth less than an earlier one, by enough to order two
        // names that each answer through one but never enough to reach the tier above.
        for (var i = 0; i < term.Aliases.Length; i++)
        {
            var alias = term.Aliases[i];

            // An alias that points at a compound name - "basket" at ShoppingCart, "spinner" at
            // ProgressRing - is one word to the table and two to the splitter, so it is matched
            // against the name rather than the words: opening with it is being it.
            if (icon.Lowered.StartsWith(alias, StringComparison.Ordinal)) return AliasWord - Rank(i);

            foreach (var word in icon.Words)
            {
                if (word == alias) return AliasWord - Rank(i);
            }
        }

        for (var i = 0; i < term.Aliases.Length; i++)
        {
            if (icon.Lowered.Contains(term.Aliases[i], StringComparison.Ordinal)) return AliasInName - Rank(i);
        }

        if (term.Text.Length >= 3 && icon.Lowered.Contains(term.Text, StringComparison.Ordinal)) return Fragment;

        if (term.Fuzz > 0)
        {
            foreach (var word in icon.Words)
            {
                if (word.Length >= 4 && BlazorUISuggest.Within(word, term.Text, term.Fuzz)) return NearWord;
            }
        }

        foreach (var category in term.Categories)
        {
            if (Array.IndexOf(icon.Categories, category) >= 0) return Family;
        }

        return 0;
    }

    /// <summary>
    /// What an alias gives up for being the second thing a word is read as rather than the first -
    /// capped, so that however long a table entry grows its last fragment still outranks the tier
    /// below it. Ordering within a tier is all this is for.
    /// </summary>
    private static int Rank(int position) => Math.Min(2 * position, 6);

    /// <summary>
    /// Everything a query word is also allowed to mean: the same word in the other number, whatever
    /// the alias table points it at, and the family it names if it names one.
    /// </summary>
    private static Term AsTerm(string text)
    {
        // The set names things in the singular and a reader asks in the plural as often as not -
        // "arrows", "charts", "people" - and the reverse happens just as often on a name that is
        // already plural ("Accounts", "Settings").
        var forms = new List<string>(2);

        if (text.Length > 4 && text.EndsWith("ies", StringComparison.Ordinal)) forms.Add(text[..^3] + "y");
        else if (text.Length > 3 && text.EndsWith("es", StringComparison.Ordinal)) forms.Add(text[..^2]);

        if (text.Length > 3 && text.EndsWith('s')) forms.Add(text[..^1]);
        else forms.Add(text + "s");

        var aliases = new List<string>(4);

        foreach (var form in forms.Prepend(text))
        {
            if (IconCatalog.Aliases.TryGetValue(form, out var pointed) is false) continue;

            foreach (var alias in pointed)
            {
                if (aliases.Contains(alias, StringComparer.Ordinal) is false) aliases.Add(alias);
            }
        }

        var categories = IconCatalog.Categories
            .Where(category => CategoryWords(category).Any(word => word == text || forms.Contains(word, StringComparer.Ordinal)))
            .ToArray();

        // A typo is forgiven in proportion to how much was typed: one edit inside a short word is
        // usually a different word ("card" and "cart" are both in the set), where in a long one it
        // is a slip - "calender", "favourite", "recieve".
        var fuzz = text.Length >= 8 ? 2 : text.Length >= 5 ? 1 : 0;

        return new Term(text, [.. forms], [.. aliases], categories, fuzz);
    }

    /// <summary>
    /// What to say about a word the answer does not literally contain, so that a list of names with
    /// none of the query in them reads as an answer rather than as a miss dressed up as one.
    /// </summary>
    private static IEnumerable<string> Notes(Term[] terms, Row[] shown)
    {
        foreach (var term in terms)
        {
            if (shown.Any(icon => icon.Lowered.Contains(term.Text, StringComparison.Ordinal))) continue;

            var through = term.Aliases.Length > 0
                ? term.Aliases
                : [.. shown.SelectMany(icon => icon.Words)
                           .Distinct(StringComparer.Ordinal)
                           .Where(word => word.Length >= 4 && BlazorUISuggest.Within(word, term.Text, term.Fuzz))
                           .Take(3)];

            if (through.Length == 0) continue;

            yield return $"MDL2 has no '{term.Text}' - it is read here as {string.Join(", ", through.Select(word => $"`{word}`"))}.";
        }
    }

    /// <summary>
    /// A query nothing answers, answered anyway - with the names nearest to what was typed and the
    /// families the set is browsed by, either of which is something to try next.
    /// </summary>
    private static string Missed(string typed)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"No `BitIconName` glyph matches '{typed}'. The names are Microsoft's Fabric (MDL2) names, so try the")
               .AppendLine("object rather than the action - \"mail\" rather than \"send message\", \"cancel\" rather than \"dismiss\".")
               .AppendLine();

        var closest = BlazorUISuggest.Closest(typed, _icons.Value.Select(icon => icon.Name));

        if (closest.Length > 0)
        {
            builder.AppendLine($"Did you mean: {string.Join(", ", closest)}?").AppendLine();
        }

        builder.Append("Or search one of the families the set is browsed by: ")
               .Append(string.Join(", ", IconCatalog.Categories))
               .AppendLine(".");

        return builder.ToString();
    }

    private static string[] CategoryWords(string category)
    {
        return [.. category
            .Split([' ', '&'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => word.ToLowerInvariant())];
    }

    /// <summary>
    /// The catalog the iconography page browses, as this tool reads it: the C# member name to print
    /// - not always the glyph name, since the handful that start with a digit carry a leading
    /// underscore and <c>BitIconName.12PointStar</c> does not compile - beside what it is matched on.
    /// </summary>
    private static Row[] Build()
    {
        return [.. IconCatalog.Items.Select(icon => new Row(
            icon.FieldName,
            icon.Lower,
            icon.Words,
            icon.InCorePackage,
            [.. IconCatalog.CategoriesOf(icon)]))];
    }
}
