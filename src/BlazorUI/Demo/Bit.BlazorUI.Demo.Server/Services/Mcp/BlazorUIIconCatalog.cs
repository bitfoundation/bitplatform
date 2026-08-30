using System.Text;
using System.Reflection;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// The Fabric (MDL2) glyph names <c>BitIconName</c> declares, and a search over them.
/// <para>
/// There are over two thousand, which is why this is a search rather than a listing: handing the
/// whole set to a client costs more than every other answer this server gives put together, and an
/// agent wants one of them. The names are compound and written in Pascal case
/// (<c>AddFriend</c>, <c>ChevronDownSmall</c>), so they are matched word by word - "add friend"
/// finds <c>AddFriend</c>, and a request for "delete" finds <c>Delete</c> ahead of
/// <c>DeleteTable</c>.
/// </para>
/// </summary>
public static class BlazorUIIconCatalog
{
    private sealed record Icon(string Name, string Lowered, string[] Words);

    private static readonly Lazy<Icon[]> _icons = new(Build, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Every glyph name, in the order the class declares them.</summary>
    public static int Count => _icons.Value.Length;

    public static void Warm() => _ = _icons.Value;

    /// <summary>The glyph names closest to a query, best first.</summary>
    public static string Search(string? query, int limit)
    {
        var terms = (query ?? string.Empty)
            .Split([' ', '-', '_', ',', '.', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (terms.Length == 0)
        {
            return $"Searching for nothing matches nothing. `BitIconName` declares {Count:N0} glyphs; search for what the glyph shows - \"save\", \"chevron down\", \"add friend\", \"shopping cart\".";
        }

        var hits = _icons.Value
            .Select(icon => (icon.Name, Score: Score(icon, terms)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Name.Length)
            .ThenBy(hit => hit.Name, StringComparer.Ordinal)
            .Take(Math.Clamp(limit, 1, 200))
            .ToArray();

        if (hits.Length == 0)
        {
            return $"No `BitIconName` glyph matches '{query}'. The names are Microsoft's Fabric (MDL2) names, so try the object rather than the action - \"mail\" rather than \"send message\", \"cancel\" rather than \"dismiss\".";
        }

        var builder = new StringBuilder();

        builder.AppendLine($"# BitIconName glyphs matching '{query}'").AppendLine();
        builder.AppendLine(string.Join(", ", hits.Select(h => h.Name))).AppendLine();
        builder.AppendLine("Used as `IconName=\"@BitIconName.<Name>\"` on any component that takes an icon, or as the whole of")
               .AppendLine("`<BitIcon IconName=\"@BitIconName.<Name>\" />`. They render only where the `Bit.BlazorUI.Icons`")
               .AppendLine("package is referenced and its stylesheet is on the page - without it the glyph is an empty box.");

        return builder.ToString();
    }

    private static int Score(Icon icon, string[] terms)
    {
        var score = 0;

        foreach (var term in terms)
        {
            if (icon.Lowered == term) score += 100;
            else if (icon.Words.Any(w => w == term)) score += 30;
            else if (icon.Words.Any(w => w.StartsWith(term, StringComparison.Ordinal))) score += 12;
            else if (icon.Lowered.Contains(term, StringComparison.Ordinal)) score += 4;
            else return 0;
        }

        // A name made of nothing but the words asked for beats one that merely contains them.
        return score + Math.Max(0, 8 - icon.Words.Length);
    }

    private static Icon[] Build()
    {
        return [.. typeof(BitIconName)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Select(name => new Icon(name, name.ToLowerInvariant(), Words(name)))];
    }

    /// <summary>The Pascal-case humps of a glyph name, lowered - "AddFriend" becomes ["add", "friend"].</summary>
    private static string[] Words(string name)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        foreach (var c in name)
        {
            // A capital after a lowercase letter starts a new word; a run of capitals ("PDF", "SQL")
            // stays together, and a digit run starts one of its own.
            if (current.Length > 0 && (char.IsUpper(c) && char.IsLower(current[^1]) || char.IsDigit(c) != char.IsDigit(current[^1])))
            {
                words.Add(current.ToString().ToLowerInvariant());
                current.Clear();
            }

            current.Append(c);
        }

        if (current.Length > 0) words.Add(current.ToString().ToLowerInvariant());

        return [.. words];
    }
}
