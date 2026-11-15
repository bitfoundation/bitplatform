namespace Bit.Bink;

/// <summary>
/// Fractional order keys: strings that sort in stacking order, with room for a new key between any two. They are
/// what lets concurrent reorders reconcile: an element's place is its key, not its index, so moves made at the same
/// time by different people land in the same order everywhere.
/// </summary>
/// <remarks>
/// <para>
/// A key is an integer part followed by an optional fraction, in base-62 digits (<c>0-9</c>, <c>A-Z</c>, <c>a-z</c>,
/// which sort in that order under ordinal comparison). The integer part's first character gives its length: <c>a</c>
/// to <c>z</c> head integers of 2 to 27 characters counting up, <c>Z</c> down to <c>A</c> integers counting down, so
/// adding on top or underneath steps the integer and keys grow with the logarithm of the count: the first 62 strokes
/// drawn are <c>a0</c> to <c>az</c>, the next 3,844 <c>b00</c> to <c>bzz</c>. Only an insertion between two
/// neighbours needs a fraction, which never ends in <c>0</c>, so there is always room below it.
/// </para>
/// <para>This is the scheme of the widely used "fractional indexing" libraries, so keys interoperate with them.</para>
/// </remarks>
public static class BinkOrderKey
{
    /// <summary>The longest key accepted from a document or a collaborator, and the longest <see cref="Between"/> makes.</summary>
    public const int MaxLength = 4096;

    private const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>The lowest integer part, which has nothing below it and so is not a key itself.</summary>
    private static readonly string Smallest = "A" + new string('0', 26);

    /// <summary>Whether a string is a well-formed key.</summary>
    public static bool IsValid(string? key)
    {
        if (string.IsNullOrEmpty(key) || key.Length > MaxLength) return false;
        int length = IntegerLength(key[0]);
        if (length < 0 || length > key.Length) return false;
        foreach (char c in key) if (Digit(c) < 0) return false;
        if (key.AsSpan(0, length).SequenceEqual(Smallest)) return false;
        return key.Length == length || key[^1] != '0';
    }

    /// <summary>Compares keys in stacking order.</summary>
    public static int Compare(string? a, string? b) => string.CompareOrdinal(a, b);

    /// <summary>
    /// A key that sorts after <paramref name="before"/> and before <paramref name="after"/>; null for either means the
    /// bottom or the top of the stack.
    /// </summary>
    /// <exception cref="ArgumentException">A key is malformed, or <paramref name="before"/> does not sort before <paramref name="after"/>.</exception>
    /// <exception cref="InvalidOperationException">There is no room left: at the very top or bottom after about 10^48 additions there, or between two keys after some twenty thousand insertions at one spot.</exception>
    public static string Between(string? before, string? after)
    {
        string key = BetweenCore(before, after);
        // Inserting again and again at one spot lengthens keys by about a character every five insertions; past
        // MaxLength (some twenty thousand insertions there) the key could not be read back, so it is refused as a
        // change that cannot be applied.
        return key.Length <= MaxLength ? key : throw new InvalidOperationException($"There is no room left for an order key at this place: it would be longer than {MaxLength} characters.");
    }

    private static string BetweenCore(string? before, string? after)
    {
        if (before is not null && !IsValid(before)) throw new ArgumentException($"\"{before}\" is not an order key.", nameof(before));
        if (after is not null && !IsValid(after)) throw new ArgumentException($"\"{after}\" is not an order key.", nameof(after));
        if (before is not null && after is not null && string.CompareOrdinal(before, after) >= 0)
            throw new ArgumentException($"\"{before}\" does not sort before \"{after}\".", nameof(before));

        if (before is null)
        {
            if (after is null) return "a0";
            string ib = IntegerPart(after), fb = after[ib.Length..];
            if (ib == Smallest) return ib + Midpoint(string.Empty, fb);
            if (string.CompareOrdinal(ib, after) < 0) return ib;
            return Decrement(ib) ?? throw new InvalidOperationException("There is no room below the lowest order key.");
        }
        string ia = IntegerPart(before), fa = before[ia.Length..];
        if (after is null) return Increment(ia) ?? ia + Midpoint(fa, null);

        string ib2 = IntegerPart(after), fb2 = after[ib2.Length..];
        if (ia == ib2) return ia + Midpoint(fa, fb2);
        string? next = Increment(ia) ?? throw new InvalidOperationException("There is no room above the highest order key.");
        return string.CompareOrdinal(next, after) < 0 ? next : ia + Midpoint(fa, null);
    }

    /// <summary>
    /// <paramref name="count"/> keys in increasing order, as short as the count allows: the keys of a layer read from a
    /// file that does not store them.
    /// </summary>
    public static string[] Spread(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var keys = new string[count];
        string? previous = null;
        for (int i = 0; i < count; i++) previous = keys[i] = Between(previous, null);
        return keys;
    }

    private static int Digit(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'A' and <= 'Z' => c - 'A' + 10,
        >= 'a' and <= 'z' => c - 'a' + 36,
        _ => -1,
    };

    private static int IntegerLength(char head) => head switch
    {
        >= 'a' and <= 'z' => head - 'a' + 2,
        >= 'A' and <= 'Z' => 'Z' - head + 2,
        _ => -1,
    };

    private static string IntegerPart(string key) => key[..IntegerLength(key[0])];

    /// <summary>The next integer, or null past the highest.</summary>
    private static string? Increment(string integer)
    {
        char head = integer[0];
        var digits = new List<char>(integer.Length);
        for (int i = 1; i < integer.Length; i++) digits.Add(integer[i]);
        bool carry = true;
        for (int i = digits.Count - 1; carry && i >= 0; i--)
        {
            int d = Digit(digits[i]) + 1;
            if (d == Digits.Length) digits[i] = Digits[0];
            else { digits[i] = Digits[d]; carry = false; }
        }
        if (!carry) return head + new string([.. digits]);
        if (head == 'Z') return "a" + Digits[0];
        if (head == 'z') return null;
        char h = (char)(head + 1);
        // Counting up, a longer integer follows; counting the negative side up, a shorter one.
        if (h > 'a') digits.Add(Digits[0]);
        else digits.RemoveAt(digits.Count - 1);
        return h + new string([.. digits]);
    }

    /// <summary>The previous integer, or null below the lowest.</summary>
    private static string? Decrement(string integer)
    {
        char head = integer[0];
        var digits = new List<char>(integer.Length);
        for (int i = 1; i < integer.Length; i++) digits.Add(integer[i]);
        bool borrow = true;
        for (int i = digits.Count - 1; borrow && i >= 0; i--)
        {
            int d = Digit(digits[i]) - 1;
            if (d < 0) digits[i] = Digits[^1];
            else { digits[i] = Digits[d]; borrow = false; }
        }
        if (!borrow) return head + new string([.. digits]);
        if (head == 'a') return "Z" + Digits[^1];
        if (head == 'A') return null;
        char h = (char)(head - 1);
        if (h < 'Z') digits.Add(Digits[^1]);
        else digits.RemoveAt(digits.Count - 1);
        return h + new string([.. digits]);
    }

    /// <summary>The midpoint of two fractions written as digit strings, <paramref name="b"/> null meaning 1.</summary>
    private static string Midpoint(string a, string? b)
    {
        if (b is not null)
        {
            // Skip the digits they share; a is read as padded with zeros.
            int n = 0;
            while (n < b.Length && (n < a.Length ? a[n] : '0') == b[n]) n++;
            if (n > 0) return b[..n] + Midpoint(n < a.Length ? a[n..] : string.Empty, b[n..]);
        }
        int low = a.Length > 0 ? Digit(a[0]) : 0;
        int high = b is not null ? Digit(b[0]) : Digits.Length;
        if (high - low > 1) return Digits[(low + high) / 2].ToString();
        // The first digits are adjacent: b's first digit alone fits when b goes on past it, otherwise the key keeps
        // a's first digit and continues between the rest of a and the top.
        if (b is not null && b.Length > 1) return b[..1];
        return Digits[low] + Midpoint(a.Length > 0 ? a[1..] : string.Empty, null);
    }
}
