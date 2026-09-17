namespace Bit.Minifier;

internal static class ShortName
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>0 -> a, 51 -> Z, 52 -> aa, ...</summary>
    public static string Get(int index)
    {
        var name = "";
        do
        {
            name = Alphabet[index % Alphabet.Length] + name;
            index = (index / Alphabet.Length) - 1;
        } while (index >= 0);
        return name;
    }

    /// <summary>The next short name not in <paramref name="taken"/>, which it is then added to.</summary>
    public static string Next(HashSet<string> taken, ref int counter)
    {
        while (true)
        {
            var name = Get(counter++);
            if (taken.Add(name)) return name;
        }
    }
}

/// <summary>Roslyn's generated names whose bracketed part is a hand-written member name.</summary>
internal static class GeneratedName
{
    /// <summary>
    /// <c>&lt;Name&gt;k__rest</c> with k one of d (state machine), b (lambda), g (local function) or
    /// k (backing field): <paramref name="inner"/> is <c>Name</c>, <paramref name="rest"/> is <c>&gt;k__rest</c>.
    /// </summary>
    public static bool TryParse(string name, out string inner, out string rest)
    {
        inner = rest = "";
        if (name.Length < 5 || name[0] != '<') return false;

        var close = name.IndexOf('>');
        if (close <= 1 || close + 3 >= name.Length) return false;
        if (name[close + 1] is not ('d' or 'b' or 'g' or 'k') || name[close + 2] != '_' || name[close + 3] != '_') return false;

        inner = name[1..close];
        if (inner.Contains('<')) return false;
        rest = name[close..];
        return true;
    }
}
