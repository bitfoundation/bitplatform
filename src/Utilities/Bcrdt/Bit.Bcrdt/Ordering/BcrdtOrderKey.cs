using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// Fractional order keys: strings that sort ordinally, with room for another key between any two.
/// </summary>
/// <remarks>
/// <para>
/// A position stored as an index is meaningless once two replicas edit concurrently - "third" on one
/// replica is "fourth" on another. A position stored as a key that sorts among its neighbours is not:
/// every replica sorts the same keys the same way, and moving something is a single write of a new
/// key between the keys of its new neighbours. This is how <see cref="BcrdtOrderedCollection"/>
/// orders items without ever deleting and re-inserting them.
/// </para>
/// <para>
/// The scheme is the one David Greenspan described for Figma and published as <c>fractional-indexing</c>:
/// a key is an <i>integer part</i> - one head character that says how many digits follow, then those
/// digits - and an optional <i>fraction</i>, with base-62 digits (<c>0-9A-Za-z</c>) that sort in the
/// same order as their values under <see cref="string.CompareOrdinal(string?, string?)"/>. The first
/// key is <c>a0</c>. Generating past either end steps the integer part, which only gains a digit
/// every power of 62, so a list that grows by repeated appends or prepends keeps keys a handful of
/// characters long; generating between two keys extends the fraction by about one character per six
/// halvings of the gap. A fraction never ends in <c>0</c>, which is what keeps every key unique to
/// its position: <c>a0V</c> and <c>a0V0</c> would otherwise be two names for the same point.
/// </para>
/// <para>
/// Keys from a peer are untrusted. <see cref="IsValid(string?)"/> says whether a string is a key this
/// class can generate around; ordering by <see cref="string.CompareOrdinal(string?, string?)"/>
/// copes with any string at all, so a collection can sort garbage deterministically and repair it
/// on the next write near it.
/// </para>
/// </remarks>
public static class BcrdtOrderKey
{
    /// <summary>The base-62 digits, in ordinal order.</summary>
    private const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>The integer part with the most digits and the smallest value, which nothing can precede.</summary>
    private static readonly string SmallestInteger = "A" + new string('0', 26);

    /// <summary>
    /// How many times <see cref="Between(string?, string?, Random)"/> halves the gap before it
    /// settles: two replicas inserting into the same gap pick the same key one time in 2^16.
    /// </summary>
    private const int RandomBisections = 16;

    /// <summary>The key a list's first element gets: the integer zero.</summary>
    public const string First = "a0";

    /// <summary>Whether <paramref name="key"/> is a well-formed order key.</summary>
    /// <remarks>
    /// Well-formed means: a head character <c>A-Z</c> or <c>a-z</c>, exactly as many integer digits as
    /// the head calls for, base-62 digits throughout, and a fraction - if any - that does not end in
    /// <c>0</c>. The single smallest integer, <c>A</c> followed by 26 zeros, is not a key either,
    /// because there would be nothing to generate before it.
    /// </remarks>
    public static bool IsValid(string? key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        int integerLength = IntegerLength(key[0]);
        if (integerLength < 0 || key.Length < integerLength) return false;

        for (int i = 1; i < key.Length; i++)
        {
            if (DigitValue(key[i]) < 0) return false;
        }

        if (key.Length > integerLength && key[^1] == '0') return false;
        return !string.Equals(key, SmallestInteger, StringComparison.Ordinal);
    }

    /// <summary>
    /// Generates the key that sorts strictly between <paramref name="before"/> and <paramref name="after"/>.
    /// </summary>
    /// <param name="before">The key to sort after, or <see langword="null"/> for the start of the list.</param>
    /// <param name="after">The key to sort before, or <see langword="null"/> for the end of the list.</param>
    /// <remarks>
    /// The result is deterministic, and as short as the scheme allows: past either end it is the next
    /// integer, and between two keys it is the midpoint. That makes it the right choice when one
    /// replica alone decides a position; when several may insert into the same gap at once, prefer
    /// <see cref="Between(string?, string?, Random)"/>, which spreads them apart.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// A key is malformed (see <see cref="IsValid(string?)"/>), or <paramref name="before"/> does not
    /// sort strictly before <paramref name="after"/>.
    /// </exception>
    public static string Between(string? before, string? after)
    {
        Validate(before, after);
        return BetweenValid(before, after);
    }

    /// <summary>
    /// Generates a key at a random point strictly between <paramref name="before"/> and
    /// <paramref name="after"/>.
    /// </summary>
    /// <param name="before">The key to sort after, or <see langword="null"/> for the start of the list.</param>
    /// <param name="after">The key to sort before, or <see langword="null"/> for the end of the list.</param>
    /// <param name="random">The source of the choice.</param>
    /// <remarks>
    /// <para>
    /// Two replicas that insert between the same neighbours at the same time compute the same
    /// deterministic midpoint, so their items tie on the key - an order a collection still settles,
    /// by identifier, but a tie that must be broken up before anything can go between them. This
    /// instead halves the gap sixteen times, keeping a random half each time, and takes the midpoint
    /// of what is left: one of 65 536 evenly spread points, so a collision is a one-in-65 536 event.
    /// </para>
    /// <para>
    /// The price is about three characters on the key. Repeated appends and prepends still step the
    /// integer part, so they stay short.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// A key is malformed, or <paramref name="before"/> does not sort strictly before <paramref name="after"/>.
    /// </exception>
    public static string Between(string? before, string? after, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        Validate(before, after);

        string? low = before;
        string? high = after;
        for (int round = 0; round < RandomBisections; round++)
        {
            // Every key generated lies strictly inside the current gap, so the two halves are
            // disjoint and different choices can never meet.
            string middle = BetweenValid(low, high);
            if (random.Next(2) == 0) low = middle;
            else high = middle;
        }

        return BetweenValid(low, high);
    }

    /// <summary>
    /// Generates <paramref name="count"/> keys, in ascending order, strictly between
    /// <paramref name="before"/> and <paramref name="after"/>.
    /// </summary>
    /// <param name="before">The key to sort after, or <see langword="null"/> for the start of the list.</param>
    /// <param name="after">The key to sort before, or <see langword="null"/> for the end of the list.</param>
    /// <param name="count">How many keys to generate.</param>
    /// <remarks>
    /// For a bulk insert or for giving a whole list keys at once. Toward an open end the keys are
    /// consecutive integers; between two keys the gap is split recursively, so the longest key grows
    /// with the logarithm of <paramref name="count"/> rather than with <paramref name="count"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// A key is malformed, or <paramref name="before"/> does not sort strictly before <paramref name="after"/>.
    /// </exception>
    public static IReadOnlyList<string> BetweenMany(string? before, string? after, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Validate(before, after);

        var keys = new string[count];
        Fill(keys, 0, count, before, after);
        return keys;
    }

    private static void Fill(string[] keys, int start, int count, string? before, string? after)
    {
        if (count == 0) return;

        if (after is null)
        {
            string? current = before;
            for (int i = 0; i < count; i++) keys[start + i] = current = BetweenValid(current, null);
            return;
        }

        if (before is null)
        {
            string? current = after;
            for (int i = count - 1; i >= 0; i--) keys[start + i] = current = BetweenValid(null, current);
            return;
        }

        int middle = count / 2;
        string key = BetweenValid(before, after);
        keys[start + middle] = key;
        Fill(keys, start, middle, before, key);
        Fill(keys, start + middle + 1, count - middle - 1, key, after);
    }

    private static void Validate(string? before, string? after)
    {
        if (before is not null && !IsValid(before)) throw new ArgumentException($"'{before}' is not a valid order key.", nameof(before));
        if (after is not null && !IsValid(after)) throw new ArgumentException($"'{after}' is not a valid order key.", nameof(after));
        if (before is not null && after is not null && string.CompareOrdinal(before, after) >= 0)
        {
            throw new ArgumentException($"'{before}' does not sort before '{after}'.", nameof(before));
        }
    }

    private static string BetweenValid(string? before, string? after)
    {
        if (before is null)
        {
            if (after is null) return First;

            string integerAfter = after[..IntegerLength(after[0])];
            if (string.Equals(integerAfter, SmallestInteger, StringComparison.Ordinal))
            {
                return integerAfter + Midpoint("", after[integerAfter.Length..]);
            }

            // A key with a fraction sorts after its own integer part, which is then free.
            if (integerAfter.Length < after.Length) return integerAfter;
            return Decrement(integerAfter) ?? throw new InvalidOperationException("No integer precedes the smallest one.");
        }

        string integerBefore = before[..IntegerLength(before[0])];
        string fractionBefore = before[integerBefore.Length..];

        if (after is null)
        {
            return Increment(integerBefore) ?? integerBefore + Midpoint(fractionBefore, null);
        }

        string integerAfterPart = after[..IntegerLength(after[0])];
        if (string.Equals(integerBefore, integerAfterPart, StringComparison.Ordinal))
        {
            return integerBefore + Midpoint(fractionBefore, after[integerAfterPart.Length..]);
        }

        string next = Increment(integerBefore) ?? throw new InvalidOperationException("No integer follows the largest one.");
        return string.CompareOrdinal(next, after) < 0 ? next : integerBefore + Midpoint(fractionBefore, null);
    }

    /// <summary>
    /// A fraction strictly between two fractions, where <paramref name="after"/> of <see langword="null"/>
    /// is the open end, and <paramref name="before"/> reads as padded with zeros.
    /// </summary>
    /// <remarks>
    /// Iterative rather than recursive like the reference implementation: a key from a peer can be
    /// arbitrarily long, and recursing once per character would overflow the stack.
    /// </remarks>
    private static string Midpoint(string before, string? after)
    {
        var result = new StringBuilder();
        int a = 0;
        int b = 0;

        while (true)
        {
            if (after is not null)
            {
                // Copy the common prefix, reading past the end of 'before' as zeros. It stops inside
                // 'after', because 'after' sorts higher and does not end in a zero.
                while (b < after.Length - 1 && (a < before.Length ? before[a] : '0') == after[b])
                {
                    result.Append(after[b]);
                    a++;
                    b++;
                }
            }

            int digitBefore = a < before.Length ? DigitValue(before[a]) : 0;
            int digitAfter = after is not null ? DigitValue(after[b]) : Digits.Length;

            if (digitAfter - digitBefore > 1)
            {
                result.Append(Digits[(digitBefore + digitAfter + 1) / 2]);
                return result.ToString();
            }

            if (after is not null && after.Length - b > 1)
            {
                // 'after' continues past this digit, so the digit alone already sorts below it.
                result.Append(after[b]);
                return result.ToString();
            }

            // The digits are consecutive: keep 'before's digit and find room after the rest of it.
            result.Append(Digits[digitBefore]);
            a++;
            after = null;
        }
    }

    private static string? Increment(string integer)
    {
        char[] digits = integer.ToCharArray();
        for (int i = digits.Length - 1; i > 0; i--)
        {
            int value = DigitValue(digits[i]) + 1;
            if (value < Digits.Length)
            {
                digits[i] = Digits[value];
                return new string(digits);
            }
            digits[i] = '0';
        }

        char head = integer[0];
        if (head == 'Z') return "a0";
        if (head == 'z') return null;

        // Every digit carried: the next head holds one digit more among the positives, one fewer
        // among the negatives.
        char nextHead = (char)(head + 1);
        string rest = new(digits, 1, digits.Length - 1);
        return nextHead > 'a' ? nextHead + rest + "0" : nextHead + rest[..^1];
    }

    private static string? Decrement(string integer)
    {
        char[] digits = integer.ToCharArray();
        for (int i = digits.Length - 1; i > 0; i--)
        {
            int value = DigitValue(digits[i]) - 1;
            if (value >= 0)
            {
                digits[i] = Digits[value];
                return new string(digits);
            }
            digits[i] = 'z';
        }

        char head = integer[0];
        if (head == 'a') return "Zz";
        if (head == 'A') return null;

        char previousHead = (char)(head - 1);
        string rest = new(digits, 1, digits.Length - 1);
        return previousHead < 'Z' ? previousHead + rest + "z" : previousHead + rest[..^1];
    }

    /// <summary>The length of the integer part a head character introduces, or -1 for no head at all.</summary>
    private static int IntegerLength(char head) => head switch
    {
        >= 'a' and <= 'z' => head - 'a' + 2,
        >= 'A' and <= 'Z' => 'Z' - head + 2,
        _ => -1,
    };

    private static int DigitValue(char digit) => digit switch
    {
        >= '0' and <= '9' => digit - '0',
        >= 'A' and <= 'Z' => digit - 'A' + 10,
        >= 'a' and <= 'z' => digit - 'a' + 36,
        _ => -1,
    };
}
